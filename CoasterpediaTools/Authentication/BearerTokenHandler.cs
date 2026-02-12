using System.Net.Http.Headers;
using CoasterpediaTools.Authentication.Handler;
using Microsoft.AspNetCore.Components.Authorization;

namespace CoasterpediaTools.Authentication;

public class BearerTokenHandler : DelegatingHandler
{
    private readonly CircuitServicesAccessor _circuitServicesAccessor;
    private readonly TokenHandler _tokenHandler;

    public BearerTokenHandler(CircuitServicesAccessor circuitServicesAccessor, TokenHandler tokenHandler)
    {
        _circuitServicesAccessor = circuitServicesAccessor;
        _tokenHandler = tokenHandler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authStateProvider = _circuitServicesAccessor.Services!.GetRequiredService<AuthenticationStateProvider>();
        var authState = await authStateProvider.GetAuthenticationStateAsync();        
        var token = await _tokenHandler.GetToken(authState.User, cancellationToken);
        if (token is { Success: true, Token: not null })
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}