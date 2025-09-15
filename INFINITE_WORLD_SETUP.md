# 🗺️ Infinite World Testing Setup Guide

## Quick Setup Steps:

### 1. **Create a Player GameObject**
```
1. Create Empty GameObject → Name it "Player"
2. Add Tag "Player" to this GameObject
3. Add Component: SimplePlayerMovement (from Scripts/Player/)
4. Add a visual indicator (e.g., a simple Sprite or basic shape)
5. Position at (0, 0, 0)
```

### 2. **Setup Camera**
```
1. Select Main Camera
2. Add Component: CameraController (from Scripts/Camera/)
3. In CameraController:
   - Drag Player GameObject to "Target" field
   - Set "Use Bounds" to FALSE (for infinite world)
   - Set Follow Speed to 3-5 for smooth following
```

### 3. **Setup World Generator**
```
1. Create Empty GameObject → Name it "WorldManager" 
2. Add Component: InfiniteWorldGenerator (from Scripts/World/)
3. In InfiniteWorldGenerator:
   - Drag Player GameObject to "Player" field
   - Set "Chunk Size" to 50
   - Set "Chunks Around Player" to 3
   - Create some simple prefabs for testing and assign to arrays
```

### 4. **Optional: Add Minimap**
```
1. Create UI Canvas
2. Add MinimapController component
3. Set up basic minimap UI elements
```

## Testing Instructions:

### **Method 1: Manual Movement**
1. Enter Play Mode
2. Use WASD or Arrow Keys to move
3. Hold Shift for fast movement
4. Watch the Scene view to see chunks generate/unload
5. Check Console for generation logs

### **Method 2: Editor Testing Tool**
1. Go to Menu: `RougeLite → Infinite World Tester`
2. Enter Play Mode
3. Use the teleport buttons to jump to different areas
4. Test far corners like (1000, 1000) to see generation

### **Method 3: Scene View Following**
To make Scene view follow the player:
1. Select the Player GameObject in Hierarchy
2. In Scene view, press 'F' to focus on player
3. Double-click player in Scene view to lock focus
4. As you move with WASD, Scene view will follow

## What to Look For:

### ✅ **Successful Generation Signs:**
- New GameObjects appear as you move to new areas
- Console shows "Generated chunk at (X, Y)" messages
- Minimap shows new explored areas
- No lag or freezing during generation
- Smooth camera following

### ❌ **Potential Issues:**
- No new content appearing = Check prefab assignments
- Lag during generation = Reduce objects per chunk
- Camera not following = Check CameraController target assignment
- Errors in console = Check prefab references

## Performance Tips:

1. **Start Simple**: Use basic cube/sphere prefabs for initial testing
2. **Check Generation Logs**: Enable verbose logging in InfiniteWorldGenerator
3. **Monitor Performance**: Use Unity Profiler during testing
4. **Adjust Settings**: Tune chunk size and object counts based on performance

## Quick Test Prefabs:

Create these simple prefabs for testing:
- **Enemy**: Red cube with "Enemy" tag
- **Item**: Yellow sphere with "Item" tag  
- **Structure**: Blue cylinder with "Structure" tag
- **Decoration**: Green cube (no special tag needed)

## Console Commands for Testing:

Add these debug commands in your player movement:
- **Page Up/Down**: Zoom camera in/out
- **Home**: Teleport to origin (0,0)
- **End**: Teleport to random far location

Happy exploring! 🎮✨