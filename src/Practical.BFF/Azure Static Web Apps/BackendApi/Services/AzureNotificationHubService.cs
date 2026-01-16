using Microsoft.Azure.NotificationHubs;
using System.Text.Json;

namespace BackendApi.Services;

public class AzureNotificationHubService : IAzureNotificationHubService
{
    private readonly NotificationHubClient _hubClient;
    private readonly ILogger<AzureNotificationHubService> _logger;

    public AzureNotificationHubService(IConfiguration configuration, ILogger<AzureNotificationHubService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureNotificationHub:ConnectionString"]
            ?? throw new InvalidOperationException("AzureNotificationHub:ConnectionString is not configured");

        var hubName = configuration["AzureNotificationHub:HubName"]
            ?? throw new InvalidOperationException("AzureNotificationHub:HubName is not configured");

        _hubClient = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);
    }

    public async Task<PushNotificationResponse> SendNotificationToUserAsync(PushNotificationRequest request)
    {
        try
        {
            // Build web push notification payload
            var webPushPayload = BuildWebPushPayload(request);
            
            _logger.LogInformation("Sending Web Push notification to user: {ExternalId}", request.ExternalId);
            
            // Send as generic template notification that will be delivered to web browsers
            var outcome = await _hubClient.SendTemplateNotificationAsync(
                webPushPayload,
                $"ExternalId:{request.ExternalId}");

            if (outcome.State == NotificationOutcomeState.Completed)
            {
                _logger.LogInformation(
                    "Push notification sent successfully. NotificationId: {NotificationId}, Success: {Success}, Failure: {Failure}",
                    outcome.NotificationId, outcome.Success, outcome.Failure);

                return new PushNotificationResponse
                {
                    Success = true,
                    MessageId = outcome.NotificationId,
                    Recipients = (int)outcome.Success
                };
            }
            else
            {
                var errorMessage = $"Notification state: {outcome.State}";
                _logger.LogWarning("Failed to send push notification: {Error}", errorMessage);
                
                return new PushNotificationResponse
                {
                    Success = false,
                    Error = errorMessage,
                    Recipients = 0
                };
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when sending notification to: {ExternalId}", request.ExternalId);
            return new PushNotificationResponse
            {
                Success = false,
                Error = $"Invalid argument: {ex.Message}",
                Recipients = 0
            };
        }
        catch (Exception ex) when (ex.GetType().Name == "NotificationHubException")
        {
            _logger.LogError(ex, "Azure Notification Hub error: {Message}", ex.Message);
            return new PushNotificationResponse
            {
                Success = false,
                Error = $"Azure Notification Hub error: {ex.Message}",
                Recipients = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to user: {ExternalId}", request.ExternalId);
            return new PushNotificationResponse
            {
                Success = false,
                Error = ex.Message,
                Recipients = 0
            };
        }
    }

    private Dictionary<string, string> BuildWebPushPayload(PushNotificationRequest request)
    {
        // Build Web Push payload (for browsers)
        var notificationData = new
        {
            notification = new
            {
                title = request.Title,
                body = request.Message,
                icon = "/icon.png", // You can customize this
                badge = "/badge.png", // You can customize this
                data = new
                {
                    url = request.Url ?? "/",
                    customData = request.Data
                },
                actions = request.Url != null ? new[]
                {
                    new { action = "open", title = "Open" }
                } : null
            }
        };

        // Return as dictionary for template notification
        return new Dictionary<string, string>
        {
            { "message", JsonSerializer.Serialize(notificationData) }
        };
    }
}
