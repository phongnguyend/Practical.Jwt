namespace Practical.Jwt.Client.Models;

public class TokenRequestModel
{
    public string GrantType { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string RefreshToken { get; set; }

    public string Scope { get; set; }
}