using WikiClientLibrary.Sites;

namespace CoasterpediaTools.Clients.Wiki;

public class WikiSiteAccessor
{
    private readonly CoasterpediaClient _coasterpediaClient;
    private readonly CommonsClient _commonsClient;
    private WikiSite? _coasterpediaSite;
    private WikiSite? _commonsSite;

    public WikiSiteAccessor(CoasterpediaClient coasterpediaClient, CommonsClient commonsClient)
    {
        _coasterpediaClient = coasterpediaClient;
        _commonsClient = commonsClient;
    }

    public async Task<WikiSite> GetCoasterpedia()
    {
        if (_coasterpediaSite == null)
        {
            _coasterpediaSite = new WikiSite(_coasterpediaClient.GetSite(), "https://coasterpedia.net/w/api.php");
            await _coasterpediaSite.Initialization;
        }

        return _coasterpediaSite;
    }
    
    public async Task<WikiSite> GetCommons()
    {
        if (_commonsSite == null)
        {
            _commonsSite = new WikiSite(_commonsClient.GetSite(), "https://commons.wikimedia.org/w/api.php");
            await _commonsSite.Initialization;
        }

        return _commonsSite;
    }
}