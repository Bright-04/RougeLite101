# Expanded Event System Documentation

## Overview
The RougeLite event system has been significantly expanded to provide comprehensive coverage for all aspects of a roguelike game. The system now includes **60+ event types** organized into logical categories.

## Event Categories

### 🎮 **Player Events** (8 events)
- **Health/Mana**: PlayerDamagedEvent, PlayerHealedEvent, PlayerDeathEvent, PlayerManaUsedEvent, PlayerManaRestoredEvent
- **Progression**: PlayerLevelUpEvent, PlayerRespawnEvent
- **Movement**: PlayerMovementEvent

### 🕹️ **Enhanced Input & Interaction Events** (5 events)
- **Input**: PlayerInputEvent (with InputData for all input types)
- **Interaction**: PlayerInteractionEvent (with InteractionData)
- **Movement Actions**: PlayerJumpEvent, PlayerLandEvent, PlayerDashEvent

### ⚔️ **Enhanced Combat Events** (8 events)
- **Basic Combat**: AttackPerformedEvent, DamageDealtEvent, SpellCastEvent, CriticalHitEvent
- **Advanced Combat**: DetailedDamageEvent (with comprehensive damage info)
- **Status Effects**: StatusEffectAppliedEvent, StatusEffectRemovedEvent
- **Combo System**: ComboAttackEvent
- **Equipment**: WeaponEquipEvent

### 📈 **Progression & Upgrade Events** (4 events)
- **Experience**: ExperienceGainedEvent (with detailed XP tracking)
- **Character Growth**: SkillUpgradeEvent, StatUpgradeEvent
- **Achievements**: AchievementUnlockedEvent

### 👾 **Enemy Events** (4 events)
- **Lifecycle**: EnemySpawnedEvent, EnemyDeathEvent
- **AI Behavior**: EnemyAggroEvent, EnemyLostAggroEvent

### 🌍 **Environmental & World Events** (6 events)
- **Doors**: DoorStateChangedEvent (with key requirements)
- **Switches**: SwitchToggledEvent (affecting multiple objects)
- **Collectibles**: CollectiblePickedUpEvent (with rarity/value)
- **Area Navigation**: AreaEnteredEvent, AreaExitedEvent (with timing)
- **Items**: ItemCollectedEvent, ItemUsedEvent

### 🎵 **Audio & Visual Effect Events** (4 events)
- **Sound**: PlaySoundEffectEvent (with 3D positioning)
- **Particles**: SpawnParticleEffectEvent (with full configuration)
- **Camera**: ScreenShakeEvent, CameraFocusEvent

### 💾 **Save/Load & Persistence Events** (4 events)
- **Game State**: GameSavedEvent, GameLoadedEvent
- **Settings**: SettingChangedEvent
- **Data**: DataPersistenceEvent

### 🎯 **Game State Events** (5 events)
- **Core States**: GameStartEvent, GamePausedEvent, GameResumedEvent, GameOverEvent
- **Level Progression**: LevelCompleteEvent (with scoring)

### 🎲 **Roguelike-Specific Events** (14 events)
- **Generation**: LevelGeneratedEvent (procedural generation tracking)
- **Loot System**: LootDroppedEvent, TreasureOpenedEvent
- **Room System**: RoomTransitionEvent, RoomClearedEvent
- **Boss Encounters**: BossEncounterStartEvent, BossPhaseChangeEvent, BossDefeatedEvent
- **Power-ups**: PowerUpActivatedEvent, PowerUpExpiredEvent
- **Run Management**: RunStartedEvent, RunEndedEvent (with full statistics)

### 🖥️ **UI Events** (1 event)
- **Interface**: UIUpdateEvent (for UI state synchronization)

## Key Features

### 📊 **Rich Data Structures**
Each event includes comprehensive data structures with relevant information:
```csharp
// Example: Detailed damage with all context
public struct DetailedDamageData
{
    public float baseDamage, finalDamage;
    public string damageType;
    public bool isCritical;
    public float criticalMultiplier;
    public Vector3 hitPosition;
    public Vector3 knockbackDirection;
    public float knockbackForce;
    public GameObject attacker, target;
    public string weaponUsed;
}
```

### 🔍 **Debug Information**
Most events include custom `GetDebugInfo()` methods for detailed logging:
```csharp
public override string GetDebugInfo()
{
    return base.GetDebugInfo() + $" - Combo: {ComboCount}x{ComboMultiplier:F2} '{ComboName}'";
}
```

### 🎯 **Roguelike Focus**
Specialized events for roguelike features:
- **Procedural Generation** tracking
- **Run Statistics** with comprehensive metrics
- **Loot & Treasure** systems
- **Boss Encounter** phases
- **Room-based** progression

### 🔧 **Easy Integration**
All events follow the established pattern:
```csharp
// Broadcasting events
BroadcastEvent(new PlayerJumpEvent(jumpHeight, isDouble, gameObject));

// Listening for events
RegisterForEvent<PlayerJumpEvent>(OnPlayerJump);

// Handling events
private void OnPlayerJump(PlayerJumpEvent eventData)
{
    Debug.Log($"Jump: {eventData.JumpHeight}m");
}
```

## Usage Examples

### 🎮 **Player Actions**
```csharp
// Dash with invulnerability frames
var dashEvent = new PlayerDashEvent(Vector2.right, 5f, true, gameObject);
BroadcastEvent(dashEvent);

// Critical hit with knockback
var damageData = new DetailedDamageData
{
    baseDamage = 50f,
    finalDamage = 125f,
    isCritical = true,
    criticalMultiplier = 2.5f,
    knockbackForce = 15f,
    weaponUsed = "Fire Sword"
};
BroadcastEvent(new DetailedDamageEvent(damageData, gameObject));
```

### 🏆 **Progression System**
```csharp
// Experience gain
var expData = new ExperienceData
{
    experienceGained = 150,
    totalExperience = 2350,
    currentLevel = 5,
    source = "Boss Kill"
};
BroadcastEvent(new ExperienceGainedEvent(expData, gameObject));

// Achievement unlock
BroadcastEvent(new AchievementUnlockedEvent(
    "Dragon Slayer", 
    "Defeat your first dragon", 
    500, 
    gameObject
));
```

### 🌍 **World Interaction**
```csharp
// Room cleared with perfect score
BroadcastEvent(new RoomClearedEvent(
    "Goblin Lair", 
    12, // enemies defeated
    67.4f, // clear time
    true, // perfect clear
    gameObject
));

// Loot drop
var lootData = new LootData
{
    itemName = "Dragon Scale",
    rarity = "Legendary",
    quantity = 1,
    dropChance = 0.01f,
    droppedBy = "Ancient Dragon"
};
BroadcastEvent(new LootDroppedEvent(lootData, gameObject));
```

### 🎵 **Effects & Feedback**
```csharp
// Screen shake for impact
BroadcastEvent(new ScreenShakeEvent(1.2f, 0.5f, Vector3.down, gameObject));

// Particle effect
var particleData = new ParticleEffectData
{
    effectName = "Explosion",
    position = transform.position,
    color = Color.red,
    scale = 2f,
    duration = 3f
};
BroadcastEvent(new SpawnParticleEffectEvent(particleData, gameObject));
```

## Benefits

### 🔗 **Loose Coupling**
- Systems communicate without direct references
- Easy to add/remove game features
- Modular architecture

### 📝 **Comprehensive Logging**
- Debug console shows all events
- Rich debug information for each event
- Easy troubleshooting and development

### 🎯 **Complete Coverage**
- Every game system has appropriate events
- No direct dependencies between systems
- Event-driven architecture throughout

### 🚀 **Performance**
- Efficient event broadcasting
- Optional event filtering
- Minimal memory allocation

## Integration Guide

1. **Inherit from EventBehaviour** for any script that needs events
2. **Register for events** in Start() or OnEnable()
3. **Broadcast events** when actions occur
4. **Handle events** in dedicated methods
5. **Use the ExpandedEventUsageExample** for reference implementations

The expanded event system provides a robust foundation for any roguelike game, ensuring all systems can communicate effectively while remaining loosely coupled and maintainable.