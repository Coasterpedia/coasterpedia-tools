using System.ComponentModel.DataAnnotations;
using CoasterpediaTools.Clients.Wiki;
using CoasterpediaTools.Components.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WikiClientLibrary.Files;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Queries;
using WikiClientLibrary.Pages.Queries.Properties;
using WikiClientLibrary.Sites;

namespace CoasterpediaTools.Components.Pages;

public partial class Transfer
{
    [Inject] WikiSiteAccessor WikiSite { get; init; }

    private UrlForm _urlFormModel = new();
    private DetailsForm _detailsFormModel = new();
    private MudStepper _stepper = new();
    private bool _processing;
    private WikiPage _fileInfo;
    private string _thumbnailUrl;
    private string _warning = string.Empty;
    private UploadResult _uploadResult;
    private string _extension = string.Empty;

    private class UrlForm
    {
        [Required] public string Url { get; set; }
    }

    private class DetailsForm
    {
        [Required] public string Title { get; set; }

        [Required] public string Description { get; set; }

        [Required] public List<MultiSelectChip<string>> Categories { get; set; } = [];
    }

    private async Task GetPhotos()
    {
        _processing = true;
        _warning = string.Empty;
        var commonsSite = await WikiSite.GetCommons();
        if (!Uri.IsWellFormedUriString(_urlFormModel.Url, UriKind.Absolute) ||
            !Uri.TryCreate(_urlFormModel.Url, UriKind.Absolute, out var uri) ||
            uri.Host != "commons.wikimedia.org" ||
            !uri.AbsolutePath.StartsWith("/wiki/File:"))
        {
            _warning = "Invalid URL.";
            _processing = false;
            return;
        }

        var filename = uri.AbsolutePath[11..];

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
            _processing = false;
            return;
        }

        _fileInfo = page;
        _thumbnailUrl = _fileInfo.GetPropertyGroup<PageImagesPropertyGroup>().ThumbnailImage.Url;

        var coasterpediaSite = await WikiSite.GetCoasterpedia();
        _uploadResult = await coasterpediaSite.UploadAsync(_fileInfo.Title,
            new ExternalFileStashSource(_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.Url), "", false);

        if (_uploadResult.Warnings.Count > 0)
        {
            _warning = _uploadResult.Warnings.ToString();
            _processing = false;
            return;
        }

        _extension = Path.GetExtension(filename);
        _detailsFormModel.Title = filename[..^_extension.Length].Replace('_', ' ');

        _processing = false;
        await _stepper.NextStepAsync();
    }

    private async Task Upload()
    {
        _processing = true;
        _warning = string.Empty;
        var coasterpediaSite = await WikiSite.GetCoasterpedia();
        var newExtension = Path.GetExtension(_detailsFormModel.Title);
        var uploadTitle = _detailsFormModel.Title[..^newExtension.Length];
        var comment = $$$"""
                         =={{int:filedesc}}==
                         {{Information
                         |description={{{_detailsFormModel.Description}}}
                         |date={{{_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.ExtMetadata["DateTime"].Value}}}
                         |source={{{_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.DescriptionUrl}}}
                         |author={{{_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.UserName}}}
                         |permission=
                         |other versions=
                         }}

                         """;

        if (_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.ExtMetadata.ContainsKey("GPSLatitude"))
        {
            comment +=
                $$$"""{{Location|{{{_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.ExtMetadata["GPSLatitude"].Value}}}|{{{_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.ExtMetadata["GPSLongitude"].Value}}}|}}""";
        }

        comment += $$$"""
                      =={{int:license-header}}==
                      {{{{{_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.ExtMetadata["License"].Value}}}}}
                      {{Wikimedia Commons|{{{_fileInfo.GetPropertyGroup<FileInfoPropertyGroup>().LatestRevision.DescriptionUrl}}}}}

                      """;

        foreach (var category in _detailsFormModel.Categories)
        {
            comment += $"[[Category:{category.Value}]]\n";
        }

        var result = await coasterpediaSite.UploadAsync(uploadTitle + _extension, new FileKeyUploadSource(_uploadResult.FileKey), comment, false);
        if (result.Warnings.Count > 0)
        {
            _warning = result.Warnings.ToString();
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

        return new[] { value }.Concat(result.Select(x => x.Title[9..]).Where(x => x != value));
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
        _fileInfo = null;
        _thumbnailUrl = string.Empty;
        _warning = string.Empty;
        _uploadResult = null;
        _extension = string.Empty;
        await _stepper.ResetAsync();
    }
}