using System;
using System.Text.Json.Serialization;

namespace Practical.Jwt.Client.Models;

public class TokenResponseModel
{
    [JsonPropertyName(".expires")]
    public DateTime Expires { get; set; }

    [JsonPropertyName(".issued")]
    public DateTime Issued { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    public string Error { get; set; }

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; }
}
