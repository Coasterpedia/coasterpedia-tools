using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace CoasterpediaTools.Authentication.Scheme;

public class MediawikiAuthenticationOptions : OAuthOptions
{
    public MediawikiAuthenticationOptions()
    {
        ClaimsIssuer = MediawikiAuthenticationDefaults.Issuer;
        CallbackPath = MediawikiAuthenticationDefaults.CallbackPath;

        AuthorizationEndpoint = MediawikiAuthenticationDefaults.AuthorizationEndpoint;
        TokenEndpoint = MediawikiAuthenticationDefaults.TokenEndpoint;
        UserInformationEndpoint = MediawikiAuthenticationDefaults.UserInformationEndpoint;
        
        Scope.Add("basic");
        Scope.Add("uploadeditmovefile");
        Scope.Add("createeditmovepage");
        
        ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
    }
}