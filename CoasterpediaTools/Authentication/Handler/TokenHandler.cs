using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Hybrid;
using Refit;

namespace CoasterpediaTools.Authentication.Handler;

public class TokenHandler
{
    private readonly IRefreshTokenClient _refreshTokenClient;
    private readonly HybridCache _cache;

    public TokenHandler(IRefreshTokenClient refreshTokenClient, HybridCache cache)
    {
        _refreshTokenClient = refreshTokenClient;
        _cache = cache;
    }

    public async Task<TokenResult> GetToken(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var sub = GetSub(principal);
        if (sub is null) return new TokenResult(false);

        return await _cache.GetOrCreateAsync<TokenResult>(TokenKey(sub), async cancel => await RefreshToken(sub, cancel), TokenOptions,
            cancellationToken: cancellationToken);
    }

    public async Task SetToken(ClaimsPrincipal principal, UserToken token, CancellationToken cancellationToken = default)
    {
        var sub = GetSub(principal);
        if (sub is null) return;

        await _cache.SetAsync(RefreshKey(sub), new TokenResult(true, token), RefreshOptions, cancellationToken: cancellationToken);

        await _cache.SetAsync(TokenKey(sub), new TokenResult(true, token), TokenOptions, cancellationToken: cancellationToken);
    }

    public async Task RemoveToken(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var sub = GetSub(principal);
        if (sub is null) return;

        await RemoveToken(sub, cancellationToken);
    }

    private async Task<TokenResult> RefreshToken(string sub, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _cache.GetOrCreateAsync<TokenResult>(RefreshKey(sub), static _ => new ValueTask<TokenResult>(new TokenResult(false)),
            new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite },
            cancellationToken: cancellationToken);

        if (refreshToken.Token is null) return refreshToken;

        try
        {
            var result = await _refreshTokenClient.RefreshTokenAsync(new RefreshTokenRequest(refreshToken.Token.RefreshToken), cancellationToken);
            await _cache.SetAsync(RefreshKey(sub), new TokenResult(true, result), RefreshOptions, cancellationToken: cancellationToken);

            return new TokenResult(true, result);
        }
        catch (ApiException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            await RemoveToken(sub, cancellationToken);
            return new TokenResult(false);
        }
    }

    private async Task RemoveToken(string sub, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(TokenKey(sub), cancellationToken);
        await _cache.RemoveAsync(RefreshKey(sub), cancellationToken);
    }

    private static string? GetSub(ClaimsPrincipal principal) => principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

    private static string TokenKey(string sub) => $"{sub}-token";

    private static string RefreshKey(string sub) => $"{sub}-refresh";

    private static HybridCacheEntryOptions TokenOptions => new()
    {
        Expiration = TimeSpan.FromHours(1)
    };

    private static HybridCacheEntryOptions RefreshOptions => new()
    {
        Expiration = TimeSpan.FromDays(28)
    };
}