using Microsoft.Identity.Client;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;

Console.WriteLine("Weather Forecast Client - Entra ID Authentication (MSAL)");
Console.WriteLine("======================================================");

// Load configuration from appsettings.json and user secrets
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .Build();

// Azure AD Configuration - Try to load from config, fallback to constants
var tenantId = configuration["AzureAd:TenantId"];
var clientId = configuration["AzureAd:ClientId"];
var clientSecret = configuration["AzureAd:ClientSecret"];
var apiScope = configuration["ApiSettings:Scope"];

// API Configuration
var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

try
{
    Console.WriteLine("Creating MSAL Confidential Client Application...");

    // Debug: Show current configuration
    Console.WriteLine("Current Configuration:");
    Console.WriteLine($"  Tenant ID: {tenantId}");
    Console.WriteLine($"  Client ID: {clientId}");
    Console.WriteLine($"  API Scope: {apiScope}");
    Console.WriteLine($"  API Base URL: {apiBaseUrl}");
    Console.WriteLine();

    // Validate scope format for client credentials
    if (!apiScope.EndsWith("/.default"))
    {
        Console.WriteLine("⚠️  WARNING: Client credentials flow requires '.default' suffix!");
        Console.WriteLine($"   Current scope: {apiScope}");
        Console.WriteLine("   Expected format: api://your-api-id/.default");
        Console.WriteLine("   This will likely cause AADSTS1002012 error.");
        Console.WriteLine();
    }

    // Create MSAL Confidential Client Application
    var app = ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithClientSecret(clientSecret)
        .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
        .Build();

    Console.WriteLine("Acquiring access token using client credentials...");

    // Acquire token using client credentials flow
    var authResult = await app.AcquireTokenForClient(new[] { apiScope })
        .ExecuteAsync();

    Console.WriteLine("✓ Access token acquired successfully using MSAL");
    Console.WriteLine($"Token expires at: {authResult.ExpiresOn:yyyy-MM-dd HH:mm:ss} UTC");

    // Enhanced Debug: Show token information for troubleshooting
    Console.WriteLine("🔍 Enhanced Token Debug Information:");
    Console.WriteLine($"   Token length: {authResult.AccessToken.Length} characters");
    Console.WriteLine($"   Token starts with: {authResult.AccessToken.Substring(0, Math.Min(30, authResult.AccessToken.Length))}...");
    Console.WriteLine($"   Scopes granted: {string.Join(", ", authResult.Scopes)}");
    Console.WriteLine($"   Account: {authResult.Account?.Username ?? "Service Account"}");
    Console.WriteLine($"   Token Type: {authResult.TokenType}");

    // Show the full token for debugging (remove in production!)
    Console.WriteLine();
    Console.WriteLine("🔑 FULL ACCESS TOKEN (for debugging - decode at https://jwt.ms):");
    Console.WriteLine($"{authResult.AccessToken}");
    Console.WriteLine();
    Console.WriteLine("   💡 Copy the token above and paste it at https://jwt.ms to inspect claims");
    Console.WriteLine("   🔍 Look for 'aud' (audience) and 'roles'/'scp' (permissions) claims");
    Console.WriteLine();

    // Create HTTP client and add authorization header
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

    Console.WriteLine("Calling Weather Forecast API...");

    // Call the Weather Forecast API
    var response = await httpClient.GetAsync($"{apiBaseUrl}/WeatherForecast");

    Console.WriteLine($"Response Status: {response.StatusCode}");

    if (response.IsSuccessStatusCode)
    {
        var weatherForecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();

        Console.WriteLine("✓ Weather forecast data received:");
        Console.WriteLine();

        if (weatherForecasts != null)
        {
            foreach (var forecast in weatherForecasts)
            {
                Console.WriteLine($"📅 Date: {forecast.Date:yyyy-MM-dd}");
                Console.WriteLine($"🌡️  Temperature: {forecast.TemperatureC}°C ({forecast.TemperatureF}°F)");
                Console.WriteLine($"☁️  Summary: {forecast.Summary}");
                Console.WriteLine("---");
            }
        }
        else
        {
            Console.WriteLine("No weather data received.");
        }
    }
    else
    {
        Console.WriteLine($"✗ API call failed: {response.StatusCode}");
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Error details: {errorContent}");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine();
            Console.WriteLine("🔐 Authentication failed!");
            Console.WriteLine("   Possible causes:");
            Console.WriteLine("   - Invalid client credentials");
            Console.WriteLine("   - Token audience/scope mismatch");
            Console.WriteLine("   - API configuration issues");
        }
    }
}
catch (MsalServiceException ex)
{
    Console.WriteLine($"✗ MSAL Service Error: {ex.Message}");
    Console.WriteLine($"Error Code: {ex.ErrorCode}");

    if (ex.ErrorCode == "invalid_client")
    {
        Console.WriteLine("   Check your client ID and client secret configuration.");
    }
    else if (ex.ErrorCode == "invalid_scope" || ex.Message.Contains("AADSTS1002012"))
    {
        Console.WriteLine("   🚨 CLIENT CREDENTIALS SCOPE ERROR!");
        Console.WriteLine("   For client credentials flow, you MUST use '.default' suffix.");
        Console.WriteLine($"   Current scope: {apiScope}");
        Console.WriteLine("   ❌ WRONG: api://your-api-id/Weather.Read");
        Console.WriteLine("   ✅ CORRECT: api://your-api-id/.default");
        Console.WriteLine("   Please update your appsettings.json file.");
    }
    else if (ex.Message.Contains("AADSTS500011"))
    {
        Console.WriteLine("   🚨 API SCOPE NOT FOUND ERROR!");
        Console.WriteLine("   The API app registration doesn't expose the requested scope.");
        Console.WriteLine("   1. Go to Azure AD → App registrations → Your API app");
        Console.WriteLine("   2. Go to 'Expose an API' section");
        Console.WriteLine("   3. Set Application ID URI if not set");
        Console.WriteLine("   4. Add scopes if needed");
        Console.WriteLine("   5. Grant permissions in client app registration");
    }
}
catch (MsalClientException ex)
{
    Console.WriteLine($"✗ MSAL Client Error: {ex.Message}");
    Console.WriteLine($"Error Code: {ex.ErrorCode}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"✗ Network error: {ex.Message}");
    Console.WriteLine("Make sure the API is running on the specified URL.");
    Console.WriteLine($"Expected API URL: {apiBaseUrl}/WeatherForecast");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Unexpected error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine();
Console.WriteLine("Configuration Check:");
Console.WriteLine($"  Tenant ID: {(tenantId.Contains("your-tenant") ? "❌ Not configured" : "✓ Configured")}");
Console.WriteLine($"  Client ID: {(clientId.Contains("your-client") ? "❌ Not configured" : "✓ Configured")}");
Console.WriteLine($"  Client Secret: {(clientSecret.Contains("your-client") ? "❌ Not configured" : "✓ Configured")}");
Console.WriteLine($"  API Scope: {(apiScope.Contains("your-api") ? "❌ Not configured" : "✓ Configured")}");

Console.WriteLine();
Console.WriteLine("MSAL Features Used:");
Console.WriteLine("  ✓ ConfidentialClientApplication for client credentials flow");
Console.WriteLine("  ✓ Automatic token caching (tokens are cached by MSAL)");
Console.WriteLine("  ✓ Built-in retry logic for transient failures");
Console.WriteLine("  ✓ Comprehensive error handling with specific error codes");
Console.WriteLine("  ✓ Configuration loading from appsettings.json");

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

// Weather forecast model to match the API response
public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF { get; set; }
    public string? Summary { get; set; }
}
