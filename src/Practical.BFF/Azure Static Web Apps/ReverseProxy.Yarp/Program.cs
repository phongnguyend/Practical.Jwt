using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ReverseProxy.Yarp.ConfigurationOptions;
using ReverseProxy.Yarp.Middleware;
using System.Net;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

var appSettings = new AppSettings();
configuration.Bind(appSettings);

// Add the reverse proxy to capability to the server
var proxyBuilder = builder.Services.AddReverseProxy();

// Initialize the reverse proxy from the "ReverseProxy" section of configuration
proxyBuilder.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddRequestTransform(async transformContext =>
        {
            var user = transformContext.HttpContext.User;
            var token = await transformContext.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
    });

services.AddControllers();

services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.AccessDeniedPath = "/Authorization/AccessDenied";
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.CorrelationCookie.Path = "/";
    options.NonceCookie.Path = "/";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.Authority = appSettings.OpenIdConnect?.Authority;
    options.ClientId = appSettings.OpenIdConnect?.ClientId;
    options.ClientSecret = appSettings.OpenIdConnect?.ClientSecret;
    options.ResponseType = "code";
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("offline_access");
    options.Scope.Add("api://5a08cfb4-0904-4733-b50b-134798fe373b/Full");
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = false;

    if (appSettings.OpenIdConnect?.RequireHttpsMetadata != null)
    {
        options.RequireHttpsMetadata = appSettings.OpenIdConnect.RequireHttpsMetadata;
    }

    if (!string.IsNullOrEmpty(appSettings.OpenIdConnect?.CallbackPath))
    {
        options.CallbackPath = appSettings.OpenIdConnect.CallbackPath;
    }

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            if (!string.IsNullOrEmpty(appSettings.OpenIdConnect?.RedirectUri))
            {
                context.ProtocolMessage.RedirectUri = appSettings.OpenIdConnect?.RedirectUri;
            }
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            if (!string.IsNullOrEmpty(appSettings.OpenIdConnect?.PostLogoutRedirectUri))
            {
                context.ProtocolMessage.PostLogoutRedirectUri = appSettings.OpenIdConnect?.PostLogoutRedirectUri;
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Add Response Cookie Logging Middleware
app.UseMiddleware<ResponseCookieLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.StartsWith("/api/data/", StringComparison.OrdinalIgnoreCase) ?? false)
    {
        try
        {
            var antiForgeryService = context.RequestServices.GetRequiredService<IAntiforgery>();
            await antiForgeryService.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }
    }

    await next(context);
});

app.MapReverseProxy();

app.Run();