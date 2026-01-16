# Firebase Topics Guide - How to Choose & Use Topics

## ?? What Are Firebase Topics?

Firebase topics allow you to send messages to multiple devices that have subscribed to a particular topic. Instead of sending to individual tokens, you broadcast to all subscribers of a topic.

## ?? Topic Naming Strategies

### 1. **Content-Based Topics**
Group users by the type of content they're interested in:

```csharp
// News & Updates
"news"              // All news
"breaking_news"     // Urgent news only
"daily_digest"      // Daily summary
"weekly_newsletter" // Weekly updates

// Product Categories
"sports"
"technology"
"entertainment"
"business"
"health"
```

### 2. **User Role Topics**
Segment by user roles or permissions:

```csharp
// User Roles
$"role_admin"           // All admins
$"role_moderator"       // Moderators
$"role_premium"         // Premium users
$"role_free"            // Free tier users

// User Status
"verified_users"
"new_users"
"active_users"
```

### 3. **Geographic Topics**
Target users by location:

```csharp
// Country/Region
"country_us"
"country_uk"
"region_europe"
"region_asia"

// City-Level
"city_seattle"
"city_london"
"city_tokyo"
```

### 4. **Language Topics**
Send notifications in user's preferred language:

```csharp
"lang_en"    // English
"lang_es"    // Spanish
"lang_fr"    // French
"lang_de"    // German
```

### 5. **Feature-Specific Topics**
For specific app features or events:

```csharp
// App Features
"feature_chat"
"feature_marketplace"
"feature_live_events"

// Event Types
"promotions"        // Sales & discounts
"system_updates"    // App updates
"maintenance"       // Maintenance alerts
"security_alerts"   // Security notices
```

### 6. **User-Specific Topics**
Create personal topics for individual users:

```csharp
// Personal Topic (for targeting specific users)
$"user_{userId}"  // e.g., "user_12345"

// Order/Transaction Topics
$"order_{orderId}"  // e.g., "order_67890"
```

## ?? Implementation Examples

### Backend: Subscribe Users to Topics

```csharp
// When user registers or updates preferences
public async Task<IActionResult> UpdateNotificationPreferences(string userId, [FromBody] NotificationPreferences prefs)
{
    // Get user's FCM token
    var token = await GetUserFcmToken(userId);
    
    // Subscribe to topics based on preferences
    if (prefs.WantsNews)
        await _firebaseService.SubscribeToTopicAsync(token, "news");
    
    if (prefs.WantsSports)
        await _firebaseService.SubscribeToTopicAsync(token, "sports");
    
    if (prefs.WantsTech)
        await _firebaseService.SubscribeToTopicAsync(token, "technology");
    
    // Subscribe to user role topic
    await _firebaseService.SubscribeToTopicAsync(token, $"role_{prefs.UserRole}");
    
    // Subscribe to language topic
    await _firebaseService.SubscribeToTopicAsync(token, $"lang_{prefs.Language}");
    
    return Ok(new { success = true });
}
```

### Backend: Send to Appropriate Topic

```csharp
// Example 1: Send breaking news
public async Task SendBreakingNews(string headline, string content)
{
    await _firebaseService.SendNotificationToTopicAsync("breaking_news", new PushNotificationRequest
    {
        Title = "Breaking News",
        Message = headline,
        Url = "/news/latest"
    });
}

// Example 2: Send promotion to premium users
public async Task SendPremiumPromotion(string title, string message)
{
    await _firebaseService.SendNotificationToTopicAsync("role_premium", new PushNotificationRequest
    {
        Title = title,
        Message = message,
        Url = "/promotions"
    });
}

// Example 3: Send regional alert
public async Task SendRegionalAlert(string region, string alert)
{
    await _firebaseService.SendNotificationToTopicAsync($"region_{region}", new PushNotificationRequest
    {
        Title = "Regional Alert",
        Message = alert,
        Url = "/alerts"
    });
}

// Example 4: Send maintenance notification to all users
public async Task SendMaintenanceNotification(DateTime maintenanceTime)
{
    await _firebaseService.SendNotificationToTopicAsync("system_updates", new PushNotificationRequest
    {
        Title = "Scheduled Maintenance",
        Message = $"System maintenance at {maintenanceTime:HH:mm}",
        Url = "/status"
    });
}
```

## ?? Frontend: Topic Subscription UI

Here's how users can manage their topic subscriptions:

```javascript
// firebaseService.js - Add topic management

export async function subscribeToTopic(token, topic) {
  try {
    const response = await fetch('/api/pushnotification/firebase/subscribe', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token, topic })
    });
    
    if (response.ok) {
      console.log(`Subscribed to topic: ${topic}`);
      return true;
    }
    return false;
  } catch (error) {
    console.error('Failed to subscribe to topic:', error);
    return false;
  }
}

export async function unsubscribeFromTopic(token, topic) {
  try {
    const response = await fetch('/api/pushnotification/firebase/unsubscribe', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token, topic })
    });
    
    if (response.ok) {
      console.log(`Unsubscribed from topic: ${topic}`);
      return true;
    }
    return false;
  } catch (error) {
    console.error('Failed to unsubscribe from topic:', error);
    return false;
  }
}
```

```jsx
// NotificationPreferences.jsx - Topic subscription UI

import React, { useState } from 'react';
import { subscribeToTopic, unsubscribeFromTopic } from '../services/firebaseService';

const NotificationPreferences = ({ fcmToken }) => {
  const [topics, setTopics] = useState({
    news: false,
    sports: false,
    technology: false,
    promotions: false,
    updates: false
  });

  const handleTopicToggle = async (topic) => {
    const newState = !topics[topic];
    
    if (newState) {
      await subscribeToTopic(fcmToken, topic);
    } else {
      await unsubscribeFromTopic(fcmToken, topic);
    }
    
    setTopics(prev => ({ ...prev, [topic]: newState }));
  };

  return (
    <div>
      <h3>Notification Preferences</h3>
      
      <label>
        <input 
          type="checkbox" 
          checked={topics.news}
          onChange={() => handleTopicToggle('news')}
        />
        News Updates
      </label>
      
      <label>
        <input 
          type="checkbox" 
          checked={topics.sports}
          onChange={() => handleTopicToggle('sports')}
        />
        Sports News
      </label>
      
      <label>
        <input 
          type="checkbox" 
          checked={topics.technology}
          onChange={() => handleTopicToggle('technology')}
        />
        Technology News
      </label>
      
      <label>
        <input 
          type="checkbox" 
          checked={topics.promotions}
          onChange={() => handleTopicToggle('promotions')}
        />
        Promotions & Offers
      </label>
      
      <label>
        <input 
          type="checkbox" 
          checked={topics.updates}
          onChange={() => handleTopicToggle('updates')}
        />
        App Updates
      </label>
    </div>
  );
};

export default NotificationPreferences;
```

## ?? Topic Management Strategies

### Strategy 1: **Database-Driven Topics**

Store available topics in your database:

```csharp
// Create a Topics table
public class NotificationTopic
{
    public int Id { get; set; }
    public string TopicName { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public bool IsActive { get; set; }
}

// API to get available topics
[HttpGet("firebase/topics")]
public async Task<ActionResult<List<NotificationTopic>>> GetAvailableTopics()
{
    var topics = await _dbContext.NotificationTopics
        .Where(t => t.IsActive)
        .ToListAsync();
    
    return Ok(topics);
}
```

### Strategy 2: **Configuration-Based Topics**

Define topics in appsettings.json:

```json
{
  "Firebase": {
    "Topics": {
      "News": ["news", "breaking_news", "daily_digest"],
      "Sports": ["sports", "football", "basketball"],
      "Content": ["technology", "entertainment", "business"],
      "System": ["updates", "maintenance", "alerts"]
    }
  }
}
```

### Strategy 3: **Dynamic Topic Generation**

Generate topics based on user actions:

```csharp
// When user follows a category
public async Task UserFollowCategory(string userId, string category)
{
    var token = await GetUserFcmToken(userId);
    var topic = $"category_{category.ToLower()}";
    
    await _firebaseService.SubscribeToTopicAsync(token, topic);
    
    // Save to database
    await _dbContext.UserTopicSubscriptions.AddAsync(new UserTopicSubscription
    {
        UserId = userId,
        Topic = topic,
        SubscribedAt = DateTime.UtcNow
    });
    await _dbContext.SaveChangesAsync();
}
```

## ? Best Practices

### 1. **Topic Naming Conventions**
```csharp
// ? Good - Clear, lowercase, underscores
"breaking_news"
"role_admin"
"lang_en"
"region_us_west"

// ? Bad - Mixed case, spaces, special chars
"Breaking-News"
"Role Admin"
"lang.en"
"region us/west"
```

### 2. **Topic Limits**
- Maximum 2000 topics per app (Firebase limit)
- Users can subscribe to unlimited topics
- Topic names: 1-900 characters, `[a-zA-Z0-9-_.~%]+`

### 3. **Cleanup Inactive Topics**
```csharp
// Periodically clean up unused topics
public async Task CleanupInactiveTopics()
{
    var inactiveTopics = await _dbContext.NotificationTopics
        .Where(t => t.LastUsed < DateTime.UtcNow.AddMonths(-6))
        .ToListAsync();
    
    foreach (var topic in inactiveTopics)
    {
        // Mark as inactive
        topic.IsActive = false;
        _logger.LogInformation("Deactivated inactive topic: {Topic}", topic.TopicName);
    }
    
    await _dbContext.SaveChangesAsync();
}
```

## ?? API Endpoints Summary

### Subscribe to Topic
```http
POST /api/pushnotification/firebase/subscribe
Content-Type: application/json

{
  "token": "fcm-device-token",
  "topic": "news"
}
```

### Unsubscribe from Topic
```http
POST /api/pushnotification/firebase/unsubscribe
Content-Type: application/json

{
  "token": "fcm-device-token",
  "topic": "news"
}
```

### Send to Topic
```http
GET /api/pushnotification/send/firebase/topic
  ?topic=news
  &message=Breaking+news+update
  &title=News+Alert
```

## ?? Quick Decision Guide

**Use Topics When:**
- ? Broadcasting to multiple users (news, alerts, updates)
- ? Segmenting by interest or preference
- ? Sending region or language-specific messages
- ? Role-based notifications (admin, moderator)

**Use Direct Tokens When:**
- ? Personal notifications (order status, messages)
- ? One-to-one communication
- ? User-specific alerts
- ? Time-sensitive personal updates

## ?? Example Topic Structure

```
your-app/
??? Content Topics
?   ??? news
?   ??? sports
?   ??? technology
?   ??? entertainment
?
??? Role Topics
?   ??? role_admin
?   ??? role_moderator
?   ??? role_premium
?
??? Language Topics
?   ??? lang_en
?   ??? lang_es
?   ??? lang_fr
?
??? System Topics
?   ??? updates
?   ??? maintenance
?   ??? alerts
?
??? Regional Topics
    ??? region_us
    ??? region_eu
    ??? region_asia
```

## ?? Summary

**To determine which topic to use:**

1. **Ask yourself:**
   - Who should receive this notification?
   - Is it content-based, role-based, or location-based?
   - Is it for all users or a specific group?

2. **Choose topic type:**
   - Broadcast ? `"news"`, `"updates"`
   - Segment ? `"role_admin"`, `"lang_en"`
   - Feature ? `"promotions"`, `"alerts"`

3. **Subscribe users** based on their preferences and profile

4. **Send notifications** to the appropriate topic(s)

Your system now has full topic management capabilities! ??
