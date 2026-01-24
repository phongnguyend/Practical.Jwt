namespace Practical.MultipleAuthenticationSchemes.Api.Models;

public class ApiKeyInfo
{
    public string KeyId { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public List<string>? Scopes { get; set; }
    public List<string>? Roles { get; set; }
}
