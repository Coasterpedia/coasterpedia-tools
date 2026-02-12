using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoasterpediaTools.Authentication;

[AllowAnonymous]
public class AccountController : ControllerBase
{
    public IActionResult LogIn(string? returnUrl)
    {
        var redirectUri = "/";

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                redirectUri = returnUrl;
            }
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };

        return Challenge(props);
    }

    public async Task<IActionResult> LogOut()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}