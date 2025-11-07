# 🎮 How to Add Goblin to Your Enemy Spawning System

## 📊 Current Spawning System Overview

Your game uses a **ScriptableObject-based spawning system**. Here's how it works:

```
DungeonManager → Loads Room → Uses RoomSpawnProfile → Spawns Enemies
```

### Flow Breakdown:

1. **DungeonManager** loads a room from a theme
2. Each **Room** has a **RoomTemplate** component with:
   - Enemy spawn points (Transform positions)
   - A **RoomSpawnProfileSO** (ScriptableObject asset)
3. **RoomSpawnProfileSO** contains:
   - List of enemy prefabs to spawn
   - Min/max count for each enemy type
   - Spawn settings (gradual vs instant)
4. **DungeonManager** spawns enemies at random spawn points
5. Exit door unlocks when all enemies are dead

---

## 🎯 3 Ways to Add Goblin (From Easiest to Most Flexible)

### ⭐ Method 1: Modify Existing Spawn Profile (EASIEST - 2 minutes)

This adds Goblin to the current enemy pool.

**Steps:**

1. **In Unity**, navigate to: `Assets/ScriptableObjects/SpawnProfiles/`
2. **Click on** `Forest_Easy.asset`
3. **In the Inspector**, find the **Entries** list
4. **Click the `+` button** to add a new entry
5. **Drag your Goblin prefab** into the new `Prefab` field
6. **Set counts**:
   - Min Count: `1`
   - Max Count: `2`
7. **Save** (Ctrl+S)

**Done!** Now rooms will spawn 1-3 Slimes AND 1-2 Goblins.

---

### ⭐⭐ Method 2: Create New Spawn Profile (RECOMMENDED - 5 minutes)

Create a separate spawn profile for testing Goblin specifically.

**Steps:**

1. **In Unity Project window**, navigate to `Assets/ScriptableObjects/SpawnProfiles/`
2. **Right-click** in the folder → **Create** → **Dungeon** → **Room Spawn Profile**
3. **Rename it** to `Goblin_Test`
4. **Click on the new asset** to select it
5. **In Inspector**, configure:
   ```
   Entries:
   - Size: 1
     - Element 0:
       - Prefab: [Drag your Goblin prefab here]
       - Min Count: 2
       - Max Count: 4
   
   Spawn Gradually: ✓ (checked)
   Initial Delay: 0.5
   Per Spawn Delay Range: X: 0.4, Y: 1.0
   ```
6. **Save** (Ctrl+S)

**Now assign it to a room:**

1. **Open your game scene** (where DungeonManager is)
2. **In Project**, navigate to `Assets/Prefabs/Rooms/`
3. **Find a room prefab** (e.g., Forest room)
4. **Click on it** to open in Inspector
5. **Find the RoomTemplate component**
6. **Drag** `Goblin_Test` into the **Spawn Profile** field
7. **Save the prefab**

**Test:** Play your game - that room will now spawn only Goblins!

---

### ⭐⭐⭐ Method 3: Create Mixed Enemy Spawn Profile (BEST - 10 minutes)

Create a spawn profile with multiple enemy types for variety.

**Steps:**

1. **Right-click** in `SpawnProfiles` folder → **Create** → **Dungeon** → **Room Spawn Profile**
2. **Name it**: `Forest_Mixed`
3. **Configure in Inspector**:
   ```
   Entries:
   - Size: 3
     
     - Element 0 (Slimes - Common):
       - Prefab: [Slime prefab]
       - Min Count: 2
       - Max Count: 4
     
     - Element 1 (Goblins - Common):
       - Prefab: [Goblin prefab]
       - Min Count: 1
       - Max Count: 3
     
     - Element 2 (Bats - Uncommon, if ready):
       - Prefab: [Bat prefab]
       - Min Count: 0
       - Max Count: 2
   
   Spawn Gradually: ✓
   Initial Delay: 0.5
   Per Spawn Delay Range: X: 0.4, Y: 1.0
   ```
4. **Save**
5. **Assign to rooms** as shown in Method 2

**Result:** Each room spawns a mix of 2-4 Slimes, 1-3 Goblins, and maybe 0-2 Bats!

---

## 🔧 Quick Test Without Modifying Anything

Want to test Goblin RIGHT NOW without changing files?

1. **Play your game**
2. **Pause** (or just stop after a room loads)
3. **In Hierarchy**, find the active room GameObject
4. **Right-click** → **Create Empty** → Name it "TestSpawn"
5. **Position it** in the scene where you want Goblin
6. **Drag your Goblin prefab** into the scene at that position
7. **Press Play** (if paused) or continue playing

The Goblin will spawn manually for testing!

---

## 📝 Understanding the Spawn Profile Settings

### Entry Settings:

| Setting | What It Does | Example |
|---------|--------------|---------|
| **Prefab** | The enemy to spawn | Goblin prefab |
| **Min Count** | Minimum enemies to spawn | 1 |
| **Max Count** | Maximum enemies to spawn | 3 |

**Result:** Game randomly picks a number between 1-3 Goblins per room.

### Spawn Behavior Settings:

| Setting | What It Does | Recommended |
|---------|--------------|-------------|
| **Spawn Gradually** | Enemies appear one by one over time | ✓ Yes (more dramatic) |
| **Initial Delay** | Wait time before first enemy spawns | 0.5 seconds |
| **Per Spawn Delay Range** | Time between each enemy spawn | 0.4 to 1.0 seconds |

---

## 🎨 Creating Different Difficulty Profiles

You can create multiple spawn profiles for different difficulty levels:

### Easy Profile (`Forest_Easy`):
```
- Slime: 1-2 (weak, few)
- Goblin: 0-1 (maybe one)
```

### Medium Profile (`Forest_Medium`):
```
- Slime: 2-3
- Goblin: 1-2
- Ghost: 0-1
```

### Hard Profile (`Forest_Hard`):
```
- Slime: 3-5
- Goblin: 2-3
- Ghost: 1-2
- Orc: 0-1 (boss-like)
```

### Boss Room Profile (`Boss_Room`):
```
- Orc: 1 (main boss)
- Goblin: 2-3 (adds)
- Slime: 3-4 (fodder)
```

---

## 🔍 How to Find Your Room Prefabs

Your room prefabs should be in: `Assets/Prefabs/Rooms/`

Each room prefab should have:
- **RoomTemplate** component
- **Enemy Spawns** (empty GameObjects as spawn points)
- **Spawn Profile** field (assign your spawn profile here)

---

## 🧪 Testing Your Goblin Spawning

### Test Checklist:

1. ✅ Create Goblin prefab (or duplicate Slime prefab)
2. ✅ Add GoblinAI and GoblinHealth scripts
3. ✅ Create/modify spawn profile
4. ✅ Assign spawn profile to a room
5. ✅ Play the game
6. ✅ Enter the room
7. ✅ Watch Goblins spawn!
8. ✅ Verify behavior:
   - [ ] Goblins detect player
   - [ ] Goblins chase player (faster than slimes)
   - [ ] Goblins take damage
   - [ ] Goblins die properly
   - [ ] Exit door unlocks after all die

---

## 🐛 Common Issues & Solutions

### Issue: Goblin doesn't spawn
**Check:**
- ✅ Goblin prefab is assigned in spawn profile
- ✅ Spawn profile is assigned to room's RoomTemplate
- ✅ Room has enemy spawn points (Transforms)
- ✅ Min/Max count is greater than 0

### Issue: "Missing Prefab" error
**Solution:**
- Make sure you created the Goblin prefab first
- Drag it from Prefabs folder, not from Hierarchy

### Issue: Goblin spawns but doesn't move
**Solution:**
- Check Goblin has GoblinAI component
- Check Player has "Player" tag
- Check Rigidbody2D settings (Dynamic, Gravity 0)

### Issue: Exit door doesn't unlock
**Solution:**
- Check Goblin has EnemyDeathNotifier component
- Check DungeonManager is tracking enemy deaths
- Look for errors in Console (red messages)

---

## 💡 Pro Tips

### Tip 1: Test with High Spawn Counts
For testing, set high spawn counts:
```
Min Count: 5
Max Count: 10
```
This helps you see how many enemies your game can handle!

### Tip 2: Instant Spawn for Quick Testing
Uncheck **Spawn Gradually** to spawn all enemies instantly - faster for testing!

### Tip 3: Create Multiple Test Profiles
Have different profiles for different scenarios:
- `Test_SingleGoblin` - Just one goblin
- `Test_GoblinSwarm` - 10-20 goblins
- `Test_Mixed` - All enemy types

### Tip 4: Check Console for Debug Messages
DungeonManager logs helpful messages:
- "Spawn profile produced 0 enemies"
- "No spawn profile on this room"
- Enemy count updates

---

## 🎮 Example Spawn Profile Configs

### For Testing Single Goblin:
```yaml
Name: Goblin_Single
Entries:
  - Goblin: 1-1
Spawn Gradually: No
```

### For Goblin Swarm:
```yaml
Name: Goblin_Swarm
Entries:
  - Goblin: 8-12
Spawn Gradually: Yes
Initial Delay: 0.5
Per Spawn Delay: 0.2-0.5
```

### For Progressive Difficulty:
```yaml
Name: Forest_Progressive
Entries:
  - Slime: 2-3
  - Goblin: 1-2
  - Ghost: 0-1
```

---

## 📚 System Architecture Summary

```
ScriptableObject Assets:
├── SpawnProfiles/
│   ├── Forest_Easy.asset       ← Enemy lists & counts
│   ├── Goblin_Test.asset       ← Your new test profile
│   └── Forest_Mixed.asset      ← Mix of enemies
│
└── Themes/
    └── Forest.asset            ← Contains room prefabs

Room Prefabs (in Prefabs/Rooms/):
├── Room has RoomTemplate component
├── RoomTemplate.spawnProfile = Forest_Easy (or other)
└── RoomTemplate.enemySpawns = Array of spawn point Transforms

At Runtime:
1. DungeonManager loads room
2. Reads room's spawn profile
3. Randomly picks count for each enemy type
4. Spawns them at random spawn points
5. Tracks enemy deaths
6. Unlocks exit when all dead
```

---

## ✅ Quick Action Plan

**To add Goblin spawning right now:**

1. **Create Goblin prefab** (duplicate Slime, swap scripts)
2. **Open** `Forest_Easy.asset` in Inspector
3. **Add new entry** with Goblin prefab, counts: 1-2
4. **Play game** - Goblins should spawn!

**Total time: 5 minutes** ⏱️

---

## 🎯 Next Steps

Once Goblin works:
1. Create spawn profiles for other enemies (Ghost, Archer, Orc, Bat)
2. Create different difficulty spawn profiles
3. Assign different profiles to different rooms
4. Balance enemy counts and mix
5. Create special "boss room" profiles

**You now understand the full spawning system!** 🎉

Need help with any specific step? Let me know!
