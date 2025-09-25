using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Weather Forecast Client - Entra ID Authentication (MSAL vs OAuth Standard)");
Console.WriteLine("=======================================================================");

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
    Console.WriteLine("Choose authentication method:");
    Console.WriteLine("1. MSAL (Microsoft Authentication Library)");
    Console.WriteLine("2. Standard OAuth 2.0 Client Credentials");
    Console.Write("Enter your choice (1 or 2): ");
    
    var choice = Console.ReadLine();
    string accessToken;
    
    if (choice == "2")
    {
        Console.WriteLine("\n=== Using Standard OAuth 2.0 Client Credentials Flow ===");
        accessToken = await GetTokenUsingOAuthStandardAsync(tenantId, clientId, clientSecret, apiScope);
    }
    else
    {
        Console.WriteLine("\n=== Using MSAL Confidential Client Application ===");
        accessToken = await GetTokenUsingMSALAsync(tenantId, clientId, clientSecret, apiScope);
    }

    if (string.IsNullOrEmpty(accessToken))
    {
        Console.WriteLine("✗ Failed to acquire access token");
        return;
    }

    // Create HTTP client and add authorization header
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", accessToken);

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
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

/// <summary>
/// Gets an access token using standard OAuth 2.0 Client Credentials flow
/// This method uses direct HTTP calls to the OAuth 2.0 token endpoint
/// </summary>
static async Task<string> GetTokenUsingOAuthStandardAsync(string tenantId, string clientId, string clientSecret, string scope)
{
    Console.WriteLine("Getting token using standard OAuth 2.0 Client Credentials flow...");
    
    // Debug: Show current configuration
    Console.WriteLine("Current Configuration:");
    Console.WriteLine($"  Tenant ID: {tenantId}");
    Console.WriteLine($"  Client ID: {clientId}");
    Console.WriteLine($"  Scope: {scope}");
    Console.WriteLine();

    // Validate scope format for client credentials
    if (!scope.EndsWith("/.default"))
    {
        Console.WriteLine("⚠️  WARNING: Client credentials flow requires '.default' suffix!");
        Console.WriteLine($"   Current scope: {scope}");
        Console.WriteLine("   Expected format: api://your-api-id/.default");
        Console.WriteLine("   This will likely cause authentication errors.");
        Console.WriteLine();
    }

    try
    {
        using var httpClient = new HttpClient();
        
        // OAuth 2.0 token endpoint for Azure AD
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        
        // Prepare the request body for client credentials flow
        var requestBody = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret),
            new("scope", scope)
        };

        var requestContent = new FormUrlEncodedContent(requestBody);

        Console.WriteLine($"Making OAuth 2.0 token request to: {tokenEndpoint}");
        Console.WriteLine("Request parameters:");
        Console.WriteLine($"  grant_type: client_credentials");
        Console.WriteLine($"  client_id: {clientId}");
        Console.WriteLine($"  client_secret: [REDACTED]");
        Console.WriteLine($"  scope: {scope}");
        Console.WriteLine();

        var response = await httpClient.PostAsync(tokenEndpoint, requestContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"OAuth Response Status: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse?.AccessToken != null)
            {
                Console.WriteLine("✓ Access token acquired successfully using standard OAuth 2.0");
                Console.WriteLine($"Token type: {tokenResponse.TokenType}");
                Console.WriteLine($"Expires in: {tokenResponse.ExpiresIn} seconds");
                
                if (tokenResponse.ExpiresIn > 0)
                {
                    var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    Console.WriteLine($"Token expires at: {expiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                }

                // Enhanced Debug: Show token information for troubleshooting
                Console.WriteLine();
                Console.WriteLine("🔍 Enhanced Token Debug Information:");
                Console.WriteLine($"   Token length: {tokenResponse.AccessToken.Length} characters");
                Console.WriteLine($"   Token starts with: {tokenResponse.AccessToken.Substring(0, Math.Min(30, tokenResponse.AccessToken.Length))}...");
                
                if (!string.IsNullOrEmpty(tokenResponse.Scope))
                {
                    Console.WriteLine($"   Scopes granted: {tokenResponse.Scope}");
                }

                // Show the full token for debugging (remove in production!)
                Console.WriteLine();
                Console.WriteLine("🔑 FULL ACCESS TOKEN (for debugging - decode at https://jwt.ms):");
                Console.WriteLine($"{tokenResponse.AccessToken}");
                Console.WriteLine();
                Console.WriteLine("   💡 Copy the token above and paste it at https://jwt.ms to inspect claims");
                Console.WriteLine("   🔍 Look for 'aud' (audience) and 'roles'/'scp' (permissions) claims");
                Console.WriteLine();

                return tokenResponse.AccessToken;
            }
            else
            {
                Console.WriteLine("✗ Token response did not contain access_token");
                Console.WriteLine($"Response: {responseContent}");
                return string.Empty;
            }
        }
        else
        {
            Console.WriteLine($"✗ OAuth token request failed: {response.StatusCode}");
            Console.WriteLine($"Response: {responseContent}");

            // Parse error response
            try
            {
                var errorResponse = JsonSerializer.Deserialize<OAuthErrorResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (errorResponse?.Error != null)
                {
                    Console.WriteLine($"Error: {errorResponse.Error}");
                    if (!string.IsNullOrEmpty(errorResponse.ErrorDescription))
                    {
                        Console.WriteLine($"Description: {errorResponse.ErrorDescription}");
                    }

                    // Provide specific guidance based on error
                    if (errorResponse.Error == "invalid_client")
                    {
                        Console.WriteLine("   💡 Check your client ID and client secret configuration.");
                    }
                    else if (errorResponse.Error == "invalid_scope" || errorResponse.ErrorDescription?.Contains("AADSTS1002012") == true)
                    {
                        Console.WriteLine("   🚨 CLIENT CREDENTIALS SCOPE ERROR!");
                        Console.WriteLine("   For client credentials flow, you MUST use '.default' suffix.");
                        Console.WriteLine($"   Current scope: {scope}");
                        Console.WriteLine("   ❌ WRONG: api://your-api-id/Weather.Read");
                        Console.WriteLine("   ✅ CORRECT: api://your-api-id/.default");
                    }
                    else if (errorResponse.ErrorDescription?.Contains("AADSTS500011") == true)
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
            }
            catch (JsonException)
            {
                // Error response is not valid JSON
                Console.WriteLine("Error response is not valid JSON");
            }

            return string.Empty;
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"✗ Network error during OAuth request: {ex.Message}");
        Console.WriteLine("Make sure you have internet connectivity and the OAuth endpoint is accessible.");
        return string.Empty;
    }
    catch (TaskCanceledException ex)
    {
        Console.WriteLine($"✗ OAuth request timed out: {ex.Message}");
        return string.Empty;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Unexpected error during OAuth request: {ex.Message}");
        return string.Empty;
    }
}

/// <summary>
/// Gets an access token using MSAL (Microsoft Authentication Library)
/// This is the existing implementation
/// </summary>
static async Task<string> GetTokenUsingMSALAsync(string tenantId, string clientId, string clientSecret, string scope)
{
    Console.WriteLine("Creating MSAL Confidential Client Application...");

    // Debug: Show current configuration
    Console.WriteLine("Current Configuration:");
    Console.WriteLine($"  Tenant ID: {tenantId}");
    Console.WriteLine($"  Client ID: {clientId}");
    Console.WriteLine($"  API Scope: {scope}");
    Console.WriteLine();

    // Validate scope format for client credentials
    if (!scope.EndsWith("/.default"))
    {
        Console.WriteLine("⚠️  WARNING: Client credentials flow requires '.default' suffix!");
        Console.WriteLine($"   Current scope: {scope}");
        Console.WriteLine("   Expected format: api://your-api-id/.default");
        Console.WriteLine("   This will likely cause AADSTS1002012 error.");
        Console.WriteLine();
    }

    try
    {
        // Create MSAL Confidential Client Application
        var app = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();

        Console.WriteLine("Acquiring access token using client credentials...");

        // Acquire token using client credentials flow
        var authResult = await app.AcquireTokenForClient(new[] { scope })
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

        return authResult.AccessToken;
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
            Console.WriteLine($"   Current scope: {scope}");
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
        
        return string.Empty;
    }
    catch (MsalClientException ex)
    {
        Console.WriteLine($"✗ MSAL Client Error: {ex.Message}");
        Console.WriteLine($"Error Code: {ex.ErrorCode}");
        return string.Empty;
    }
}

// OAuth token response models
public class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

public class OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("error_codes")]
    public int[]? ErrorCodes { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("trace_id")]
    public string? TraceId { get; set; }

    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; set; }
}

// Weather forecast model to match the API response
public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF { get; set; }
    public string? Summary { get; set; }
}
