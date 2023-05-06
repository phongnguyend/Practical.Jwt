using System.Text.Json.Serialization;

namespace Practical.Jwt.Api.Models;

public class TokenRequestModel
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; }

    [JsonPropertyName("username")]
    public string UserName { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; }
}