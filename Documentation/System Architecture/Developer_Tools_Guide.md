# Developer Tools Guide

This guide covers the Unity Editor tools created for RougeLite101 development.

## Room Creation Helper

**Location**: `Tools → Dungeon → Room Creation Helper`

**Purpose**: Automate the creation of properly structured tilemap rooms.

### Features

- **Automated Structure Creation**: Creates all necessary tilemap layers and GameObjects
- **Spawn Point Generation**: Automatically places player spawn, exit anchor, and enemy spawns
- **Component Configuration**: Adds and configures RoomTemplate component
- **Collision Setup**: Sets up proper collision layers with optimized settings

### Usage

1. Open the tool from the Unity menu
2. Configure room settings:
   - **Room Name**: Descriptive name for your room
   - **Enemy Spawn Count**: Number of enemy spawn points (1-8)
   - **Room Size**: Approximate size in tiles for spawn point placement
3. Click "Create Room Structure"
4. Paint your tiles using the standard Tile Palette
5. Save as prefab when complete

### Generated Structure

```text
RoomContainer (with RoomTemplate component)
├── Background (Tilemap) - Sorting Order: 0
├── Walls (Tilemap) - Sorting Order: -1
├── Details (Tilemap) - Sorting Order: -2
├── Collision (Tilemap) - Invisible, with TilemapCollider2D
├── PlayerSpawn (Transform)
├── ExitAnchor (Transform)
└── EnemySpawns (Container)
    ├── EnemySpawn_01
    ├── EnemySpawn_02
    └── ... (based on count setting)
```

## Room Template Validator

**Location**: `Tools → Dungeon → Validate Room Templates`

**Purpose**: Validate and fix room prefabs to ensure they work properly with the dungeon system.

### Features

- **Bulk Validation**: Check all room prefabs in the project
- **Missing Component Detection**: Find prefabs without RoomTemplate components
- **Configuration Validation**: Check for missing spawn points and references
- **Auto-Fix Capability**: Automatically add missing RoomTemplate components

### Usage

#### Validate All Rooms

1. Click "Validate All Room Prefabs"
2. Check the Console for validation results
3. Review any warnings or errors reported
4. Fix issues manually or use auto-fix features

#### Fix Selected Rooms

1. Select room prefabs in the Project window
2. Click "Fix Selected Room Prefabs"
3. Tool will add missing RoomTemplate components
4. Manually configure spawn points as needed

### Validation Checks

The validator checks for:

- **Missing RoomTemplate component**
- **Null playerSpawn reference**
- **Null exitAnchor reference**
- **Missing or empty enemySpawns array**

## Integration with Existing Systems

### DungeonManager Integration

Both tools are designed to work seamlessly with the existing DungeonManager system:

- Created rooms automatically work with theme systems
- Spawn points are compatible with enemy spawning
- Exit doors integrate properly with room progression

### Runtime Error Recovery

The DungeonManager includes automatic recovery for common issues:

- **Missing RoomTemplate**: Automatically added at runtime
- **Missing Spawn Points**: Basic spawn points created automatically
- **Missing References**: Graceful fallbacks with warnings

## Best Practices

### Room Creation Workflow

1. **Use the Room Creation Helper** for consistent structure
2. **Paint tiles systematically**: Background → Walls → Details → Collision
3. **Test collision early** by adding a player GameObject
4. **Validate before saving** using the validation tool
5. **Save with descriptive names** following the naming convention

### Naming Conventions

- Room prefabs: `Room_[Theme]_[Descriptor]_[Number]`
  - Example: `Room_Forest_Canyon_01`
- Spawn points: `EnemySpawn_[Number]`
  - Example: `EnemySpawn_01`, `EnemySpawn_02`

### Quality Assurance

1. **Always validate** room prefabs before adding to themes
2. **Test in-game** by adding to a theme and playing
3. **Check spawn point placement** to avoid wall spawning
4. **Verify collision setup** for proper player movement

## Troubleshooting

### Common Issues

**Tool doesn't appear in menu**
- Check that the script is in an `Editor` folder
- Verify Unity has compiled the scripts
- Restart Unity if necessary

**Created rooms don't work in game**
- Ensure the room prefab is added to a ThemeSO asset
- Check that the DungeonManager has the theme in its themes array
- Verify spawn points are properly configured

**Validation shows false positives**
- Check that prefabs are actually room prefabs (not other GameObjects)
- Ensure RoomTemplate component references are assigned
- Clear console and re-run validation

### Debug Information

Both tools provide comprehensive logging:
- **Success messages**: Confirm when operations complete
- **Warning messages**: Alert to potential issues
- **Error messages**: Report critical problems

Check the Unity Console for all tool feedback and debugging information.

## Extending the Tools

### Adding New Features

The tools are designed to be extensible:

1. **Room Creation Helper**: Add new layer types or spawn point configurations
2. **Validator**: Add new validation rules or auto-fix capabilities
3. **Both tools**: Integrate with new room systems or components

### Code Structure

Both tools follow Unity Editor window patterns:
- Clean separation of UI and logic
- Comprehensive error handling
- Helpful user feedback
- Integration with Unity's built-in systems