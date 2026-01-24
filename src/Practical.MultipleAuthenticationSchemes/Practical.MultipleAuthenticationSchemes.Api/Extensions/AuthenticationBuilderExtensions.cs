using Microsoft.AspNetCore.Authentication;
using Practical.MultipleAuthenticationSchemes.Api.Authentication;

namespace Practical.MultipleAuthenticationSchemes.Api.Extensions;

public static class AuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddBasic(
        this AuthenticationBuilder builder,
        Action<BasicAuthenticationOptions>? configureOptions = null)
    {
        return builder.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
            "Basic",
            configureOptions);
    }

    public static AuthenticationBuilder AddBasic(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<BasicAuthenticationOptions>? configureOptions = null)
    {
        return builder.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
            authenticationScheme,
            configureOptions);
    }

    public static AuthenticationBuilder AddBasic(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        string? displayName,
        Action<BasicAuthenticationOptions>? configureOptions = null)
    {
        return builder.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
            authenticationScheme,
            displayName,
            configureOptions);
    }

    public static AuthenticationBuilder AddApiKey(
        this AuthenticationBuilder builder,
        Action<ApiKeyAuthenticationOptions>? configureOptions = null)
    {
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            "ApiKey",
            configureOptions);
    }

    public static AuthenticationBuilder AddApiKey(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<ApiKeyAuthenticationOptions>? configureOptions = null)
    {
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            authenticationScheme,
            configureOptions);
    }

    public static AuthenticationBuilder AddApiKey(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        string? displayName,
        Action<ApiKeyAuthenticationOptions>? configureOptions = null)
    {
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            authenticationScheme,
            displayName,
            configureOptions);
    }
}
