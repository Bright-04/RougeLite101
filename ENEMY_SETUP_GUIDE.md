# 🎮 New Enemies Setup Guide for Unity

This guide will help you add 5 new enemy types to your Rouge-Lite game. I've created all the scripts for you - now you just need to set them up in Unity!

---

## 📋 Enemy Types Overview

| Enemy | AI Behavior | Health | Speed | Special Ability |
|-------|-------------|--------|-------|-----------------|
| **Goblin** | Aggressive chaser | 4 HP | Fast | Always chases player |
| **Ghost** | Teleporter | 2 HP | Medium | Teleports near player |
| **Archer** | Ranged attacker | 3 HP | Slow | Shoots arrows, keeps distance |
| **Orc** | Tank charger | 8 HP | Very Slow | Charges at player, gets stunned |
| **Bat** | Flying patrol | 2 HP | Fast | Flies in circles, swoops at player |

---

## 🚀 Step-by-Step Setup (For Each Enemy)

I'll use **Goblin** as an example. Repeat these steps for each enemy type.

### 🎯 RECOMMENDED: Clone the Slime Prefab (EASIEST METHOD - 2 minutes)

This is the **fastest and easiest** way since you already have a working Slime:

1. **In Unity Project window**, navigate to `Assets/Prefabs`
2. **Find the Slime.prefab** file
3. **Right-click on Slime.prefab** → Select **Duplicate** (or press Ctrl+D)
4. **Rename the duplicate** to "Goblin"
5. **Click on the Goblin prefab** to select it
6. **In the Inspector panel**, scroll down to find:
   - Remove `SlimeAI` component (click the ⋮ menu → Remove Component)
   - Remove `SlimeHealth` component
   - Remove `SlimePathFinding` component (if it exists)
7. **Click "Add Component"** and add:
   - Search for `GoblinAI` → Add it
   - Search for `GoblinHealth` → Add it
8. **Configure GoblinAI settings** (in Inspector):
   - Move Speed: `3` (faster than slime's 2)
   - Detection Range: `12`
   - Attack Range: `1.5`
9. **Optional: Change sprite color** to differentiate from slime:
   - Find **Sprite Renderer** component
   - Click the **Color** field
   - Choose a red or brown tint
10. **Save** (Ctrl+S)

**Done! Your Goblin is ready to use!** It will look like a slime but behave more aggressively.

---

### Alternative Method: Create from Scratch (5-10 minutes)

Only use this if you want to learn the full manual process:

1. **Open Unity Editor**
2. **In the Hierarchy**, right-click → **Create Empty** → Name it "Goblin"
3. **Add Components** to the Goblin GameObject:
   - Click **Add Component** → Search for each and add:
     - ✅ **Sprite Renderer** (for the visual)
     - ✅ **Rigidbody2D**
     - ✅ **Capsule Collider 2D** (or Circle Collider 2D)
     - ✅ **Knockback** (your existing script)
     - ✅ **Flash** (your existing script)
     - ✅ **EnemyDeathNotifier** (your existing script)
     - ✅ **EnemyDamageSource** (your existing script)
     - ✅ **GoblinAI** (new script I created)
     - ✅ **GoblinHealth** (new script I created)

### Step 2: Configure the Components

#### Rigidbody2D Settings:
- **Body Type**: Dynamic
- **Gravity Scale**: 0
- **Collision Detection**: Continuous
- **Constraints**: ✅ Freeze Rotation Z

#### Collider Settings:
- **Is Trigger**: ❌ (unchecked)
- Adjust size to fit your sprite

#### EnemyDamageSource Settings:
- **Damage Amount**: 3
- **Damage Cooldown**: 0.5

#### GoblinAI Settings:
- **Starting Health**: 4
- **Move Speed**: 3 (faster than slime)
- **Detection Range**: 12
- **Attack Range**: 1.5
- **Attack Cooldown**: 1

#### GoblinHealth Settings:
- **Starting Health**: 4

### Step 3: Add a Sprite/Visual (Manual Method Only)

If you're using the manual method:

1. **In Sprite Renderer**, click the circle next to "Sprite"
2. **Search for** `SlimeMove` or `idle-Sheet`
3. **Select** any slime sprite frame
4. **Optional:** Change the Color tint to differentiate from regular slimes

If you duplicated the Slime prefab, **skip this step** - it already has sprites!

### Step 4: Set Layer and Tag (Manual Method Only)

If you're using the manual method:

1. **Layer**: Set to your enemy layer (probably "Enemy" or whatever you use)
2. **Tag**: Set to "Enemy"

If you duplicated the Slime prefab, **skip this step** - it already has the correct tags!

### Step 5: Create the Prefab (Manual Method Only)

If you used the **manual method** (creating from scratch in Hierarchy):

1. **Drag the Goblin GameObject** from Hierarchy to `Assets/Prefabs` folder
2. Now you have a reusable Goblin prefab!
3. You can delete the one in the Hierarchy (the prefab is saved)

If you **duplicated the Slime prefab**, it's already a prefab - you're done!

---

## 🎯 Specific Setup for Each Enemy Type

### 1. 👹 GOBLIN (Aggressive Chaser)
**Files created:**
- `Assets/Scripts/Enemies/Goblin/GoblinAI.cs`
- `Assets/Scripts/Enemies/Goblin/GoblinHealth.cs`

**Recommended Settings:**
- Move Speed: 3
- Detection Range: 12
- Starting Health: 4

**Notes:** Simple aggressive enemy. Good for swarms!

---

### 2. 👻 GHOST (Teleporter)
**Files created:**
- `Assets/Scripts/Enemies/Ghost/GhostAI.cs`
- `Assets/Scripts/Enemies/Ghost/GhostHealth.cs`

**Recommended Settings:**
- Move Speed: 1.5
- Detection Range: 15
- Starting Health: 2
- Teleport Cooldown: 4
- Teleport Distance: 3

**Special Setup:**
- Make sure the Ghost has a **Sprite Renderer** (it fades in/out)
- Set the sprite's alpha to 1 initially
- Consider making the Ghost semi-transparent (set Sprite alpha to 0.8)

**Notes:** Tricky enemy that teleports! Low health but hard to hit.

---

### 3. 🏹 ARCHER (Ranged Attacker)
**Files created:**
- `Assets/Scripts/Enemies/Archer/ArcherAI.cs`
- `Assets/Scripts/Enemies/Archer/ArcherHealth.cs`
- `Assets/Scripts/Enemies/Archer/Arrow.cs`

**Recommended Settings:**
- Move Speed: 1.5
- Detection Range: 12
- Starting Health: 3
- Shoot Cooldown: 2
- Optimal Range: 7
- Too Close Range: 4

**IMPORTANT - Create Arrow Prefab:**

1. **Create Empty GameObject** → Name it "Arrow"
2. **Add Components:**
   - Sprite Renderer (add an arrow sprite or a small line)
   - Rigidbody2D (Body Type: Dynamic, Gravity Scale: 0)
   - Capsule Collider 2D (Is Trigger: ✅ checked)
   - Arrow script
3. **Configure Arrow.cs:**
   - Speed: 8
   - Lifetime: 3
   - Damage: 2
4. **Save as Prefab** in `Assets/Prefabs/Arrow.prefab`
5. **In Archer prefab**, drag the Arrow prefab into the **Arrow Prefab** field

**Create Fire Point:**
1. Right-click on Archer → Create Empty → Name it "FirePoint"
2. Position it slightly in front of the Archer (like x: 0.5, y: 0)
3. Drag FirePoint into the Archer's **Fire Point** field

**Notes:** Keeps distance and shoots arrows. Weak but annoying!

---

### 4. 💪 ORC (Tank Charger)
**Files created:**
- `Assets/Scripts/Enemies/Orc/OrcAI.cs`
- `Assets/Scripts/Enemies/Orc/OrcHealth.cs`

**Recommended Settings:**
- Move Speed: 1.5 (slow)
- Detection Range: 12
- Starting Health: 8 (very tanky!)
- Charge Range: 5
- Charge Speed: 8
- Charge Duration: 1
- Charge Cooldown: 3
- Stun Duration: 0.5

**Special Setup:**
- Use a **Box Collider 2D** instead of Circle (better for charging)
- Make the Orc sprite larger than other enemies

**Notes:** Slow but deadly! Charges at player. Boss-like enemy.

---

### 5. 🦇 BAT (Flying Patrol)
**Files created:**
- `Assets/Scripts/Enemies/Bat/BatAI.cs`
- `Assets/Scripts/Enemies/Bat/BatHealth.cs`

**Recommended Settings:**
- Move Speed: 2.5
- Detection Range: 10
- Starting Health: 2
- Patrol Radius: 8
- Swoop Speed: 6
- Swoop Cooldown: 2.5
- Circle Speed: 2

**Special Setup:**
- Set **Sorting Layer** higher so bat appears above ground enemies
- Consider adding a small shadow sprite as a child object

**Notes:** Flies in circles and swoops! Can fly over obstacles.

---

## 🎨 Getting Sprites (If You Don't Have Them)

### Option 1: Free Assets
- **itch.io**: https://itch.io/game-assets/free/tag-enemies
- **OpenGameArt**: https://opengameart.org/
- **Kenney.nl**: https://kenney.nl/assets

### Option 2: Temporary Colored Squares
1. Open Paint or Photoshop
2. Create 32x32 pixel squares with different colors:
   - Goblin: Green
   - Ghost: White/Gray (semi-transparent)
   - Archer: Brown
   - Orc: Dark Green/Black
   - Bat: Purple
3. Save as PNG with transparency

### Option 3: Recolor Existing Slime
- Duplicate your slime sprite
- Change hue in image editor
- Use temporarily until you get proper sprites

---

## 🧪 Testing Each Enemy

After creating each enemy prefab:

1. **Open a test scene**
2. **Drag the enemy prefab** into the scene
3. **Position it away from the player**
4. **Press Play**
5. **Test the behavior:**
   - Does it detect the player?
   - Does it move correctly?
   - Does it attack?
   - Does it take damage?
   - Does it die properly?

---

## 🐛 Common Issues & Solutions

### Issue: "Script not found" error
**Solution:** 
- Make sure all scripts are in the correct folders
- Click on the error and Unity will try to find the script
- Reimport the scripts (right-click → Reimport)

### Issue: Enemy doesn't move
**Solution:**
- Check Rigidbody2D is set to Dynamic
- Check Gravity Scale is 0
- Check Move Speed is > 0
- Make sure player has "Player" tag

### Issue: Enemy doesn't detect player
**Solution:**
- Check player GameObject has tag "Player"
- Increase Detection Range value
- Check there are no walls between enemy and player

### Issue: Archer doesn't shoot
**Solution:**
- Make sure Arrow prefab is assigned
- Check Arrow prefab has Arrow.cs script
- Check Fire Point is created and assigned

### Issue: Enemy takes no damage
**Solution:**
- Make sure enemy has the Health script
- Check player weapon has damage dealing code
- Check colliders are set up correctly

---

## 🎯 Spawning Enemies in Your Dungeon

To add these enemies to your dungeon generation:

1. **Find your dungeon/room generation script**
2. **Add enemy prefab references** at the top:
```csharp
[Header("Enemy Prefabs")]
[SerializeField] private GameObject slimePrefab;
[SerializeField] private GameObject goblinPrefab;
[SerializeField] private GameObject ghostPrefab;
[SerializeField] private GameObject archerPrefab;
[SerializeField] private GameObject orcPrefab;
[SerializeField] private GameObject batPrefab;
```

3. **Create an enemy array** for random selection:
```csharp
private GameObject[] enemyPrefabs;

void Start()
{
    enemyPrefabs = new GameObject[] 
    { 
        slimePrefab, 
        goblinPrefab, 
        ghostPrefab, 
        archerPrefab, 
        orcPrefab, 
        batPrefab 
    };
}
```

4. **Spawn random enemies**:
```csharp
void SpawnEnemy(Vector3 position)
{
    // Random enemy
    GameObject randomEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    Instantiate(randomEnemy, position, Quaternion.identity);
}
```

5. **Or spawn specific mixes**:
```csharp
void SpawnEnemyMix(Vector3 position)
{
    int rand = Random.Range(0, 100);
    
    if (rand < 30) // 30% Slimes (common)
        Instantiate(slimePrefab, position, Quaternion.identity);
    else if (rand < 50) // 20% Goblins (common)
        Instantiate(goblinPrefab, position, Quaternion.identity);
    else if (rand < 65) // 15% Ghosts (uncommon)
        Instantiate(ghostPrefab, position, Quaternion.identity);
    else if (rand < 80) // 15% Archers (uncommon)
        Instantiate(archerPrefab, position, Quaternion.identity);
    else if (rand < 90) // 10% Bats (uncommon)
        Instantiate(batPrefab, position, Quaternion.identity);
    else // 10% Orcs (rare/boss)
        Instantiate(orcPrefab, position, Quaternion.identity);
}
```

---

## ✅ Checklist for Each Enemy

Use this checklist when creating each enemy:

**For Goblin:**
- [ ] Created Goblin GameObject
- [ ] Added all required components
- [ ] Configured Rigidbody2D settings
- [ ] Configured Collider settings
- [ ] Added Sprite/Visual
- [ ] Set Layer and Tag
- [ ] Created Prefab
- [ ] Tested in scene
- [ ] Works correctly

**For Ghost:**
- [ ] Created Ghost GameObject
- [ ] Added all required components (including Sprite Renderer!)
- [ ] Configured Rigidbody2D settings
- [ ] Configured Collider settings
- [ ] Added Sprite/Visual
- [ ] Set Layer and Tag
- [ ] Created Prefab
- [ ] Tested teleportation
- [ ] Works correctly

**For Archer:**
- [ ] Created Archer GameObject
- [ ] Added all required components
- [ ] Configured Rigidbody2D settings
- [ ] Configured Collider settings
- [ ] Created Arrow prefab
- [ ] Created FirePoint child object
- [ ] Assigned Arrow prefab and FirePoint
- [ ] Added Sprite/Visual
- [ ] Set Layer and Tag
- [ ] Created Prefab
- [ ] Tested shooting
- [ ] Works correctly

**For Orc:**
- [ ] Created Orc GameObject
- [ ] Added all required components
- [ ] Configured Rigidbody2D settings
- [ ] Configured Collider settings (Box Collider recommended)
- [ ] Added Sprite/Visual (larger than normal)
- [ ] Set Layer and Tag
- [ ] Created Prefab
- [ ] Tested charging behavior
- [ ] Works correctly

**For Bat:**
- [ ] Created Bat GameObject
- [ ] Added all required components
- [ ] Configured Rigidbody2D settings
- [ ] Configured Collider settings
- [ ] Added Sprite/Visual
- [ ] Set higher Sorting Layer
- [ ] Set Layer and Tag
- [ ] Created Prefab
- [ ] Tested patrol and swoop
- [ ] Works correctly

---

## 🎓 What I Did For You

I created **13 new C# scripts** organized in folders:

```
Assets/Scripts/Enemies/
├── BaseEnemy.cs (shared base class)
├── Goblin/
│   ├── GoblinAI.cs
│   └── GoblinHealth.cs
├── Ghost/
│   ├── GhostAI.cs
│   └── GhostHealth.cs
├── Archer/
│   ├── ArcherAI.cs
│   ├── ArcherHealth.cs
│   └── Arrow.cs
├── Orc/
│   ├── OrcAI.cs
│   └── OrcHealth.cs
└── Bat/
    ├── BatAI.cs
    └── BatHealth.cs
```

Each enemy has:
- ✅ Unique AI behavior
- ✅ Health system (compatible with your existing code)
- ✅ Works with your existing Knockback, Flash, and EnemyDeathNotifier
- ✅ Compatible with your player damage system
- ✅ Commented code for easy understanding

---

## 🆘 Need Help?

If you get stuck:
1. **Check the Console** for error messages (red text)
2. **Make sure Player has "Player" tag** (very common issue!)
3. **Check all components are added** to the enemy prefab
4. **Verify Rigidbody2D settings** (Dynamic, Gravity=0)
5. **Test one enemy at a time** before adding more

---

## 🎮 Next Steps

1. **Start with Goblin** (easiest - similar to Slime)
2. **Then do Bat** (simple patrol behavior)
3. **Then Ghost** (cool teleport effect)
4. **Then Orc** (satisfying charge attack)
5. **Finally Archer** (requires Arrow prefab setup)

**Good luck! You now have 5 unique enemies with different behaviors! 🎉**

Let me know if you need help with any specific step!
