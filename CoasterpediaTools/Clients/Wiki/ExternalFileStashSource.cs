using WikiClientLibrary.Files;
using WikiClientLibrary.Sites;

namespace CoasterpediaTools.Clients.Wiki;

/// <summary>
/// Uploadable content identified by external file URL.
/// </summary>
/// <remarks>
/// Note that not all the Mediawiki sites allow uploading by external file URL.
/// Especially, Wikimedia and Wikia sites does not allow this.
/// </remarks>
public class ExternalFileStashSource : WikiUploadSource
{

    /// <param name="sourceUrl">The URL of the file to be uploaded.</param>
    public ExternalFileStashSource(string sourceUrl)
    {
        SourceUrl = sourceUrl;
    }

    /// <summary>The URL of the file to be uploaded.</summary>
    public virtual string SourceUrl { get; }

    /// <inheritdoc />
    public override IEnumerable<KeyValuePair<string, object>> GetUploadParameters(SiteInfo siteInfo)
    {
        if (SourceUrl == null) throw new ArgumentNullException(nameof(SourceUrl));
        return [new KeyValuePair<string, object>("url", SourceUrl), new KeyValuePair<string, object>("stash", true)];
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "ExternalFileUploadSource(" + SourceUrl + ")";
    }

}