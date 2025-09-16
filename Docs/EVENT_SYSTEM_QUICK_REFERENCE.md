# Event System Quick Reference

## Quick Setup

### 1. Convert MonoBehaviour to EventBehaviour
```csharp
// Before
public class MyClass : MonoBehaviour

// After  
public class MyClass : EventBehaviour
```

### 2. Override Awake and OnDestroy
```csharp
protected override void Awake()
{
    base.Awake(); // Initialize event system
    // Your initialization code
}

protected override void OnDestroy()
{
    // Your cleanup code
    base.OnDestroy(); // Event system cleanup
}
```

## Broadcasting Events

### Simple Event
```csharp
var gameEvent = new GameStartEvent 
{ 
    Timestamp = System.DateTime.Now 
};
BroadcastEvent(gameEvent);
```

### Event with Data
```csharp
var eventData = new PlayerHealthData
{
    player = gameObject,
    currentHealth = health,
    maxHealth = maxHealth
};

var healthEvent = new PlayerDamagedEvent
{
    Data = eventData,
    Timestamp = System.DateTime.Now
};

BroadcastEvent(healthEvent);
```

## Listening to Events

### Subscribe in Awake
```csharp
public class Listener : EventBehaviour, IEventListener<PlayerDamagedEvent>
{
    protected override void Awake()
    {
        base.Awake();
        SubscribeToEvent<PlayerDamagedEvent>(this);
    }

    public void OnEventReceived(PlayerDamagedEvent eventData)
    {
        // Handle the event
        Debug.Log($"Player took {eventData.Data.damage} damage");
    }

    protected override void OnDestroy()
    {
        UnsubscribeFromEvent<PlayerDamagedEvent>(this);
        base.OnDestroy();
    }
}
```

## Available Events Cheat Sheet

| Event | Data Type | Purpose |
|-------|-----------|---------|
| `PlayerMovementEvent` | `PlayerMovementData` | Player position/velocity |
| `PlayerDamagedEvent` | `PlayerHealthData` | Player takes damage |
| `PlayerHealedEvent` | `PlayerHealthData` | Player heals |
| `PlayerDeathEvent` | None | Player dies |
| `PlayerManaChangedEvent` | `PlayerManaData` | Mana updates |
| `AttackPerformedEvent` | `AttackData` | Weapon attacks |
| `SpellCastEvent` | `SpellData` | Spell casting |
| `EnemyDeathEvent` | `EnemyData` | Enemy defeated |
| `EnemySpawnedEvent` | `EnemyData` | Enemy spawned |
| `LevelCompleteEvent` | `LevelData` | Level completed |
| `GameOverEvent` | None | Game ended |
| `GameStartEvent` | None | Game started |
| `UIUpdateEvent` | `UIData` | UI changes |
| `InventoryChangedEvent` | `InventoryData` | Item changes |

## Common Patterns

### Damage System
```csharp
// In damage dealer
var attackData = new AttackData
{
    attacker = gameObject,
    damage = weaponDamage,
    attackType = "Sword"
};
BroadcastEvent(new AttackPerformedEvent { Data = attackData });

// In health system (listener)
public void OnEventReceived(AttackPerformedEvent attackEvent)
{
    TakeDamage(attackEvent.Data.damage);
}
```

### UI Updates
```csharp
// In game logic
var uiData = new UIData { elementId = "HealthBar", value = currentHealth };
BroadcastEvent(new UIUpdateEvent { Data = uiData });

// In UI controller (listener)
public void OnEventReceived(UIUpdateEvent uiEvent)
{
    UpdateUIElement(uiEvent.Data.elementId, uiEvent.Data.value);
}
```

### Player Stats
```csharp
// Broadcasting mana change
var manaData = new PlayerManaData
{
    player = gameObject,
    currentMana = mana,
    maxMana = maxMana,
    manaUsed = spellCost
};
BroadcastEvent(new PlayerManaChangedEvent { Data = manaData });
```

## Debugging Tips

### Add SimpleEventListener
```csharp
// In any GameObject for quick debugging
var debugListener = gameObject.AddComponent<SimpleEventListener>();
debugListener.logPlayerDamage = true;
debugListener.logEnemyDeath = true;
```

### Manual Event Manager Access
```csharp
// If not using EventBehaviour
EventManager.Instance.Broadcast(myEvent);
EventManager.Instance.Subscribe<MyEvent>(this);
```

### Check EventManager Exists
```csharp
if (EventManager.Instance == null)
{
    Debug.LogError("EventManager not found!");
}
```

## Memory Management

### Always Unsubscribe
```csharp
protected override void OnDestroy()
{
    // Unsubscribe from ALL events you subscribed to
    UnsubscribeFromEvent<PlayerDamagedEvent>(this);
    UnsubscribeFromEvent<EnemyDeathEvent>(this);
    base.OnDestroy();
}
```

### Performance Considerations
- Use `QueueEvent()` for non-critical events
- Avoid creating events in Update() loops
- Cache event data objects when possible

## Error Handling

### Safe Event Broadcasting
```csharp
try
{
    BroadcastEvent(myEvent);
}
catch (System.Exception e)
{
    Debug.LogError($"Event broadcast failed: {e.Message}");
}
```

### Null Checks
```csharp
public void OnEventReceived(PlayerDamagedEvent eventData)
{
    if (eventData?.Data?.player == null) return;
    
    // Process event
}
```

---

**Remember**: 
- Always call `base.Awake()` first in derived classes
- Always call `base.OnDestroy()` last in derived classes  
- Unsubscribe from events to prevent memory leaks
- Use descriptive event names and rich data payloads