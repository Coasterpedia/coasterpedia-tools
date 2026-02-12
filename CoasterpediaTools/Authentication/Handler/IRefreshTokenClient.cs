using Refit;

namespace CoasterpediaTools.Authentication.Handler;

public interface IRefreshTokenClient
{
    [Post("/oauth2/access_token")]
    public Task<UserToken> RefreshTokenAsync([Body(BodySerializationMethod.UrlEncoded)] RefreshTokenRequest request, CancellationToken ct);
}