namespace CoasterpediaTools.Clients.Wiki;

public class CoasterpediaClient
{
    private readonly HttpClient _client;

    public CoasterpediaClient(HttpClient client)
    {
        _client = client;
    }

    public WikiClient GetSite() => new(_client);
}