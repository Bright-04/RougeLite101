# System Architecture Overview

## Core Systems

### Dungeon Generation System

#### Components

- **DungeonManager**: Main controller for dungeon progression and room loading
- **RoomTemplate**: Component that defines room structure and spawn points
- **ThemeSO**: ScriptableObject that groups room prefabs by theme
- **RoomSpawnProfileSO**: Defines enemy spawn configurations per room

#### Flow

1. DungeonManager builds a plan of rooms using ThemeSO configurations
2. Rooms are loaded sequentially with proper player positioning
3. Enemy spawning is handled based on RoomSpawnProfileSO settings
4. Exit doors are unlocked when all enemies are defeated

### Player Systems

#### PlayerController

- Input handling using Unity's Input System
- 2D movement with Rigidbody2D
- Animation control and sprite flipping
- Singleton pattern for global access

#### PlayerStats

- Health, mana, and stamina management
- Damage calculation with defense
- Regeneration systems
- Critical hit calculations

### Enemy Systems

#### Base Architecture

- Modular component-based design
- Health components (SlimeHealth, etc.)
- AI controllers (SlimeAI, etc.)
- Pathfinding components (SlimePathFinding)

#### Common Components

- **Knockback**: Handles knockback effects from damage
- **Flash**: Visual feedback when taking damage
- **EnemyDeathNotifier**: Notifies DungeonManager of enemy deaths

### UI Systems

- **UIManager**: Health, mana, and stamina display
- **SpellCasterUI**: Spell slot UI with cooldown visualization
- Real-time stat updates

## Design Patterns Used

### Singleton Pattern

- PlayerController: Global player access
- UIManager: Centralized UI management

### Component Pattern

- Enemy systems use composition over inheritance
- Modular health, AI, and movement components

### Observer Pattern

- EnemyDeathNotifier uses events for loose coupling
- UI updates based on player stat changes

### ScriptableObject Pattern

- ThemeSO: Data-driven room configuration
- RoomSpawnProfileSO: Configurable enemy spawning
- Spell system: Spell definitions

## Data Flow

### Room Loading Process

```text
DungeonManager.BuildPlan()
    ↓
Select room from ThemeSO
    ↓
Instantiate room prefab
    ↓
Configure RoomTemplate components
    ↓
Spawn enemies based on RoomSpawnProfileSO
    ↓
Wait for enemy clear
    ↓
Unlock exit door
```

### Combat System Flow

```text
Player attacks enemy
    ↓
DamageSource triggers
    ↓
Enemy health component processes damage
    ↓
Knockback and Flash effects
    ↓
Death check
    ↓
EnemyDeathNotifier event
    ↓
DungeonManager updates enemy count
```

## Error Handling

### Automatic Recovery

- Missing RoomTemplate components are added automatically
- Basic spawn points are created if missing
- Graceful fallbacks for missing references

### Validation Systems

- RoomTemplateValidator: Editor tool for prefab validation
- Runtime validation with detailed logging
- Compilation error detection and reporting

## Performance Considerations

### Room Management

- Only one room active at a time
- Proper cleanup when switching rooms
- Efficient enemy spawning and cleanup

### Memory Management

- ScriptableObjects for data storage
- Component pooling could be added for enemies
- Proper event unsubscription to prevent memory leaks

## Extension Points

### Adding New Enemy Types

1. Create new AI component inheriting from MonoBehaviour
2. Add health component implementing consistent interface
3. Configure spawn profile in RoomSpawnProfileSO
4. Add to room prefabs

### Adding New Room Themes

1. Create new ThemeSO asset
2. Create room prefabs with RoomTemplate components
3. Add to DungeonManager themes array
4. Configure spawn profiles as needed

### Adding New Spells

1. Create spell ScriptableObject
2. Configure in SpellCaster component
3. UI automatically updates to display new spells

## Development Tools

### Editor Extensions

- **RoomCreationHelper**: Automated room structure creation
- **RoomTemplateValidator**: Prefab validation and auto-fixing
- Built-in gizmos for spawn point visualization

### Debugging Features

- Comprehensive logging system
- Visual gizmos for room structure
- Runtime validation with helpful error messages