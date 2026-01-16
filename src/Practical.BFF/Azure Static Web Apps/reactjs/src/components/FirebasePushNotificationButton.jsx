import React, { useState, useEffect } from 'react';
import { 
  requestPermissionAndGetToken, 
  onMessageListener, 
  deleteToken,
  registerTokenWithBackend 
} from '../services/firebaseService';

const FirebasePushNotificationButton = ({ userId }) => {
  const [fcmToken, setFcmToken] = useState(null);
  const [isSubscribed, setIsSubscribed] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [lastMessage, setLastMessage] = useState(null);

  useEffect(() => {
    const savedToken = localStorage.getItem('fcm_token');
    if (savedToken) {
      setFcmToken(savedToken);
      setIsSubscribed(true);
    }

    const unsubscribe = onMessageListener((payload) => {
      console.log('Received foreground message:', payload);
      setLastMessage(payload);
      setTimeout(() => setLastMessage(null), 5000);
    });

    return () => unsubscribe && unsubscribe();
  }, []);

  const handleSubscribe = async () => {
    if (!userId) {
      setError('User ID is required');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const vapidKey = process.env.REACT_APP_FIREBASE_VAPID_KEY;
      
      if (!vapidKey) {
        throw new Error('VAPID key not configured. Set REACT_APP_FIREBASE_VAPID_KEY in .env file');
      }

      const token = await requestPermissionAndGetToken(vapidKey);

      if (token) {
        setFcmToken(token);
        setIsSubscribed(true);
        localStorage.setItem('fcm_token', token);
        
        const registered = await registerTokenWithBackend(token, userId);
        
        if (registered) {
          alert('Successfully subscribed to Firebase Cloud Messaging!');
        }
      } else {
        throw new Error('Failed to get FCM token. Please check browser permissions.');
      }
    } catch (err) {
      console.error('Subscription failed:', err);
      setError(err.message);
      alert(`Failed to subscribe: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleUnsubscribe = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const deleted = await deleteToken();
      
      if (deleted) {
        setFcmToken(null);
        setIsSubscribed(false);
        localStorage.removeItem('fcm_token');
        alert('Successfully unsubscribed from Firebase Cloud Messaging!');
      } else {
        throw new Error('Failed to delete FCM token');
      }
    } catch (err) {
      console.error('Unsubscribe failed:', err);
      setError(err.message);
      alert(`Failed to unsubscribe: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const copyTokenToClipboard = () => {
    if (fcmToken) {
      navigator.clipboard.writeText(fcmToken);
      alert('FCM token copied to clipboard!');
    }
  };

  const sendTestNotification = async () => {
    if (!fcmToken) {
      alert('Please subscribe first');
      return;
    }

    try {
      const response = await fetch(
        `/api/pushnotification/send/firebase?token=${encodeURIComponent(fcmToken)}&message=Hello from Firebase!&title=Test Notification`,
        { method: 'GET' }
      );

      if (response.ok) {
        alert('Test notification sent! Check your browser.');
      } else {
        const error = await response.json();
        alert(`Failed to send: ${error.error || 'Unknown error'}`);
      }
    } catch (err) {
      console.error('Failed to send test notification:', err);
      alert(`Failed to send: ${err.message}`);
    }
  };

  return (
    <div style={{ 
      padding: '20px', 
      border: '2px solid #4285F4', 
      borderRadius: '8px',
      backgroundColor: '#f8f9fa',
      marginBottom: '20px'
    }}>
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: '15px' }}>
        <img 
          src="https://www.gstatic.com/mobilesdk/160503_mobilesdk/logo/2x/firebase_28dp.png" 
          alt="Firebase" 
          style={{ width: '32px', marginRight: '10px' }}
        />
        <h3 style={{ margin: 0 }}>Firebase Cloud Messaging</h3>
      </div>

      {error && (
        <div style={{ 
          padding: '10px', 
          backgroundColor: '#f44336', 
          color: 'white', 
          borderRadius: '4px', 
          marginBottom: '10px' 
        }}>
          ? {error}
        </div>
      )}

      {lastMessage && (
        <div style={{ 
          padding: '10px', 
          backgroundColor: '#4CAF50', 
          color: 'white', 
          borderRadius: '4px', 
          marginBottom: '10px' 
        }}>
          <strong>?? New Message:</strong><br />
          <strong>{lastMessage.notification?.title}</strong><br />
          {lastMessage.notification?.body}
        </div>
      )}

      <div style={{ marginBottom: '15px' }}>
        <strong>Status:</strong>{' '}
        {isSubscribed ? (
          <span style={{ color: '#4CAF50' }}>? Subscribed</span>
        ) : (
          <span style={{ color: '#f44336' }}>? Not subscribed</span>
        )}
      </div>

      {fcmToken && (
        <div style={{ 
          marginBottom: '15px', 
          padding: '10px', 
          backgroundColor: 'white', 
          borderRadius: '4px',
          wordBreak: 'break-all',
          fontSize: '12px',
          fontFamily: 'monospace'
        }}>
          <strong>FCM Token:</strong><br />
          {fcmToken.substring(0, 50)}...
          <button
            onClick={copyTokenToClipboard}
            style={{
              marginLeft: '10px',
              padding: '5px 10px',
              fontSize: '12px',
              cursor: 'pointer',
              backgroundColor: '#2196F3',
              color: 'white',
              border: 'none',
              borderRadius: '4px'
            }}
          >
            ?? Copy
          </button>
        </div>
      )}

      <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
        {isSubscribed ? (
          <>
            <button
              onClick={handleUnsubscribe}
              disabled={isLoading}
              style={{
                padding: '10px 20px',
                backgroundColor: '#f44336',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: isLoading ? 'not-allowed' : 'pointer',
                fontSize: '16px',
                opacity: isLoading ? 0.6 : 1
              }}
            >
              {isLoading ? 'Unsubscribing...' : '?? Unsubscribe'}
            </button>
            <button
              onClick={sendTestNotification}
              disabled={isLoading}
              style={{
                padding: '10px 20px',
                backgroundColor: '#4285F4',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: isLoading ? 'not-allowed' : 'pointer',
                fontSize: '16px',
                opacity: isLoading ? 0.6 : 1
              }}
            >
              ?? Send Test
            </button>
          </>
        ) : (
          <button
            onClick={handleSubscribe}
            disabled={isLoading}
            style={{
              padding: '10px 20px',
              backgroundColor: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              fontSize: '16px',
              opacity: isLoading ? 0.6 : 1
            }}
          >
            {isLoading ? 'Subscribing...' : '?? Subscribe to Notifications'}
          </button>
        )}
      </div>

      <div style={{ marginTop: '20px', fontSize: '14px', color: '#666' }}>
        <p>?? Features:</p>
        <ul style={{ marginTop: '5px', paddingLeft: '20px' }}>
          <li>Cross-platform (Web, iOS, Android)</li>
          <li>Background & foreground notifications</li>
          <li>Topic-based messaging</li>
          <li>Free with no limits</li>
        </ul>
      </div>
    </div>
  );
};

export default FirebasePushNotificationButton;
