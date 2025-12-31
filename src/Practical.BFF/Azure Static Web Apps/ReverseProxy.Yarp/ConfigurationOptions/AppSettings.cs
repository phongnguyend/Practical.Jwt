using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace ReverseProxy.Yarp.ConfigurationOptions;

public class AppSettings
{
    public ExtendedOpenIdConnectOptions? OpenIdConnect { get; set; }
}

public class ExtendedOpenIdConnectOptions: OpenIdConnectOptions
{
    public string? RedirectUri { get; set; }

    public string? PostLogoutRedirectUri { get; set; }
}
