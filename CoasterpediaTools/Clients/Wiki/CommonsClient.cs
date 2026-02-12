namespace CoasterpediaTools.Clients.Wiki;

public class CommonsClient
{
    private readonly HttpClient _client;

    public CommonsClient(HttpClient client)
    {
        _client = client;
    }

    public WikiClient GetSite() => new(_client);
}