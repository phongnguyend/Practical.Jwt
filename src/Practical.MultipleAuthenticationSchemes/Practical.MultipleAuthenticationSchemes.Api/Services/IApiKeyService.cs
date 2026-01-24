using Practical.MultipleAuthenticationSchemes.Api.Models;

namespace Practical.MultipleAuthenticationSchemes.Api.Services;

public interface IApiKeyService
{
    Task<ApiKeyInfo?> ValidateApiKeyAsync(string apiKey);
    Task<ApiKeyInfo?> GetApiKeyInfoAsync(string keyId);
    Task<IEnumerable<ApiKeyInfo>> GetApiKeysByOwnerAsync(string ownerId);
}
