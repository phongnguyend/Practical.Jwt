// Service Worker for Azure Notification Hub Web Push
// Save this as public/service-worker.js in your React app

self.addEventListener('install', (event) => {
  console.log('Service Worker installing.');
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  console.log('Service Worker activating.');
  event.waitUntil(self.clients.claim());
});

self.addEventListener('push', (event) => {
  console.log('Push notification received:', event);

  let notificationData;
  
  try {
    // Try to parse the push data
    const data = event.data ? event.data.json() : {};
    notificationData = data.notification || data;
    
    console.log('Notification data:', notificationData);
  } catch (error) {
    console.error('Failed to parse push notification data:', error);
    notificationData = {
      title: 'New Notification',
      body: event.data ? event.data.text() : 'You have a new notification'
    };
  }

  const title = notificationData.title || 'Notification';
  const options = {
    body: notificationData.body || notificationData.message || '',
    icon: notificationData.icon || '/logo192.png',
    badge: notificationData.badge || '/logo192.png',
    data: notificationData.data || { url: '/' },
    vibrate: [200, 100, 200],
    tag: notificationData.tag || 'notification-tag',
    requireInteraction: notificationData.requireInteraction || false,
    actions: notificationData.actions || [
      { action: 'open', title: 'Open', icon: '/logo192.png' }
    ]
  };

  event.waitUntil(
    self.registration.showNotification(title, options)
  );
});

self.addEventListener('notificationclick', (event) => {
  console.log('Notification clicked:', event);
  
  event.notification.close();

  const urlToOpen = event.notification.data?.url || '/';
  const action = event.action;

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true })
      .then((windowClients) => {
        // Check if there's already a window open with this URL
        for (let i = 0; i < windowClients.length; i++) {
          const client = windowClients[i];
          if (client.url === urlToOpen && 'focus' in client) {
            return client.focus();
          }
        }
        // If no window is open, open a new one
        if (clients.openWindow) {
          return clients.openWindow(urlToOpen);
        }
      })
  );
});

self.addEventListener('notificationclose', (event) => {
  console.log('Notification closed:', event);
  
  // You can track notification close events here
  // For example, send analytics
});

// Handle push subscription changes
self.addEventListener('pushsubscriptionchange', (event) => {
  console.log('Push subscription changed:', event);
  
  event.waitUntil(
    // Resubscribe with new subscription
    self.registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: self.registration.pushManager.applicationServerKey
    }).then((subscription) => {
      // Send new subscription to server
      return fetch('/api/pushnotification/register/browser', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          subscription: subscription.toJSON()
        })
      });
    })
  );
});
