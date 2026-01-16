namespace BackendApi.Services;

public interface IFirebaseService
{
    Task<PushNotificationResponse> SendNotificationToTokenAsync(PushNotificationRequest request);
    Task<PushNotificationResponse> SendNotificationToTopicAsync(string topic, PushNotificationRequest request);
    Task<bool> SubscribeToTopicAsync(string token, string topic);
    Task<bool> UnsubscribeFromTopicAsync(string token, string topic);
}
