using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Practical.MultipleAuthenticationSchemes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    private readonly ILogger<SecureController> _logger;

    public SecureController(ILogger<SecureController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Accepts JWT Bearer, Basic, and API Key authentication
    /// </summary>
    [Authorize]
    [HttpGet("all")]
    public IActionResult GetSecureDataAll()
    {
        var username = User.Identity?.Name;
        var authType = User.Identity?.AuthenticationType;
        var apiKeyId = User.FindFirst("ApiKeyId")?.Value;
        var scopes = User.FindAll("scope").Select(c => c.Value).ToList();
        
        return Ok(new
        {
            Message = "Access granted with any authentication scheme",
            Username = username,
            AuthenticationType = authType,
            ApiKeyId = apiKeyId,
            Scopes = scopes,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Requires JWT Bearer authentication only
    /// </summary>
    [Authorize(Policy = "JwtOnly")]
    [HttpGet("jwt")]
    public IActionResult GetSecureDataJwtOnly()
    {
        var username = User.Identity?.Name;
        var authType = User.Identity?.AuthenticationType;

        return Ok(new
        {
            Message = "Access granted with JWT authentication only",
            Username = username,
            AuthenticationType = authType,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Requires Basic authentication only
    /// </summary>
    [Authorize(Policy = "BasicOnly")]
    [HttpGet("basic")]
    public IActionResult GetSecureDataBasicOnly()
    {
        var username = User.Identity?.Name;
        var authType = User.Identity?.AuthenticationType;

        return Ok(new
        {
            Message = "Access granted with Basic authentication only",
            Username = username,
            AuthenticationType = authType,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Requires API Key authentication only
    /// </summary>
    [Authorize(Policy = "ApiKeyOnly")]
    [HttpGet("apikey")]
    public IActionResult GetSecureDataApiKeyOnly()
    {
        var username = User.Identity?.Name;
        var authType = User.Identity?.AuthenticationType;
        var apiKeyId = User.FindFirst("ApiKeyId")?.Value;
        var apiKeyName = User.FindFirst("ApiKeyName")?.Value;
        var scopes = User.FindAll("scope").Select(c => c.Value).ToList();
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new
        {
            Message = "Access granted with API Key authentication only",
            Username = username,
            AuthenticationType = authType,
            ApiKeyId = apiKeyId,
            ApiKeyName = apiKeyName,
            Scopes = scopes,
            Roles = roles,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Accepts both JWT and API Key authentication
    /// </summary>
    [Authorize(Policy = "JwtOrApiKey")]
    [HttpGet("jwt-or-apikey")]
    public IActionResult GetSecureDataJwtOrApiKey()
    {
        var username = User.Identity?.Name;
        var authType = User.Identity?.AuthenticationType;
        var apiKeyId = User.FindFirst("ApiKeyId")?.Value;

        return Ok(new
        {
            Message = "Access granted with JWT or API Key authentication",
            Username = username,
            AuthenticationType = authType,
            ApiKeyId = apiKeyId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Public endpoint - no authentication required
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult GetPublicData()
    {
        return Ok(new
        {
            Message = "This is public data, no authentication required",
            Timestamp = DateTime.UtcNow
        });
    }
}
