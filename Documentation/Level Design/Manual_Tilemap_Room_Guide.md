# Manual Tilemap Room Design Guide for RougeLite101

## Overview
This guide will help you create handcrafted tilemap rooms that work seamlessly with your existing DungeonManager system.

## Step-by-Step Workflow

### Phase 1: Create the Room Scene

1. **Create New Scene**
   - File → New Scene
   - Save as "Room_Design_Workspace"

2. **Set Up Camera**
   - Set Main Camera to Orthographic
   - Size: 10-15 (adjust as needed)
   - Position: (0, 0, -10)

### Phase 2: Create Tilemap Structure

1. **Create Tilemap GameObject**
   - Right-click in Hierarchy → 2D Object → Tilemap → Rectangular
   - This creates a GameObject with Tilemap and TilemapRenderer components

2. **Create Multiple Tilemap Layers** (Recommended structure):

   ```text
   RoomContainer (Empty GameObject)
   ├── Background (Tilemap) - Z: 0
   ├── Walls (Tilemap) - Z: -1  
   ├── Details (Tilemap) - Z: -2
   └── Collision (Tilemap) - Z: -3 (invisible collision layer)
   ```

### Phase 3: Paint Your Room

1. **Open Tile Palette**
   - Window → 2D → Tile Palette
   - Create new palette or use existing

2. **Add Your Tiles to Palette**
   - Drag tiles from `Assets/Sprites/Tilemap/Jungle/Tile/` into palette
   - Organize by type (grass, dirt, walls, etc.)

3. **Paint Room Layout**
   - **Background Layer**: Paint floor tiles (grass, dirt)
   - **Walls Layer**: Paint wall tiles (dirt wall variants)
   - **Details Layer**: Add decorative elements
   - **Collision Layer**: Paint invisible collision tiles where walls should block movement

### Phase 4: Add Room Components

1. **Add RoomTemplate Component**
   - Select your RoomContainer
   - Add Component → RoomTemplate

2. **Create Spawn Points**
   - Create empty GameObjects as children of RoomContainer:

     ```text
     RoomContainer
     ├── PlayerSpawn (Empty GameObject)
     ├── ExitAnchor (Empty GameObject)
     ├── EnemySpawns (Empty GameObject - Parent)
     │   ├── EnemySpawn_01
     │   ├── EnemySpawn_02
     │   └── EnemySpawn_03
     ```

3. **Position Spawn Points**
   - **PlayerSpawn**: Where player enters (usually bottom/left)
   - **ExitAnchor**: Where exit door appears (usually top/right)
   - **EnemySpawns**: Spread around the room (3-5 points)

4. **Configure RoomTemplate**
   - Drag spawn points into RoomTemplate component slots
   - Assign a SpawnProfile ScriptableObject (create if needed)

### Phase 5: Set Up Collision

1. **Add TilemapCollider2D**
   - Select Collision tilemap
   - Add Component → TilemapCollider2D
   - Check "Used By Composite" if you want smoother collision

2. **Optional: Add CompositeCollider2D**
   - Select Collision tilemap GameObject
   - Add Component → CompositeCollider2D
   - This merges collision tiles into smoother shapes

### Phase 6: Create Prefab

1. **Save as Prefab**
   - Drag RoomContainer from Hierarchy to `Assets/Prefabs/Rooms/Forests/`
   - Name it descriptively (e.g., "Room_Forest_Swamp_01")

2. **Test the Prefab**
   - Delete from scene
   - Drag prefab back to test it works

### Phase 7: Integrate with DungeonManager

1. **Add to Theme**
   - Open `Assets/ScriptableObjects/Themes/Forest.asset`
   - Add your new room prefab to the roomPrefabs array

2. **Test in Game**
   - Play the game and your room will appear in rotation!

## Pro Tips

### Room Design Guidelines

- **Size**: Keep rooms around 20x15 tiles for consistency
- **Player Flow**: Clear path from PlayerSpawn to exit
- **Enemy Placement**: Ensure spawn points aren't in walls
- **Visual Clarity**: Use consistent tile themes per layer

### Collision Best Practices

- Paint collision tiles for ALL walls
- Leave gaps for doorways
- Test collision by adding a player and running around

### Tilemap Layer Settings

```text
Background: Sorting Layer "Default", Order 0
Walls: Sorting Layer "Default", Order 1  
Details: Sorting Layer "Default", Order 2
Collision: Invisible (uncheck TilemapRenderer)
```

### Creating Spawn Profiles

If you need a custom spawn profile for your room:

1. Right-click in Project → Create → RoomSpawnProfileSO
2. Configure enemy types and spawn counts
3. Assign to your RoomTemplate

## Example Room Structure

```text
Room_Forest_Custom_01
├── Background (Tilemap) - Grass tiles
├── Walls (Tilemap) - Dirt wall tiles  
├── Details (Tilemap) - Decorative elements
├── Collision (Tilemap) - Invisible collision
├── PlayerSpawn (Transform)
├── ExitAnchor (Transform)
└── EnemySpawns
    ├── EnemySpawn_01
    ├── EnemySpawn_02
    └── EnemySpawn_03
```

## Troubleshooting

- **Room not appearing**: Check if it's added to Forest.asset theme
- **No collision**: Ensure collision tilemap has TilemapCollider2D
- **Spawn issues**: Verify RoomTemplate component is properly configured
- **Visual issues**: Check sorting layers and Z positions

Happy room designing! 🎨