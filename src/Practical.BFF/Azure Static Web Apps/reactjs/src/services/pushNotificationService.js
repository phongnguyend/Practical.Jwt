// Azure Notification Hub - Browser Push Notification Service
// Add this to your React app (e.g., src/services/pushNotificationService.js)

class PushNotificationService {
  constructor(apiBaseUrl, vapidPublicKey) {
    this.apiBaseUrl = apiBaseUrl;
    this.vapidPublicKey = vapidPublicKey;
    this.isSupported = 'serviceWorker' in navigator && 'PushManager' in window;
  }

  /**
   * Check if push notifications are supported
   */
  isNotificationSupported() {
    return this.isSupported;
  }

  /**
   * Request notification permission from user
   */
  async requestPermission() {
    if (!this.isSupported) {
      throw new Error('Push notifications are not supported in this browser');
    }

    const permission = await Notification.requestPermission();
    
    if (permission !== 'granted') {
      throw new Error('Notification permission denied');
    }

    return permission;
  }

  /**
   * Register service worker
   */
  async registerServiceWorker() {
    if (!this.isSupported) {
      throw new Error('Service Worker is not supported');
    }

    try {
      const registration = await navigator.serviceWorker.register('/service-worker.js');
      console.log('Service Worker registered:', registration);
      return registration;
    } catch (error) {
      console.error('Service Worker registration failed:', error);
      throw error;
    }
  }

  /**
   * Subscribe to push notifications
   */
  async subscribe(userId) {
    try {
      // Request permission
      await this.requestPermission();

      // Register service worker
      await this.registerServiceWorker();

      // Get service worker registration
      const registration = await navigator.serviceWorker.ready;

      // Check if already subscribed
      let subscription = await registration.pushManager.getSubscription();

      if (!subscription) {
        // Subscribe to push notifications
        subscription = await registration.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey: this.urlBase64ToUint8Array(this.vapidPublicKey)
        });
      }

      // Register with Azure Notification Hub via backend
      await this.registerWithBackend(subscription, userId);

      console.log('Successfully subscribed to push notifications');
      return subscription;
    } catch (error) {
      console.error('Failed to subscribe to push notifications:', error);
      throw error;
    }
  }

  /**
   * Unsubscribe from push notifications
   */
  async unsubscribe(userId) {
    try {
      const registration = await navigator.serviceWorker.ready;
      const subscription = await registration.pushManager.getSubscription();

      if (subscription) {
        await subscription.unsubscribe();
        
        // Unregister from backend
        await this.unregisterFromBackend(userId, subscription.endpoint);
        
        console.log('Successfully unsubscribed from push notifications');
      }
    } catch (error) {
      console.error('Failed to unsubscribe from push notifications:', error);
      throw error;
    }
  }

  /**
   * Register subscription with backend API
   */
  async registerWithBackend(subscription, userId) {
    const subscriptionJson = subscription.toJSON();
    
    const response = await fetch(`${this.apiBaseUrl}/api/pushnotification/register/browser`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        userId: userId,
        subscription: {
          endpoint: subscriptionJson.endpoint,
          keys: {
            p256dh: subscriptionJson.keys.p256dh,
            auth: subscriptionJson.keys.auth
          }
        }
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(`Failed to register with backend: ${error.error || response.statusText}`);
    }

    return await response.json();
  }

  /**
   * Unregister from backend API
   */
  async unregisterFromBackend(userId, endpoint) {
    const response = await fetch(`${this.apiBaseUrl}/api/pushnotification/register/browser/${userId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(endpoint)
    });

    if (!response.ok) {
      throw new Error('Failed to unregister from backend');
    }

    return await response.json();
  }

  /**
   * Check subscription status
   */
  async getSubscriptionStatus() {
    if (!this.isSupported) {
      return { subscribed: false, supported: false };
    }

    try {
      const registration = await navigator.serviceWorker.ready;
      const subscription = await registration.pushManager.getSubscription();
      
      return {
        subscribed: subscription !== null,
        supported: true,
        subscription: subscription
      };
    } catch (error) {
      return { subscribed: false, supported: true, error: error.message };
    }
  }

  /**
   * Convert VAPID key to Uint8Array
   */
  urlBase64ToUint8Array(base64String) {
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
}

// Export singleton instance
const pushNotificationService = new PushNotificationService(
  process.env.REACT_APP_API_BASE_URL || 'http://localhost:5000',
  process.env.REACT_APP_VAPID_PUBLIC_KEY || 'YOUR_VAPID_PUBLIC_KEY'
);

export default pushNotificationService;
