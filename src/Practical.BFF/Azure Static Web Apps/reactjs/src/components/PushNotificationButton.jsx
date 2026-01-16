import React, { useState, useEffect } from 'react';
import pushNotificationService from '../services/pushNotificationService';

const PushNotificationButton = ({ userId }) => {
  const [isSubscribed, setIsSubscribed] = useState(false);
  const [isSupported, setIsSupported] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    checkSubscriptionStatus();
  }, []);

  const checkSubscriptionStatus = async () => {
    try {
      const status = await pushNotificationService.getSubscriptionStatus();
      setIsSupported(status.supported);
      setIsSubscribed(status.subscribed);
    } catch (err) {
      console.error('Failed to check subscription status:', err);
    }
  };

  const handleSubscribe = async () => {
    if (!userId) {
      setError('User ID is required');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await pushNotificationService.subscribe(userId);
      setIsSubscribed(true);
      alert('Successfully subscribed to push notifications!');
    } catch (err) {
      console.error('Subscription failed:', err);
      setError(err.message);
      alert(`Failed to subscribe: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleUnsubscribe = async () => {
    if (!userId) {
      setError('User ID is required');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await pushNotificationService.unsubscribe(userId);
      setIsSubscribed(false);
      alert('Successfully unsubscribed from push notifications!');
    } catch (err) {
      console.error('Unsubscribe failed:', err);
      setError(err.message);
      alert(`Failed to unsubscribe: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  if (!isSupported) {
    return (
      <div style={{ padding: '10px', backgroundColor: '#f44336', color: 'white', borderRadius: '4px' }}>
        Push notifications are not supported in this browser
      </div>
    );
  }

  return (
    <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px' }}>
      <h3>Browser Push Notifications (Azure)</h3>
      
      {error && (
        <div style={{ padding: '10px', backgroundColor: '#f44336', color: 'white', borderRadius: '4px', marginBottom: '10px' }}>
          {error}
        </div>
      )}

      <div style={{ marginBottom: '10px' }}>
        <strong>Status:</strong> {isSubscribed ? 'Subscribed ?' : 'Not subscribed ?'}
      </div>

      {isSubscribed ? (
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
            fontSize: '16px'
          }}
        >
          {isLoading ? 'Unsubscribing...' : 'Unsubscribe from Notifications'}
        </button>
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
            fontSize: '16px'
          }}
        >
          {isLoading ? 'Subscribing...' : 'Subscribe to Notifications'}
        </button>
      )}

      <div style={{ marginTop: '20px', fontSize: '14px', color: '#666' }}>
        <p>?? Click the button above to enable browser push notifications.</p>
        <p>You'll receive notifications even when the app is closed!</p>
      </div>
    </div>
  );
};

export default PushNotificationButton;
