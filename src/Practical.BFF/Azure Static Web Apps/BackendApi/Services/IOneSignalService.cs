namespace BackendApi.Services;

public interface IOneSignalService
{
    Task<PushNotificationResponse> SendNotificationToExternalIdAsync(PushNotificationRequest request);
}

public class PushNotificationRequest
{
    public string ExternalId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Dictionary<string, object>? Data { get; set; }

    public string? Url { get; set; }
}

public class PushNotificationResponse
{
    public bool Success { get; set; }

    public string? MessageId { get; set; }

    public string? Error { get; set; }

    public int Recipients { get; set; }
}
