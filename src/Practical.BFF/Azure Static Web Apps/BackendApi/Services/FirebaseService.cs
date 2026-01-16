using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace BackendApi.Services;

public class FirebaseService : IFirebaseService
{
    private readonly ILogger<FirebaseService> _logger;
    private readonly FirebaseApp _firebaseApp;

    public FirebaseService(IConfiguration configuration, ILogger<FirebaseService> logger)
    {
        _logger = logger;

        var credentialsPath = configuration["Firebase:CredentialsPath"];
        var credentialsJson = configuration["Firebase:CredentialsJson"];

        if (!string.IsNullOrEmpty(credentialsJson))
        {
            // Initialize from JSON string (recommended for production)
            _firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(credentialsJson)
            });
        }
        else if (!string.IsNullOrEmpty(credentialsPath))
        {
            // Initialize from file path (for development)
            _firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credentialsPath)
            });
        }
        else
        {
            throw new InvalidOperationException(
                "Firebase credentials not configured. Set either Firebase:CredentialsPath or Firebase:CredentialsJson");
        }

        _logger.LogInformation("Firebase service initialized successfully");
    }

    public async Task<PushNotificationResponse> SendNotificationToTokenAsync(PushNotificationRequest request)
    {
        try
        {
            _logger.LogInformation("Sending FCM notification to token/user: {ExternalId}", request.ExternalId);

            // Build the FCM message
            var message = new Message
            {
                Token = request.ExternalId, // In FCM, ExternalId is the device token
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Message
                },
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = request.Title,
                        Body = request.Message,
                        Icon = "/icon.png",
                        Badge = "/badge.png"
                    },
                    FcmOptions = new WebpushFcmOptions
                    {
                        Link = request.Url ?? "/"
                    }
                }
            };

            // Add custom data if provided
            if (request.Data != null && request.Data.Any())
            {
                message.Data = request.Data.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                );
            }

            // Send the message
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

            _logger.LogInformation("FCM notification sent successfully. MessageId: {MessageId}", response);

            return new PushNotificationResponse
            {
                Success = true,
                MessageId = response,
                Recipients = 1
            };
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase Messaging error: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            return new PushNotificationResponse
            {
                Success = false,
                Error = $"Firebase error ({ex.ErrorCode}): {ex.Message}",
                Recipients = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM notification to: {ExternalId}", request.ExternalId);
            return new PushNotificationResponse
            {
                Success = false,
                Error = ex.Message,
                Recipients = 0
            };
        }
    }

    public async Task<PushNotificationResponse> SendNotificationToTopicAsync(string topic, PushNotificationRequest request)
    {
        try
        {
            _logger.LogInformation("Sending FCM notification to topic: {Topic}", topic);

            // Build the FCM message for topic
            var message = new Message
            {
                Topic = topic,
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Message
                },
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = request.Title,
                        Body = request.Message,
                        Icon = "/icon.png",
                        Badge = "/badge.png"
                    },
                    FcmOptions = new WebpushFcmOptions
                    {
                        Link = request.Url ?? "/"
                    }
                }
            };

            // Add custom data if provided
            if (request.Data != null && request.Data.Any())
            {
                message.Data = request.Data.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                );
            }

            // Send the message
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

            _logger.LogInformation("FCM notification sent to topic successfully. MessageId: {MessageId}", response);

            return new PushNotificationResponse
            {
                Success = true,
                MessageId = response,
                Recipients = 1 // Topic recipients count is not available
            };
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase Messaging error for topic {Topic}: {ErrorCode} - {Message}", 
                topic, ex.ErrorCode, ex.Message);
            return new PushNotificationResponse
            {
                Success = false,
                Error = $"Firebase error ({ex.ErrorCode}): {ex.Message}",
                Recipients = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM notification to topic: {Topic}", topic);
            return new PushNotificationResponse
            {
                Success = false,
                Error = ex.Message,
                Recipients = 0
            };
        }
    }

    /// <summary>
    /// Subscribe device token to a topic
    /// </summary>
    public async Task<bool> SubscribeToTopicAsync(string token, string topic)
    {
        try
        {
            _logger.LogInformation("Subscribing token to topic: {Topic}", topic);
            
            await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(
                new List<string> { token }, 
                topic
            );
            
            _logger.LogInformation("Successfully subscribed to topic: {Topic}", topic);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to topic: {Topic}", topic);
            return false;
        }
    }

    /// <summary>
    /// Unsubscribe device token from a topic
    /// </summary>
    public async Task<bool> UnsubscribeFromTopicAsync(string token, string topic)
    {
        try
        {
            _logger.LogInformation("Unsubscribing token from topic: {Topic}", topic);
            
            await FirebaseMessaging.DefaultInstance.UnsubscribeFromTopicAsync(
                new List<string> { token }, 
                topic
            );
            
            _logger.LogInformation("Successfully unsubscribed from topic: {Topic}", topic);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from topic: {Topic}", topic);
            return false;
        }
    }
}
