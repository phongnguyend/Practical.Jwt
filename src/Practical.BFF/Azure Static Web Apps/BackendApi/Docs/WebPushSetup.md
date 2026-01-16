# Azure Notification Hub - Web Push Setup Guide

## Overview
This service sends push notifications to web browsers (Chrome, Firefox, Edge, Safari) using Azure Notification Hub.

## Prerequisites

1. **Azure Notification Hub** configured with Web Push credentials
2. **VAPID Keys** (Voluntary Application Server Identification) for Web Push
3. **Service Worker** registered in your web application

## Azure Portal Setup

### 1. Create Notification Hub
1. Go to Azure Portal
2. Create a new Notification Hub
3. Note the **Connection String** and **Hub Name**

### 2. Configure Web Push (Browser)
1. In your Notification Hub, go to **Settings** ? **Browser (Web Push)**
2. Generate VAPID keys or provide your own:
   - Public Key (VAPID Public Key)
   - Private Key (VAPID Private Key)
   - Subject (your website URL or mailto: email)

### 3. Get Connection String
1. Go to **Access Policies**
2. Copy the **DefaultFullSharedAccessSignature** connection string

## Client-Side Setup (React/JavaScript)

### 1. Register Service Worker

Create `public/service-worker.js`:

```javascript
self.addEventListener('push', function(event) {
  const data = event.data.json();
  const notification = data.notification;
  
  const options = {
    body: notification.body,
    icon: notification.icon || '/icon.png',
    badge: notification.badge || '/badge.png',
    data: notification.data,
    actions: notification.actions
  };

  event.waitUntil(
    self.registration.showNotification(notification.title, options)
  );
});

self.addEventListener('notificationclick', function(event) {
  event.notification.close();
  
  if (event.notification.data && event.notification.data.url) {
    event.waitUntil(
      clients.openWindow(event.notification.data.url)
    );
  }
});
```

### 2. Register Service Worker in Your App

```javascript
// Register service worker
if ('serviceWorker' in navigator) {
  navigator.serviceWorker.register('/service-worker.js')
    .then(registration => {
      console.log('Service Worker registered:', registration);
    })
    .catch(error => {
      console.error('Service Worker registration failed:', error);
    });
}
```

### 3. Subscribe to Push Notifications

```javascript
async function subscribeToPushNotifications(userId) {
  try {
    // Get service worker registration
    const registration = await navigator.serviceWorker.ready;
    
    // Subscribe to push notifications
    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array('YOUR_VAPID_PUBLIC_KEY')
    });

    // Register with Azure Notification Hub
    await registerWithAzureNotificationHub(subscription, userId);
    
    console.log('Push subscription successful');
  } catch (error) {
    console.error('Failed to subscribe to push notifications:', error);
  }
}

function urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - base64String.length % 4) % 4);
  const base64 = (base64String + padding)
    .replace(/\-/g, '+')
    .replace(/_/g, '/');
  
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}
```

### 4. Register Subscription with Azure Notification Hub

You'll need a backend API endpoint to register the browser subscription:

```javascript
async function registerWithAzureNotificationHub(subscription, userId) {
  const response = await fetch('/api/notifications/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      subscription: subscription,
      userId: userId,
      tags: [`ExternalId:${userId}`]
    })
  });
  
  if (!response.ok) {
    throw new Error('Failed to register with Notification Hub');
  }
}
```

## Backend API for Registration

Add this endpoint to register browser push subscriptions:

```csharp
[HttpPost("register")]
public async Task<IActionResult> RegisterBrowserPushSubscription([FromBody] BrowserPushRegistration registration)
{
    try
    {
        var installation = new Installation
        {
            InstallationId = registration.Subscription.Endpoint.GetHashCode().ToString(),
            Platform = NotificationPlatform.Browser,
            PushChannel = JsonSerializer.Serialize(registration.Subscription),
            Tags = registration.Tags
        };

        await _hubClient.CreateOrUpdateInstallationAsync(installation);
        return Ok(new { success = true });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to register browser push subscription");
        return BadRequest(new { error = ex.Message });
    }
}

public class BrowserPushRegistration
{
    public PushSubscription Subscription { get; set; }
    public string UserId { get; set; }
    public List<string> Tags { get; set; }
}

public class PushSubscription
{
    public string Endpoint { get; set; }
    public PushKeys Keys { get; set; }
}

public class PushKeys
{
    public string P256dh { get; set; }
    public string Auth { get; set; }
}
```

## Configuration

Update your `appsettings.json`:

```json
{
  "AzureNotificationHub": {
    "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=your-key",
    "HubName": "your-hub-name"
  }
}
```

## Testing

1. **Register a browser** by calling the subscription flow
2. **Send a test notification**:
   ```
   GET /api/pushnotification/send/azure?externalId=user123&message=Hello&title=Test
   ```

## Notification Format

The service sends notifications in this format:

```json
{
  "notification": {
    "title": "Your Title",
    "body": "Your Message",
    "icon": "/icon.png",
    "badge": "/badge.png",
    "data": {
      "url": "/your-url",
      "customData": { "key": "value" }
    },
    "actions": [
      { "action": "open", "title": "Open" }
    ]
  }
}
```

## Browser Compatibility

- ? Chrome 42+
- ? Firefox 44+
- ? Edge 17+
- ? Safari 16+ (macOS 13+, iOS 16.4+)
- ? Opera 37+

## Troubleshooting

### Notifications not appearing?
1. Check browser permissions (allow notifications)
2. Verify service worker is registered
3. Check Azure Notification Hub logs
4. Ensure VAPID keys are correctly configured

### HTTPS Required
Web Push requires HTTPS (except localhost for development)

### Cross-Origin Issues
Make sure your VAPID subject matches your domain

## Additional Resources

- [Web Push Protocol (RFC 8030)](https://tools.ietf.org/html/rfc8030)
- [Azure Notification Hubs Documentation](https://docs.microsoft.com/azure/notification-hubs/)
- [Web Push Notifications Guide](https://web.dev/push-notifications-overview/)
