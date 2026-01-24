using Practical.MultipleAuthenticationSchemes.Api.Models;

namespace Practical.MultipleAuthenticationSchemes.Api.Services;

public class ApiKeyService : IApiKeyService
{
    // In-memory API Key store for demonstration purposes
    // In a real application, this would query a database
    private static readonly List<ApiKeyInfo> _apiKeys = new()
    {
        new ApiKeyInfo
        {
            KeyId = "key-1",
            KeyName = "Development Key",
            OwnerId = "1",
            OwnerName = "admin",
            ApiKey = "dev-api-key-12345",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            IsActive = true,
            Scopes = new List<string> { "read", "write", "admin" },
            Roles = new List<string> { "Administrator" }
        },
        new ApiKeyInfo
        {
            KeyId = "key-2",
            KeyName = "Production Key",
            OwnerId = "2",
            OwnerName = "user",
            ApiKey = "prod-api-key-67890",
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            ExpiresAt = DateTime.UtcNow.AddMonths(6),
            IsActive = true,
            Scopes = new List<string> { "read", "write" },
            Roles = new List<string> { "User" }
        },
        new ApiKeyInfo
        {
            KeyId = "key-3",
            KeyName = "Read-Only Key",
            OwnerId = "3",
            OwnerName = "readonly",
            ApiKey = "readonly-api-key-11111",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            ExpiresAt = DateTime.UtcNow.AddMonths(3),
            IsActive = true,
            Scopes = new List<string> { "read" },
            Roles = new List<string> { "Reader" }
        },
        new ApiKeyInfo
        {
            KeyId = "key-4",
            KeyName = "Expired Key",
            OwnerId = "1",
            OwnerName = "admin",
            ApiKey = "expired-api-key-99999",
            CreatedAt = DateTime.UtcNow.AddDays(-90),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsActive = false,
            Scopes = new List<string> { "read" },
            Roles = new List<string> { "User" }
        }
    };

    public Task<ApiKeyInfo?> ValidateApiKeyAsync(string apiKey)
    {
        var keyInfo = _apiKeys.FirstOrDefault(k => 
            k.ApiKey == apiKey && 
            k.IsActive && 
            (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow));

        return Task.FromResult(keyInfo);
    }

    public Task<ApiKeyInfo?> GetApiKeyInfoAsync(string keyId)
    {
        var keyInfo = _apiKeys.FirstOrDefault(k => k.KeyId == keyId);
        return Task.FromResult(keyInfo);
    }

    public Task<IEnumerable<ApiKeyInfo>> GetApiKeysByOwnerAsync(string ownerId)
    {
        var keys = _apiKeys.Where(k => k.OwnerId == ownerId).ToList();
        return Task.FromResult<IEnumerable<ApiKeyInfo>>(keys);
    }
}
