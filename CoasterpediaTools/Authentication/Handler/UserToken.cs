namespace CoasterpediaTools.Authentication.Handler;

public record UserToken
{
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
}