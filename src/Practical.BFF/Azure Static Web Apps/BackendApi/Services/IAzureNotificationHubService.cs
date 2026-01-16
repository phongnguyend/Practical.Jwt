namespace BackendApi.Services;

public interface IAzureNotificationHubService
{
    Task<PushNotificationResponse> SendNotificationToUserAsync(PushNotificationRequest request);
}
