using Refit;

namespace CoasterpediaTools.Authentication.Handler;

public record RefreshTokenRequest
{
    public RefreshTokenRequest(string RefreshToken, string GrantType = "refresh_token")
    {
        this.RefreshToken = RefreshToken;
        this.GrantType = GrantType;
    }

    [AliasAs("refresh_token")] public string RefreshToken { get; init; }

    [AliasAs("grant_type")] public string GrantType { get; init; }
}