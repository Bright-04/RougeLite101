# Event System Documentation

## Overview

The RougeLite101 project implements a comprehensive event system that enables loose coupling between game systems. This event-driven architecture allows components to communicate without direct references, making the codebase more maintainable and extensible.

## Core Components

### 1. GameEvent Base Classes
Located in `Assets/Scripts/Events/GameEvent.cs`

- **GameEvent**: Abstract base class for all events
- **GameEvent<T>**: Generic base class for events with data payload
- Features type safety and timestamp tracking

### 2. EventManager
Located in `Assets/Scripts/Events/EventManager.cs`

- Singleton pattern for global event management
- Supports both immediate broadcasting and queued events
- Thread-safe implementation with proper cleanup

### 3. IEventListener Interface
Located in `Assets/Scripts/Events/IEventListener.cs`

- Generic interface for event listeners
- Type-safe event handling

### 4. EventBehaviour Helper
Located in `Assets/Scripts/Events/EventBehaviour.cs`

- Base class for MonoBehaviours that need event functionality
- Convenience methods for broadcasting and subscribing
- Automatic EventManager initialization
- Includes SimpleEventListener for prototyping

## Available Events

### Player Events
- **PlayerMovementEvent**: Player position and velocity changes
- **PlayerDamagedEvent**: Player takes damage
- **PlayerHealedEvent**: Player health restoration
- **PlayerDeathEvent**: Player dies
- **PlayerManaChangedEvent**: Mana updates

### Combat Events
- **AttackPerformedEvent**: Weapon attacks (sword, spells)
- **SpellCastEvent**: Magic spell casting

### Enemy Events
- **EnemyDeathEvent**: Enemy defeats
- **EnemySpawnedEvent**: New enemy creation

### Game State Events
- **LevelCompleteEvent**: Level completion
- **GameOverEvent**: Game ending
- **GameStartEvent**: Game initialization

### UI Events
- **UIUpdateEvent**: Interface updates
- **InventoryChangedEvent**: Item management

## Usage Examples

### Basic Event Broadcasting

```csharp
public class PlayerHealth : EventBehaviour
{
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // Broadcast damage event
        var damageData = new PlayerHealthData
        {
            player = gameObject,
            damage = damage,
            currentHealth = currentHealth,
            maxHealth = maxHealth
        };
        
        var damageEvent = new PlayerDamagedEvent
        {
            Data = damageData,
            Timestamp = System.DateTime.Now
        };
        
        BroadcastEvent(damageEvent);
        
        if (currentHealth <= 0)
        {
            // Broadcast death event
            var deathEvent = new PlayerDeathEvent
            {
                Timestamp = System.DateTime.Now
            };
            
            BroadcastEvent(deathEvent);
        }
    }
}
```

### Event Listening

```csharp
public class UIHealthBar : EventBehaviour, IEventListener<PlayerDamagedEvent>
{
    protected override void Awake()
    {
        base.Awake();
        SubscribeToEvent<PlayerDamagedEvent>(this);
    }
    
    protected override void OnDestroy()
    {
        UnsubscribeFromEvent<PlayerDamagedEvent>(this);
        base.OnDestroy();
    }
    
    public void OnEventReceived(PlayerDamagedEvent eventData)
    {
        UpdateHealthBar(eventData.Data.currentHealth, eventData.Data.maxHealth);
    }
}
```

### Multiple Event Handling

```csharp
public class GameManager : EventBehaviour, 
    IEventListener<PlayerDeathEvent>,
    IEventListener<EnemyDeathEvent>,
    IEventListener<LevelCompleteEvent>
{
    private int enemiesKilled = 0;
    
    protected override void Awake()
    {
        base.Awake();
        SubscribeToEvent<PlayerDeathEvent>(this);
        SubscribeToEvent<EnemyDeathEvent>(this);
        SubscribeToEvent<LevelCompleteEvent>(this);
    }
    
    public void OnEventReceived(PlayerDeathEvent eventData)
    {
        // Handle game over
        TriggerGameOver();
    }
    
    public void OnEventReceived(EnemyDeathEvent eventData)
    {
        enemiesKilled++;
        UpdateScore();
    }
    
    public void OnEventReceived(LevelCompleteEvent eventData)
    {
        LoadNextLevel();
    }
}
```

## Integration Guide

### Converting Existing Systems

1. **Change Base Class**: Replace `MonoBehaviour` with `EventBehaviour`
2. **Override Awake**: Call `base.Awake()` first
3. **Override OnDestroy**: Call `base.OnDestroy()` last
4. **Replace Direct Calls**: Use events instead of direct method calls

### Before (Direct Coupling)
```csharp
public class Sword : MonoBehaviour
{
    public UIManager uiManager; // Direct reference
    
    private void Attack()
    {
        // Direct method call
        uiManager.ShowAttackFeedback();
    }
}
```

### After (Event-Driven)
```csharp
public class Sword : EventBehaviour
{
    private void Attack()
    {
        // Broadcast event instead
        var attackEvent = new AttackPerformedEvent
        {
            Data = new AttackData { /* attack info */ },
            Timestamp = System.DateTime.Now
        };
        
        BroadcastEvent(attackEvent);
    }
}
```

## Best Practices

### Event Design
- **Descriptive Names**: Use clear, action-based event names
- **Rich Data**: Include all relevant information in event data
- **Immutable Data**: Event data should be read-only
- **Specific Events**: Create specific events rather than generic ones

### Performance
- **Cache References**: EventBehaviour caches EventManager for performance
- **Unsubscribe Properly**: Always unsubscribe in OnDestroy to prevent memory leaks
- **Consider Frequency**: High-frequency events (like movement) should be optimized

### Architecture
- **Single Responsibility**: Each event should represent one specific occurrence
- **Loose Coupling**: Avoid dependencies between event senders and receivers
- **Error Handling**: Include null checks and error handling in event processing

## Debugging

### Event Monitoring
Use the `SimpleEventListener` component for quick debugging:

```csharp
// Add to any GameObject for event logging
var listener = gameObject.AddComponent<SimpleEventListener>();
listener.logPlayerDamage = true;
listener.logEnemyDeath = true;
```

### Debug Output
Events automatically log when broadcast (can be disabled):

```
Player took 10 damage! Health: 40/50
Enemy Slime has been defeated!
Spell 'Fireball' cast by Player!
```

### Common Issues
1. **Null EventManager**: EventBehaviour auto-creates if missing
2. **Memory Leaks**: Always unsubscribe in OnDestroy
3. **Event Timing**: Use QueueEvent for frame-end processing
4. **Missing Data**: Validate event data before broadcasting

## Future Enhancements

### Planned Features
- **Event History**: Track event sequences for debugging
- **Event Filters**: Conditional event processing
- **Event Priorities**: Ordered event handling
- **Event Analytics**: Performance monitoring and statistics

### Extension Points
- **Custom Events**: Inherit from GameEvent<T> for new event types
- **Event Processors**: Create specialized event handling components
- **Event Persistence**: Save/load event state for game saves

## File Structure

```
Assets/Scripts/Events/
├── GameEvent.cs              # Base event classes
├── EventManager.cs           # Global event management
├── IEventListener.cs         # Event listener interface
├── EventBehaviour.cs         # Helper base class
└── GameEvents.cs            # Specific game events
```

## Dependencies

- **Unity Version**: Unity 6000.1.9f1 or later
- **Input System**: Unity's new Input System package
- **C# Version**: .NET Standard 2.1 (C# 8.0 features)

---

For additional help or questions about the event system, consult the inline code documentation or create an issue in the project repository.