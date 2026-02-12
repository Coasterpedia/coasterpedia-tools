using CoasterpediaTools.Authentication.Handler;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CoasterpediaTools.Authentication;

public class CookieEvents : CookieAuthenticationEvents
{
    private readonly TokenHandler _store;

    public CookieEvents(TokenHandler store)
    {
        _store = store;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var token = await _store.GetToken(context.Principal!);
        if (!token.Success)
        {
            context.RejectPrincipal();
        }

        await base.ValidatePrincipal(context);
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        await _store.RemoveToken(context.HttpContext.User);
        await base.SigningOut(context);
    }
}