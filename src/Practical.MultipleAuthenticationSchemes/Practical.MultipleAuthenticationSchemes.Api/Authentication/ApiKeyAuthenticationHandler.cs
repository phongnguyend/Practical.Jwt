using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Practical.MultipleAuthenticationSchemes.Api.Services;

namespace Practical.MultipleAuthenticationSchemes.Api.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyService apiKeyService)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if API Key header exists
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.Fail($"Missing {Options.HeaderName} Header");
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.Fail($"Invalid {Options.HeaderName} Header");
        }

        try
        {
            // Validate API Key
            var apiKeyInfo = await _apiKeyService.ValidateApiKeyAsync(providedApiKey);
            if (apiKeyInfo == null)
            {
                return AuthenticateResult.Fail("Invalid API Key");
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, apiKeyInfo.OwnerId),
                new Claim(ClaimTypes.Name, apiKeyInfo.OwnerName),
                new Claim("ApiKeyId", apiKeyInfo.KeyId),
                new Claim("ApiKeyName", apiKeyInfo.KeyName ?? string.Empty)
            };

            // Add custom scopes/roles if provided
            if (apiKeyInfo.Scopes != null)
            {
                foreach (var scope in apiKeyInfo.Scopes)
                {
                    claims.Add(new Claim("scope", scope));
                }
            }

            if (apiKeyInfo.Roles != null)
            {
                foreach (var role in apiKeyInfo.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating API Key");
            return AuthenticateResult.Fail("Error validating API Key");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers["WWW-Authenticate"] = $"ApiKey realm=\"{Options.Realm}\"";
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}
