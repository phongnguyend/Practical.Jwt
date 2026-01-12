using OneSignalApi.Api;
using OneSignalApi.Client;
using OneSignalApi.Model;

namespace BackendApi.Services;

public class OneSignalService : IOneSignalService
{
    private readonly DefaultApi _oneSignalClient;
    private readonly string _appId;

    public OneSignalService(IConfiguration configuration)
    {
        var apiKey = configuration["OneSignal:ApiKey"]
            ?? throw new InvalidOperationException("OneSignal:ApiKey is not configured");

        _appId = configuration["OneSignal:AppId"]
            ?? throw new InvalidOperationException("OneSignal:AppId is not configured");

        var config = new Configuration
        {
            BasePath = "https://onesignal.com/api/v1",
            AccessToken = apiKey
        };

        _oneSignalClient = new DefaultApi(config);
    }

    public async Task<PushNotificationResponse> SendNotificationToExternalIdAsync(PushNotificationRequest request)
    {
        var notification = new Notification(appId: _appId)
        {
            IncludeExternalUserIds = new List<string> { request.ExternalId },
            Headings = new StringMap
            {
                En = request.Title
            },
            Contents = new StringMap
            {
                En = request.Message
            }
        };

        // Add custom data if provided
        if (request.Data != null && request.Data.Any())
        {
            notification.Data = request.Data;
        }

        // Add URL if provided
        if (!string.IsNullOrEmpty(request.Url))
        {
            notification.Url = request.Url;
        }

        var result = await _oneSignalClient.CreateNotificationAsync(notification);

        return new PushNotificationResponse
        {
            Success = true,
            MessageId = result.Id,
            Recipients = result.Recipients
        };
    }
}
