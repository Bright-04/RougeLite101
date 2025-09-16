# Map Expansion Guide

This guide covers different methods to expand your game's playfield, from simple camera adjustments to complex procedural generation.

## Method 1: Camera Bounds Expansion (Quickest)

### For a Simple Background Image
If you're using a single background sprite:

1. **Scale the Background Sprite**:
   - In the Scene view, select your background GameObject
   - In the Transform component, increase the Scale (X: 2, Y: 2 for double size)
   - Or replace with a larger sprite

2. **Adjust Camera Size** (for Orthographic cameras):
   - Select your Main Camera
   - Increase the "Size" value in the Camera component
   - Higher value = larger view area

### For Moving Background
If you want an infinite scrolling background:
- Use a repeating texture with "Wrap Mode" set to "Repeat"
- Create a script to tile/repeat the background as player moves

## Method 2: Tilemap System (Recommended for Larger Maps)

### Setting Up Tilemaps
1. **Create Tilemap GameObject**:
   ```
   GameObject > 2D Object > Tilemap > Rectangular
   ```

2. **Import Tile Assets**:
   - Add tile sprites to Assets/Sprites/Tiles/
   - Create Tile assets from sprites
   - Use Tile Palette window to paint tiles

3. **Paint Your Expanded Map**:
   - Window > 2D > Tile Palette
   - Select tiles and paint larger areas
   - Create multiple layers (Background, Collision, Decoration)

## Method 3: Procedural Generation

### Infinite World Generation
For truly expandable worlds that generate as the player explores:

1. **Chunk-Based System**:
   - Divide world into chunks (e.g., 50x50 units)
   - Generate chunks around player position
   - Unload distant chunks to save memory

2. **Trigger-Based Expansion**:
   - Place invisible triggers at map edges
   - Generate new content when player approaches

## Method 4: Camera Follow System

### Smooth Camera Following
Ensure camera properly follows player in expanded world:

1. **Camera Bounds**:
   - Set minimum/maximum camera positions
   - Prevent camera from showing empty areas

2. **Smooth Following**:
   - Use Lerp or Slerp for smooth camera movement
   - Add slight offset for better feel

## Implementation Examples

### 1. Simple Background Scaling
For immediate results, scale your background:
- Select background GameObject
- Set Transform Scale to (3, 3, 1) for 3x larger map
- Adjust Camera Size from 5 to 15

### 2. Camera Bounds Script
```csharp
public class CameraBounds : MonoBehaviour 
{
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -30f;
    public float maxY = 30f;
    
    void LateUpdate() 
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }
}
```

### 3. Infinite Background Script
```csharp
public class InfiniteBackground : MonoBehaviour 
{
    public GameObject backgroundTile;
    public float tileSize = 20f;
    public int tilesAroundPlayer = 3;
    
    private Transform player;
    private Dictionary<Vector2, GameObject> tiles = new Dictionary<Vector2, GameObject>();
    
    void Update() 
    {
        GenerateTilesAroundPlayer();
        RemoveDistantTiles();
    }
}
```

## Recommendations Based on Your Game

### For Small Expansion (2-4x current size):
1. **Scale existing background** (quickest)
2. **Adjust camera size**
3. **Add more enemy spawn points**

### For Medium Expansion (5-10x current size):
1. **Use Tilemap system**
2. **Create multiple background layers**
3. **Implement camera bounds**
4. **Add minimap for navigation**

### For Large/Infinite Expansion:
1. **Implement chunk-based generation**
2. **Use object pooling for entities**
3. **Add fog of war or visibility system**
4. **Consider performance optimization**

## Performance Considerations

### For Larger Maps:
- **Culling**: Only render objects in camera view
- **Object Pooling**: Reuse enemies/projectiles
- **LOD System**: Lower detail for distant objects
- **Streaming**: Load/unload content dynamically

### Memory Management:
- Unload distant chunks
- Use compressed textures
- Optimize sprite atlases
- Consider texture streaming

## Next Steps

1. **Choose your expansion method** based on desired size
2. **Test performance** with expanded map
3. **Add navigation aids** (minimap, waypoints)
4. **Balance enemy density** for larger space
5. **Consider gameplay implications** (travel time, exploration)

The best approach depends on:
- Current map size vs desired size
- Performance requirements  
- Development time available
- Type of gameplay experience you want