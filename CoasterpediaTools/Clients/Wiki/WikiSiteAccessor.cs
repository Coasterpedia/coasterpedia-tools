using CoasterpediaTools.Options;
using Microsoft.Extensions.Options;
using WikiClientLibrary.Sites;

namespace CoasterpediaTools.Clients.Wiki;

public class WikiSiteAccessor
{
    private readonly IOptions<CoasterpediaConfig> _coasterpediaConfig;
    private readonly CoasterpediaClient _coasterpediaClient;
    private readonly CommonsClient _commonsClient;
    private WikiSite? _coasterpediaSite;
    private WikiSite? _commonsSite;

    public WikiSiteAccessor(CoasterpediaClient coasterpediaClient, CommonsClient commonsClient, IOptions<CoasterpediaConfig> coasterpediaConfig)
    {
        _coasterpediaClient = coasterpediaClient;
        _commonsClient = commonsClient;
        _coasterpediaConfig = coasterpediaConfig;
    }

    public async Task<WikiSite> GetCoasterpedia()
    {
        if (_coasterpediaSite == null)
        {
            _coasterpediaSite = new WikiSite(_coasterpediaClient.GetSite(), _coasterpediaConfig.Value.BaseUrl + "/w/api.php");
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