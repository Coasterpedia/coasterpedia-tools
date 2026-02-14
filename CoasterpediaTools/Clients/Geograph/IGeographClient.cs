using Refit;

namespace CoasterpediaTools.Clients.Geograph;

public interface IGeographClient
{
    [Get("/api/photo/{photoId}/coasterpedia.net?format=json")]
    public Task<GeographResponse> GetPhotoAsync(string photoId);
}