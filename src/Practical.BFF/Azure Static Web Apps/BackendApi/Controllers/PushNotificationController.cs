using BackendApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.NotificationHubs;
using System.Text.Json;

namespace BackendApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PushNotificationController : ControllerBase
{
    private readonly IOneSignalService _oneSignalService;
    private readonly IAzureNotificationHubService _azureNotificationHubService;
    private readonly IFirebaseService _firebaseService;
    private readonly ILogger<PushNotificationController> _logger;
    private readonly NotificationHubClient? _hubClient;

    public PushNotificationController(
        IOneSignalService oneSignalService,
        IAzureNotificationHubService azureNotificationHubService,
        IFirebaseService firebaseService,
        ILogger<PushNotificationController> logger,
        IConfiguration configuration)
    {
        _oneSignalService = oneSignalService;
        _azureNotificationHubService = azureNotificationHubService;
        _firebaseService = firebaseService;
        _logger = logger;

        // Initialize NotificationHubClient for registration endpoints
        var connectionString = configuration["AzureNotificationHub:ConnectionString"];
        var hubName = configuration["AzureNotificationHub:HubName"];
        if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(hubName))
        {
            _hubClient = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);
        }
    }

    /// <summary>
    /// Register browser push subscription with Azure Notification Hub
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register/browser")]
    public async Task<IActionResult> RegisterBrowserPushSubscription([FromBody] BrowserPushRegistration registration)
    {
        if (_hubClient == null)
        {
            return BadRequest(new { error = "Azure Notification Hub is not configured" });
        }

        try
        {
            _logger.LogInformation("Registering browser push subscription for user: {UserId}", registration.UserId);

            var installation = new Installation
            {
                InstallationId = $"browser-{registration.UserId}-{Math.Abs(registration.Subscription.Endpoint.GetHashCode())}",
                Platform = NotificationPlatform.Wns, // Use WNS as placeholder for web push
                PushChannel = registration.Subscription.Endpoint,
                Tags = new List<string> { $"ExternalId:{registration.UserId}" }
            };

            // Store browser-specific push subscription details as templates
            var webPushTemplate = new InstallationTemplate
            {
                Body = JsonSerializer.Serialize(new
                {
                    endpoint = registration.Subscription.Endpoint,
                    keys = new
                    {
                        p256dh = registration.Subscription.Keys.P256dh,
                        auth = registration.Subscription.Keys.Auth
                    }
                })
            };

            installation.Templates = new Dictionary<string, InstallationTemplate>
            {
                { "webPushTemplate", webPushTemplate }
            };

            await _hubClient.CreateOrUpdateInstallationAsync(installation);

            _logger.LogInformation("Successfully registered browser push subscription for user: {UserId}", registration.UserId);

            return Ok(new { success = true, installationId = installation.InstallationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register browser push subscription for user: {UserId}", registration.UserId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Unregister browser push subscription
    /// </summary>
    [AllowAnonymous]
    [HttpDelete("register/browser/{userId}")]
    public async Task<IActionResult> UnregisterBrowserPushSubscription(string userId, [FromBody] string endpoint)
    {
        if (_hubClient == null)
        {
            return BadRequest(new { error = "Azure Notification Hub is not configured" });
        }

        try
        {
            var installationId = $"browser-{userId}-{endpoint.GetHashCode()}";
            
            _logger.LogInformation("Unregistering browser push subscription: {InstallationId}", installationId);
            
            await _hubClient.DeleteInstallationAsync(installationId);
            
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister browser push subscription");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Send push notification using OneSignal to a specific external user ID
    /// </summary>
    [AllowAnonymous]
    [HttpGet("send/onesignal")]
    public async Task<ActionResult<PushNotificationResponse>> SendNotificationViaOneSignal(
        string externalId, 
        string message,
        string? title = null)
    {
        _logger.LogInformation("Sending OneSignal notification to {ExternalId}", externalId);

        var response = await _oneSignalService.SendNotificationToExternalIdAsync(new PushNotificationRequest
        {
            ExternalId = externalId,
            Title = title ?? "Notification Title",
            Message = message,
            Url = "http://localhost:3000/products"
        });

        if (response.Success)
        {
            return Ok(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// Send push notification using Azure Notification Hub to browsers
    /// </summary>
    [AllowAnonymous]
    [HttpGet("send/azure")]
    public async Task<ActionResult<PushNotificationResponse>> SendNotificationViaAzure(
        string externalId, 
        string message,
        string? title = null)
    {
        _logger.LogInformation("Sending Azure Notification Hub (Browser) notification to {ExternalId}", externalId);

        var response = await _azureNotificationHubService.SendNotificationToUserAsync(new PushNotificationRequest
        {
            ExternalId = externalId,
            Title = title ?? "Notification Title",
            Message = message,
            Url = "http://localhost:3000/products"
        });

        if (response.Success)
        {
            return Ok(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// Send push notification using Firebase Cloud Messaging (FCM) to a device token
    /// </summary>
    [AllowAnonymous]
    [HttpGet("send/firebase")]
    public async Task<ActionResult<PushNotificationResponse>> SendNotificationViaFirebase(
        string token, 
        string message,
        string? title = null)
    {
        _logger.LogInformation("Sending Firebase Cloud Messaging notification to token");

        var response = await _firebaseService.SendNotificationToTokenAsync(new PushNotificationRequest
        {
            ExternalId = token,
            Title = title ?? "Notification Title",
            Message = message,
            Url = "http://localhost:3000/products"
        });

        if (response.Success)
        {
            return Ok(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// Send push notification using Firebase Cloud Messaging (FCM) to a topic
    /// </summary>
    [AllowAnonymous]
    [HttpGet("send/firebase/topic")]
    public async Task<ActionResult<PushNotificationResponse>> SendNotificationViaFirebaseTopic(
        string topic, 
        string message,
        string? title = null)
    {
        _logger.LogInformation("Sending Firebase Cloud Messaging notification to topic: {Topic}", topic);

        var response = await _firebaseService.SendNotificationToTopicAsync(topic, new PushNotificationRequest
        {
            ExternalId = topic,
            Title = title ?? "Notification Title",
            Message = message,
            Url = "http://localhost:3000/products"
        });

        if (response.Success)
        {
            return Ok(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// Subscribe a device token to a Firebase topic
    /// </summary>
    [AllowAnonymous]
    [HttpPost("firebase/subscribe")]
    public async Task<IActionResult> SubscribeToFirebaseTopic([FromBody] TopicSubscriptionRequest request)
    {
        _logger.LogInformation("Subscribing token to topic: {Topic}", request.Topic);

        var success = await _firebaseService.SubscribeToTopicAsync(request.Token, request.Topic);

        if (success)
        {
            return Ok(new { success = true, message = $"Subscribed to topic: {request.Topic}" });
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { success = false, error = "Failed to subscribe to topic" });
        }
    }

    /// <summary>
    /// Unsubscribe a device token from a Firebase topic
    /// </summary>
    [AllowAnonymous]
    [HttpPost("firebase/unsubscribe")]
    public async Task<IActionResult> UnsubscribeFromFirebaseTopic([FromBody] TopicSubscriptionRequest request)
    {
        _logger.LogInformation("Unsubscribing token from topic: {Topic}", request.Topic);

        var success = await _firebaseService.UnsubscribeFromTopicAsync(request.Token, request.Topic);

        if (success)
        {
            return Ok(new { success = true, message = $"Unsubscribed from topic: {request.Topic}" });
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { success = false, error = "Failed to unsubscribe from topic" });
        }
    }
}

public class BrowserPushRegistration
{
    public string UserId { get; set; } = string.Empty;
    public PushSubscription Subscription { get; set; } = new();
}

public class PushSubscription
{
    public string Endpoint { get; set; } = string.Empty;
    public PushKeys Keys { get; set; } = new();
}

public class PushKeys
{
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class TopicSubscriptionRequest
{
    public string Token { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}
