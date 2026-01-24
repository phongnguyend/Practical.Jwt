using Microsoft.AspNetCore.Authentication;

namespace Practical.MultipleAuthenticationSchemes.Api.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultHeaderName = "X-API-Key";
    
    public string HeaderName { get; set; } = DefaultHeaderName;
    public string Realm { get; set; } = "Practical.MultipleAuthenticationSchemes.Api";
}
