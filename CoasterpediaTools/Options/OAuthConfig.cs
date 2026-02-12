namespace CoasterpediaTools.Options;

public record OAuthConfig
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}