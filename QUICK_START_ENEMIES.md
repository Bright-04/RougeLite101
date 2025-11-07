# 🎯 Quick Start: Adding New Enemies

## ✅ What's Been Done For You

I've created **5 complete enemy types** with unique AI behaviors:

1. **Goblin** - Fast aggressive chaser
2. **Ghost** - Teleporting phantom
3. **Archer** - Ranged attacker with arrows
4. **Orc** - Tanky charger
5. **Bat** - Flying patrol enemy

All scripts are ready to use in: `Assets/Scripts/Enemies/`

---

## 🚀 Quick Setup (3 Main Steps)

### STEP 1: Create Enemy in Unity (5 minutes per enemy)

1. **Create GameObject**: Right-click in Hierarchy → Create Empty → Name it (e.g., "Goblin")
2. **Add Components**: Click "Add Component" and add these:
   - Sprite Renderer
   - Rigidbody2D (Body Type: Dynamic, Gravity: 0, Freeze Rotation Z)
   - Circle Collider 2D
   - Knockback (your existing script)
   - Flash (your existing script)  
   - EnemyDeathNotifier (your existing script)
   - EnemyDamageSource (your existing script)
   - GoblinAI (new script - use appropriate AI for each enemy)
   - GoblinHealth (new script - use appropriate Health for each enemy)
3. **Add Sprite**: Drag a sprite to Sprite Renderer (or use colored square temporarily)
4. **Set Tag**: Set GameObject tag to "Enemy"
5. **Make Prefab**: Drag GameObject to Assets/Prefabs folder

### STEP 2: Get Sprites (Optional - can use colored squares)

**Free sprite sources:**
- itch.io/game-assets
- opengameart.org
- kenney.nl

**Or create temp sprites:**
- 32x32 colored squares in Paint
- Different color for each enemy type

### STEP 3: Test It!

1. Drag enemy prefab into scene
2. Press Play
3. Watch it detect and chase player!

---

## 📊 Enemy Component Reference

| Component | Purpose | All Enemies Need It? |
|-----------|---------|---------------------|
| Sprite Renderer | Shows the enemy | ✅ Yes |
| Rigidbody2D | Physics/Movement | ✅ Yes |
| Collider2D | Collision detection | ✅ Yes |
| Knockback | Gets knocked back when hit | ✅ Yes |
| Flash | Flashes white when damaged | ✅ Yes |
| EnemyDeathNotifier | Tells dungeon enemy died | ✅ Yes |
| EnemyDamageSource | Damages player on contact | ✅ Yes |
| [Enemy]AI | Controls behavior | ✅ Yes (different per enemy) |
| [Enemy]Health | Takes damage & dies | ✅ Yes (different per enemy) |

---

## 🎮 Enemy Behavior Summary

### Goblin (Easiest to Setup)
- **Behavior**: Sees player → Runs straight at them
- **Speed**: Fast (3)
- **Health**: Medium (4)
- **Special**: Always aggressive, no roaming

### Ghost
- **Behavior**: Floats around → Teleports near player → Chases
- **Speed**: Medium (2)
- **Health**: Low (2)
- **Special**: Fades out and teleports every 4 seconds
- **Note**: Needs Sprite Renderer for fade effect

### Archer ⚠️ (Requires Arrow Prefab)
- **Behavior**: Keeps distance → Shoots arrows at player
- **Speed**: Slow (1.5)
- **Health**: Low (3)
- **Special**: Runs away if you get too close
- **Extra Setup**: 
  1. Create Arrow GameObject with Arrow.cs script
  2. Make Arrow prefab
  3. Assign to Archer's "Arrow Prefab" field
  4. Create "FirePoint" child object on Archer

### Orc
- **Behavior**: Walks slowly → Charges fast → Gets stunned
- **Speed**: Slow walk (1.5), Fast charge (8)
- **Health**: Very High (8)
- **Special**: Charges at player like a bull, stuns self if hits wall

### Bat
- **Behavior**: Flies in circles → Swoops down at player → Returns to patrol
- **Speed**: Medium-Fast (2.5)
- **Health**: Low (2)
- **Special**: Flies in circular pattern, can go over obstacles

---

## 🐛 Common Problems & Fixes

### "Enemy doesn't move!"
- ✅ Check Rigidbody2D → Body Type is **Dynamic**
- ✅ Check Rigidbody2D → Gravity Scale is **0**
- ✅ Check enemy AI script has Move Speed > 0

### "Enemy doesn't detect player!"
- ✅ Make sure player GameObject has **"Player" tag** (most common!)
- ✅ Increase Detection Range in enemy AI script
- ✅ Check player actually exists in scene

### "Enemy doesn't take damage!"
- ✅ Make sure enemy has the Health script attached
- ✅ Check your weapon calls TakeDamage() method
- ✅ Verify colliders are set up correctly

### "Archer doesn't shoot!"
- ✅ Create Arrow prefab first
- ✅ Assign Arrow prefab to Archer's "Arrow Prefab" field
- ✅ Create FirePoint child object and assign it

### "Script not found" errors
- ✅ Right-click Scripts folder → Reimport
- ✅ Check script files are in correct folders
- ✅ Wait for Unity to finish compiling

---

## 📝 Script-to-Enemy Mapping

| Enemy Type | AI Script | Health Script |
|------------|-----------|---------------|
| Goblin | GoblinAI.cs | GoblinHealth.cs |
| Ghost | GhostAI.cs | GhostHealth.cs |
| Archer | ArcherAI.cs | ArcherHealth.cs |
| Orc | OrcAI.cs | OrcHealth.cs |
| Bat | BatAI.cs | BatHealth.cs |

**Plus Arrow.cs for Archer!**

---

## 💡 Pro Tips

1. **Start with Goblin** - It's the simplest (basically an aggressive slime)
2. **Use colored squares** for testing before getting real sprites
3. **Test one enemy at a time** - Don't make all 5 at once
4. **Duplicate prefabs** - After making Goblin, duplicate it and swap scripts
5. **Adjust values in play mode** - Unity shows current values, copy them after testing
6. **Save often** - Ctrl+S in Unity

---

## 🎯 Recommended Order

1. **Goblin** ⭐ (Easiest - just chases player)
2. **Bat** ⭐⭐ (Cool pattern, simple patrol)
3. **Orc** ⭐⭐ (Satisfying charge attack)
4. **Ghost** ⭐⭐⭐ (Teleport is cool but needs sprite)
5. **Archer** ⭐⭐⭐⭐ (Most complex - needs Arrow prefab)

---

## 📖 Full Detailed Guide

For detailed step-by-step instructions, see: **ENEMY_SETUP_GUIDE.md**

---

**You're ready! Start with Goblin and have fun! 🎮**
