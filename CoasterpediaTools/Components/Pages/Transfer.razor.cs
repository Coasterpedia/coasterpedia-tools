using System.ComponentModel.DataAnnotations;
using CoasterpediaTools.Clients.Geograph;
using CoasterpediaTools.Clients.Wiki;
using CoasterpediaTools.Components.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WikiClientLibrary;
using WikiClientLibrary.Files;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Queries;
using WikiClientLibrary.Pages.Queries.Properties;
using WikiClientLibrary.Sites;

namespace CoasterpediaTools.Components.Pages;

public partial class Transfer
{
    [Inject] WikiSiteAccessor WikiSite { get; init; }
    [Inject] IGeographClient GeographClient { get; init; }

    private UrlForm _urlFormModel = new();
    private DetailsForm _detailsFormModel = new();
    private MudStepper _stepper = new();
    private bool _processing;
    private string _thumbnailUrl;
    private string _warning = string.Empty;
    private UploadResult _uploadResult;
    private string _extension = string.Empty;
    private string? _fileName;
    private string? _url;
    private string? _wikitext;

    private class UrlForm
    {
        [Required] public string Url { get; set; }
    }

    private class DetailsForm
    {
        [Required] public string Title { get; set; }

        [Required] public string Description { get; set; }

        [Required] public List<MultiSelectChip<string>> Categories { get; set; } = [];
        public string? Date { get; set; }
        public string Source { get; set; }
        public string Author { get; set; }
        public string License { get; set; }
        public string? AdditionalLicense { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
    }

    private async Task GetPhotos()
    {
        _processing = true;
        _warning = string.Empty;
        if (!Uri.IsWellFormedUriString(_urlFormModel.Url, UriKind.Absolute) ||
            !Uri.TryCreate(_urlFormModel.Url, UriKind.Absolute, out var uri))
        {
            _warning = "Invalid URL.";
            _processing = false;
            return;
        }

        if (uri.Host == "commons.wikimedia.org")
        {
            if (!uri.AbsolutePath.StartsWith("/wiki/File:"))
            {
                _warning = "Unrecognised Wikimedia Commons URL, please enter file page (commons.wikimedia.org/File:Example)";
                _processing = false;
                return;
            }

            await ProcessCommonsImage(uri);
            _processing = false;
            return;
        }

        if (uri.Host == "www.geograph.org.uk")
        {
            if (!uri.AbsolutePath.StartsWith("/photo/"))
            {
                _warning = "Unrecognised Geograph URL, please enter photo page (www.geograph.org.uk/photo/123)";
                _processing = false;
                return;
            }

            await ProcessGeographImage(uri);
            _processing = false;
            return;
        }

        if (uri.Host == "upload.wikimedia.org" || uri.Host.Contains("wikipedia.org"))
        {
            _warning = "Invalid URL, please link to Wikimedia Commons file URL (commons.wikimedia.org/File:Example)";
            _processing = false;
            return;
        }

        _warning = "Unrecognised website";
        _processing = false;
    }

    private async Task ProcessGeographImage(Uri uri)
    {
        var filename = uri.AbsolutePath[7..];
        var page = await GeographClient.GetPhotoAsync(filename);

        if (page.Error != null)
        {
            _warning = page.Error;
            return;
        }

        var result = await UploadFile(Path.GetFileName(page.Image), page.Imgserver + page.Image);
        if (!result)
        {
            return;
        }

        _thumbnailUrl = page.Imgserver + page.Image;
        _detailsFormModel.Title = page.Title!;
        _detailsFormModel.Date = page.Taken;
        _detailsFormModel.Source = "https://www.geograph.org.uk/photo/" + filename;
        _detailsFormModel.Author = page.Realname;
        _detailsFormModel.Latitude = page.Wgs84Lat;
        _detailsFormModel.Longitude = page.Wgs84Long;
        _detailsFormModel.License = "cc-by-sa-2.0";
        _detailsFormModel.AdditionalLicense = $$$"""{{Geograph|{{{_detailsFormModel.Source}}}}}""";
        await _stepper.NextStepAsync();
    }

    private async Task ProcessCommonsImage(Uri uri)
    {
        var commonsSite = await WikiSite.GetCommons();
        var filename = Uri.UnescapeDataString(uri.AbsolutePath[11..]);

        var page = new WikiPage(commonsSite, $"File:{filename}");
        await page.RefreshAsync(new WikiPageQueryProvider
        {
            Properties =
            {
                new FileInfoPropertyProvider { QueryExtMetadata = true },
                new PageImagesPropertyProvider { ThumbnailSize = 200 }
            }
        });
        if (!page.Exists || page.GetPropertyGroup<FileInfoPropertyGroup>() == null || page.GetPropertyGroup<PageImagesPropertyGroup>() == null)
        {
            _warning = "File not found.";
            return;
        }

        _thumbnailUrl = page.GetPropertyGroup<PageImagesPropertyGroup>().ThumbnailImage.Url;
        var fileInfo = page.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision;
        var result = await UploadFile(page.Title, fileInfo.Url);
        if (!result)
        {
            return;
        }

        _detailsFormModel.Title = filename[..^_extension.Length].Replace('_', ' ');
        _detailsFormModel.Date = fileInfo.ExtMetadata["DateTimeOriginal"].Value?.ToString();
        _detailsFormModel.Source = fileInfo.DescriptionUrl;
        _detailsFormModel.Author = fileInfo.UserName;
        if (fileInfo.ExtMetadata.TryGetValue("GPSLatitude", out var latitude) && fileInfo.ExtMetadata.TryGetValue("GPSLongitude", out var longitude))
        {
            _detailsFormModel.Latitude = latitude.Value?.ToString();
            _detailsFormModel.Longitude = longitude.Value?.ToString();
        }

        _detailsFormModel.License = fileInfo.ExtMetadata["License"].Value.ToString();
        _detailsFormModel.AdditionalLicense = $$$"""{{Wikimedia Commons|{{{fileInfo.DescriptionUrl}}}}}""";
        await _stepper.NextStepAsync();
    }

    private async Task<bool> UploadFile(string title, string url)
    {
        var coasterpediaSite = await WikiSite.GetCoasterpedia();
        try
        {
            _uploadResult = await coasterpediaSite.UploadAsync(title, new ExternalFileStashSource(url), "", false);

            if (_uploadResult.Warnings.Count > 0)
            {
                _warning = _uploadResult.Warnings.ToString();
                return false;
            }
        }
        catch (OperationFailedException ex)
        {
            _warning = ex.ErrorMessage;
            return false;
        }

        _extension = Path.GetExtension(title).ToLower();
        return true;
    }

    private async Task Upload()
    {
        _processing = true;
        _warning = string.Empty;
        var coasterpediaSite = await WikiSite.GetCoasterpedia();

        _fileName = _detailsFormModel.Title;
        var newExtension = Path.GetExtension(_fileName);
        if (newExtension != string.Empty)
        {
            _fileName = _fileName[..^newExtension.Length];
        }

        _fileName = _fileName.Trim() + _extension;
        _url = "https://coasterpedia.net/wiki/File:" + _fileName.Replace(' ', '_');
        _wikitext = $"[[File:{_fileName}|thumb]]";

        var comment = $$$"""
                         =={{int:filedesc}}==
                         {{Information
                         |description={{{_detailsFormModel.Description}}}
                         |date={{{_detailsFormModel.Date}}}
                         |source={{{_detailsFormModel.Source}}}
                         |author={{{_detailsFormModel.Author}}}
                         |permission=
                         |other versions=
                         }}

                         """;

        if (_detailsFormModel is { Latitude: not null, Longitude: not null })
        {
            comment += $$$"""
                          {{Location|{{{_detailsFormModel.Latitude}}}|{{{_detailsFormModel.Longitude}}}|}}

                          """;
        }

        comment += $$$"""
                      =={{int:license-header}}==
                      {{{{{_detailsFormModel.License}}}}}

                      """;

        if (_detailsFormModel.AdditionalLicense is not null)
        {
            comment += _detailsFormModel.AdditionalLicense + "\n";
        }

        foreach (var category in _detailsFormModel.Categories)
        {
            comment += $"[[Category:{category.Value}]]\n";
        }

        try
        {
            var result = await coasterpediaSite.UploadAsync(_fileName, new FileKeyUploadSource(_uploadResult.FileKey), comment, false);
            if (result.Warnings.Count > 0)
            {
                _warning = result.Warnings.ToString();
                _processing = false;
                return;
            }
        }
        catch (OperationFailedException ex)
        {
            _warning = ex.ErrorMessage;
            _processing = false;
            return;
        }

        _processing = false;
        await _stepper.NextStepAsync();
    }

    private async Task<IEnumerable<string>> CategorySearch(string value, CancellationToken token)
    {
        // In real life use an asynchronous function for fetching data from an api.
        var coasterpediaSite = await WikiSite.GetCoasterpedia();
        var result = await coasterpediaSite.OpenSearchAsync(value, 10, 14, OpenSearchOptions.None, token);

        // if text is null or empty, don't return values (drop-down will not open)
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return new[] { value } // Add current input to top of category list
            .Concat(result
                .Select(x => x.Title[9..]) // Remove Category: from beginning of result
                .Where(x => x != value)); // Remove current value if in the returned list to prevent same item appearing twice
    }

    private async Task OnSelectedCategoriesChanged(IEnumerable<string> values)
    {
        var coasterpediaSite = await WikiSite.GetCoasterpedia();
        var categories = new List<MultiSelectChip<string>>();
        foreach (var value in values)
        {
            var existingCategory = _detailsFormModel.Categories.FirstOrDefault(x => x.Value == value);
            if (existingCategory != null)
            {
                categories.Add(existingCategory);
                continue;
            }

            var categoryPage = new WikiPage(coasterpediaSite, $"Category:{value}");
            await categoryPage.RefreshAsync();

            categories.Add(new MultiSelectChip<string>
            {
                Value = value,
                Color = categoryPage.Exists ? Color.Default : Color.Error
            });
        }

        _detailsFormModel.Categories = categories;
    }

    private async Task Reset()
    {
        _urlFormModel = new UrlForm();
        _detailsFormModel = new DetailsForm();
        _processing = false;
        _thumbnailUrl = string.Empty;
        _warning = string.Empty;
        _uploadResult = null;
        _extension = string.Empty;
        _url = string.Empty;
        _wikitext = string.Empty;
        await _stepper.ResetAsync();
    }
}