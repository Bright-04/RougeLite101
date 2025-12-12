# Quick Tilemap Room Design Reference

## 🚀 Quick Start (5 Steps)

### Step 1: Create Room Structure

1. Open Unity
2. Go to `Tools → Dungeon → Room Creation Helper`
3. Enter room name (e.g., "Room_Forest_Canyon_01")
4. Set enemy spawn count (3-5 recommended)
5. Click "Create Room Structure"

### Step 2: Set Up Tile Palette

1. Open `Window → 2D → Tile Palette`
2. Create new palette or use existing
3. Drag tiles from `Assets/Sprites/Tilemap/Jungle/Tile/` into palette

### Step 3: Paint Your Room

**Layer Order (paint in this order):**

1. **Background Layer**: Paint floor (grass, dirt tiles)
2. **Walls Layer**: Paint walls (dirt wall tiles)
3. **Details Layer**: Add decorative elements
4. **Collision Layer**: Paint collision tiles (invisible - shows as red in scene)

### Step 4: Fine-tune Spawn Points

- **PlayerSpawn**: Move to entrance area
- **ExitAnchor**: Move to exit area  
- **EnemySpawns**: Spread around room, avoid walls

### Step 5: Save and Integrate

1. Drag room from Hierarchy → `Assets/Prefabs/Rooms/Forests/`
2. Open `Assets/ScriptableObjects/Themes/Forest.asset`
3. Add your room prefab to the array
4. Test in game!

## 🎨 Available Tiles

### Floor Tiles

- `Grass_0` to `Grass_17` - Various grass patterns
- `dirt_0` to `dirt_3` - Dirt ground
- `spring_0` to `spring_14` - Spring/flower tiles

### Wall Tiles

- `dirt wall_0` to `dirt wall_14` - Wall pieces for different corners/edges

### Painting Tips

- **Grass tiles**: Mix different numbers for variety
- **Wall tiles**: Use different numbers for corners, edges, centers
- **Collision**: Paint EVERY wall tile in collision layer

## 🔧 Tilemap Layers Setup

```text
RoomContainer
├── Background (Sorting Order: 0) - Floor tiles
├── Walls (Sorting Order: 1) - Wall visuals  
├── Details (Sorting Order: 2) - Decorations
└── Collision (Invisible) - Wall collision
```

## 📍 Spawn Point Guidelines

### PlayerSpawn

- Usually bottom or left side of room
- Clear path to rest of room
- Not inside walls!

### ExitAnchor

- Usually top or right side of room
- Clear area around it for door
- Accessible from player spawn

### EnemySpawns (3-5 points)

- Spread around the room
- Not in corners (hard to reach)
- Not blocking player path
- Good mix of open and covered areas

## ⚡ Keyboard Shortcuts

**While Tile Palette is active:**

- `B` - Paint brush
- `I` - Eyedropper (pick tile)
- `U` - Eraser
- `M` - Move tool
- `Shift + Click` - Paint rectangle
- `Ctrl + Z` - Undo

## 🐛 Common Issues & Fixes

**Issue**: Player falls through floor
**Fix**: Add TilemapCollider2D to Collision layer

**Issue**: Can't see tiles I painted
**Fix**: Check sorting order - Background should be 0, Walls should be 1

**Issue**: Room doesn't appear in game
**Fix**: Make sure room prefab is added to Forest.asset theme

**Issue**: Enemies spawn in walls
**Fix**: Move enemy spawn points to open areas

**Issue**: Door doesn't appear
**Fix**: Make sure ExitAnchor is set in RoomTemplate

## 🎯 Pro Tips

1. **Test collision early** - Add a player and test movement
2. **Use tile variations** - Mix different numbered tiles for variety
3. **Plan enemy flow** - Think about how enemies will move around
4. **Keep it consistent** - Similar room sizes make transitions smoother
5. **Save frequently** - Save your scene and prefab often!

---

**Happy Room Designing! 🏰**