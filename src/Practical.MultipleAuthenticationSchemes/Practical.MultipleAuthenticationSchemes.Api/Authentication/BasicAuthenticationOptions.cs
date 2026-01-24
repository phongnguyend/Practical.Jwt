using Microsoft.AspNetCore.Authentication;

namespace Practical.MultipleAuthenticationSchemes.Api.Authentication;

public class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Realm { get; set; } = "Practical.MultipleAuthenticationSchemes.Api";
}
