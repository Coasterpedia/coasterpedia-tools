using System.Net.Http.Headers;
using CoasterpediaTools.Authentication.Handler;

namespace CoasterpediaTools.Authentication;

public class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenHandler _tokenHandler;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor, TokenHandler tokenHandler)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenHandler = tokenHandler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenHandler.GetToken(_httpContextAccessor.HttpContext.User, cancellationToken);
        if (token is { Success: true, Token: not null })
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}