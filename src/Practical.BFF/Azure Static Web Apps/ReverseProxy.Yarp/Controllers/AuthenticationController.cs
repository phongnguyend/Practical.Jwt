using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ReverseProxy.Yarp.Controllers;

public class AuthenticationController : ControllerBase
{
    private readonly IAntiforgery _forgeryService;

    public AuthenticationController(IAntiforgery forgeryService)
    {
        _forgeryService = forgeryService;
    }

    [HttpGet("/api/login")]
    public async Task LoginAsync(string returnUrl)
    {
        Response.Cookies.Append("PHONG-Test", "N",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(5),
                //Path = "/api/signin-oidc",
                SameSite = SameSiteMode.None
            });
        if (HttpContext.User.Identity?.IsAuthenticated ?? false)
        {
            Response.Redirect(Url.Content("~/").ToString());
        }
        else
        {
            await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/"
            });
        }
    }

    [HttpGet("/api/logout")]
    public async Task LogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("/api/userinfor")]
    public async Task<IActionResult> UserInforAsync()
    {
        if (HttpContext.User.Identity?.IsAuthenticated ?? false)
        {
            var tokens = _forgeryService.GetAndStoreTokens(HttpContext);
            HttpContext.Response.Cookies.Append("PHONG-XSRF-TOKEN", tokens.RequestToken!, new CookieOptions { HttpOnly = false });

            return Ok(new
            {
                Id = "",
                FirstName = "Phong",
                LastName = "Nguyen",
                IdentityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken),
                AccessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken),
                RefreshToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken),
                ExpiresAt = await HttpContext.GetTokenAsync("expires_at"),
                Claims = HttpContext.User.Claims.Select(c => new { c.Type, c.Value })
            });
        }
        else
        {
            return Unauthorized();
        }
    }
}
