import { initializeApp } from 'firebase/app';
import { getMessaging, getToken, onMessage } from 'firebase/messaging';

// Firebase configuration from Firebase Console
const firebaseConfig = {
  apiKey: process.env.REACT_APP_FIREBASE_API_KEY,
  authDomain: process.env.REACT_APP_FIREBASE_AUTH_DOMAIN,
  projectId: process.env.REACT_APP_FIREBASE_PROJECT_ID,
  storageBucket: process.env.REACT_APP_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: process.env.REACT_APP_FIREBASE_MESSAGING_SENDER_ID,
  appId: process.env.REACT_APP_FIREBASE_APP_ID
};

// Initialize Firebase
let app;
let messaging;

try {
  app = initializeApp(firebaseConfig);
  messaging = getMessaging(app);
  console.log('Firebase initialized successfully');
} catch (error) {
  console.error('Firebase initialization error:', error);
}

/**
 * Request notification permission and get FCM token
 */
export async function requestPermissionAndGetToken(vapidKey) {
  if (!messaging) {
    console.error('Firebase messaging is not initialized');
    return null;
  }

  try {
    const permission = await Notification.requestPermission();
    
    if (permission === 'granted') {
      console.log('Notification permission granted');
      
      const token = await getToken(messaging, {
        vapidKey: vapidKey || process.env.REACT_APP_FIREBASE_VAPID_KEY
      });
      
      if (token) {
        console.log('FCM Token obtained:', token.substring(0, 50) + '...');
        return token;
      } else {
        console.log('No registration token available');
        return null;
      }
    } else {
      console.warn('Notification permission denied');
      return null;
    }
  } catch (error) {
    console.error('Error getting FCM token:', error);
    return null;
  }
}

/**
 * Delete FCM token
 */
export async function deleteToken() {
  if (!messaging) {
    return false;
  }

  try {
    const { deleteToken: deleteTokenFunc } = await import('firebase/messaging');
    await deleteTokenFunc(messaging);
    console.log('FCM token deleted successfully');
    return true;
  } catch (error) {
    console.error('Error deleting FCM token:', error);
    return false;
  }
}

/**
 * Listen for foreground messages
 */
export function onMessageListener(callback) {
  if (!messaging) {
    console.error('Firebase messaging is not initialized');
    return () => {};
  }

  return onMessage(messaging, (payload) => {
    console.log('Message received in foreground:', payload);
    
    if (payload.notification) {
      new Notification(payload.notification.title, {
        body: payload.notification.body,
        icon: payload.notification.icon || '/logo192.png',
        badge: '/logo192.png',
        data: payload.data
      });
    }
    
    if (callback) {
      callback(payload);
    }
  });
}

/**
 * Register FCM token with backend
 */
export async function registerTokenWithBackend(token, userId) {
  try {
    const response = await fetch('/api/fcm/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        token: token,
        userId: userId
      })
    });

    if (response.ok) {
      console.log('FCM token registered with backend');
      return true;
    } else {
      console.error('Failed to register token with backend:', await response.text());
      return false;
    }
  } catch (error) {
    console.error('Error registering token with backend:', error);
    return false;
  }
}

export { messaging };
