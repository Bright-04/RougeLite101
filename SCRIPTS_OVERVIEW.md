# 📂 Enemy Scripts Overview

## What I Created For You

I've added **13 new script files** to your project, organized by enemy type.

```
Assets/Scripts/Enemies/
│
├── BaseEnemy.cs                    ← Shared code for all enemies
│
├── Slime/                          ← (Your existing enemy)
│   ├── SlimeAI.cs
│   ├── SlimeHealth.cs
│   ├── SlimePathFinding.cs
│   ├── EnemyDamageSource.cs
│   └── EnemyDeathNotifier.cs
│
├── Goblin/                         ← NEW! Fast aggressive chaser
│   ├── GoblinAI.cs
│   └── GoblinHealth.cs
│
├── Ghost/                          ← NEW! Teleporting enemy
│   ├── GhostAI.cs
│   └── GhostHealth.cs
│
├── Archer/                         ← NEW! Ranged attacker
│   ├── ArcherAI.cs
│   ├── ArcherHealth.cs
│   └── Arrow.cs
│
├── Orc/                            ← NEW! Tank charger
│   ├── OrcAI.cs
│   └── OrcHealth.cs
│
└── Bat/                            ← NEW! Flying patrol
    ├── BatAI.cs
    └── BatHealth.cs
```

## 🎯 How Each Enemy Is Different

### AI Behavior Comparison

| Enemy | Movement | Detection | Attack | Special |
|-------|----------|-----------|--------|---------|
| **Slime** | Roam + Chase | 10 units | Melee contact | Basic behavior |
| **Goblin** | Always chase | 12 units | Melee contact | No roaming, pure aggression |
| **Ghost** | Float + Chase | 15 units | Melee contact | Teleports every 4s, fades in/out |
| **Archer** | Keep distance | 12 units | Ranged arrows | Retreats if too close |
| **Orc** | Slow walk | 12 units | Charge attack | Charges then stunned |
| **Bat** | Circle patrol | 10 units | Swoop attack | Flies in patterns |

### Stats Comparison

| Enemy | Health | Speed | Damage | Difficulty |
|-------|--------|-------|--------|------------|
| **Slime** | 3 HP | 2.0 | 3 dmg | Easy ⭐ |
| **Goblin** | 4 HP | 3.0 | 3 dmg | Easy ⭐ |
| **Ghost** | 2 HP | 1.5 | 3 dmg | Medium ⭐⭐ |
| **Archer** | 3 HP | 1.5 | 2 dmg | Medium ⭐⭐⭐ |
| **Orc** | 8 HP | 1.5 / 8.0 charge | 3 dmg | Hard ⭐⭐⭐⭐ |
| **Bat** | 2 HP | 2.5 | 3 dmg | Medium ⭐⭐ |

## 📋 What Each Script Does

### BaseEnemy.cs
```
Common functionality shared by all enemies:
- Health management
- Player detection
- Damage taking
- Death handling
- Distance calculations
```

### [Enemy]AI.cs Scripts
```
Controls how the enemy behaves:
- State management (Idle, Chasing, Attacking, etc.)
- Movement logic
- Decision making
- Special abilities (teleport, charge, shoot, patrol)
```

### [Enemy]Health.cs Scripts
```
Manages enemy health:
- Takes damage from player
- Triggers flash effect
- Triggers knockback
- Handles death
- Notifies dungeon manager
```

### Arrow.cs (Special)
```
Projectile for Archer enemy:
- Flies in straight line
- Damages player on hit
- Destroys on wall collision
- Auto-destroys after 3 seconds
```

## 🔄 How It Works Together

### When Enemy Spawns:
1. **Unity creates GameObject** with all components
2. **AI script Awake()** - Gets references to Rigidbody, Knockback, etc.
3. **AI script Start()** - Finds player, starts behavior coroutine
4. **Health script Start()** - Sets starting health

### During Gameplay:
1. **AI checks distance to player** every 0.1-0.2 seconds
2. **AI decides action** based on distance and state
3. **AI moves enemy** using Rigidbody2D
4. **EnemyDamageSource** damages player on contact

### When Player Attacks:
1. **Player weapon hits enemy** (collision/trigger)
2. **Weapon calls** `enemyHealth.TakeDamage(damage)`
3. **Health script:**
   - Reduces health
   - Triggers knockback
   - Triggers flash effect
   - Calls Die() if health <= 0
4. **Die() method:**
   - Notifies dungeon manager
   - Destroys GameObject

## 🛠️ Modifying Behaviors

Want to change how enemies work? Here's what to edit:

### Make Enemy Faster/Slower
**File:** `[Enemy]AI.cs`  
**Line:** `[SerializeField] private float moveSpeed = X;`  
**Change:** Increase/decrease the number

### Make Enemy Tankier
**File:** `[Enemy]Health.cs`  
**Line:** `[SerializeField] private int startingHealth = X;`  
**Change:** Increase the number

### Change Detection Range
**File:** `[Enemy]AI.cs`  
**Line:** `[SerializeField] private float detectionRange = X;`  
**Change:** Increase for earlier detection, decrease for shorter

### Adjust Special Abilities
**Ghost Teleport Cooldown:**
- File: `GhostAI.cs`
- Line: `[SerializeField] private float teleportCooldown = 4f;`

**Archer Shoot Speed:**
- File: `ArcherAI.cs`
- Line: `[SerializeField] private float shootCooldown = 2f;`

**Orc Charge Speed:**
- File: `OrcAI.cs`
- Line: `[SerializeField] private float chargeSpeed = 8f;`

**Bat Patrol Radius:**
- File: `BatAI.cs`
- Line: `[SerializeField] private float patrolRadius = 8f;`

## 💡 Understanding the Code

### State Machine Pattern
Each enemy uses states to control behavior:
```csharp
enum State {
    Idle,      // Not doing anything
    Chasing,   // Running toward player
    Attacking, // Performing attack
    // ... etc
}
```

### Coroutines for AI
Enemies use `IEnumerator` coroutines for their AI loops:
```csharp
private IEnumerator AIBehaviour()
{
    while (!dead)
    {
        // Check player distance
        // Decide what to do
        // Update state
        yield return new WaitForSeconds(0.2f); // Wait a bit
    }
}
```

### SerializeField Variables
Variables marked `[SerializeField]` appear in Unity Inspector:
```csharp
[SerializeField] private float moveSpeed = 2f;
// You can adjust this in Unity without changing code!
```

## 🎓 Learning Resources

If you want to understand the code better:

### Unity Concepts Used:
- **Rigidbody2D**: Physics-based movement
- **Coroutines**: Time-based behavior loops
- **SerializeField**: Expose private variables to Inspector
- **GetComponent**: Access other components on GameObject
- **Transform**: Position and rotation
- **Vector2**: 2D positions and directions
- **Collider2D**: Collision and trigger detection

### C# Concepts Used:
- **Classes & Inheritance**: BaseEnemy → GoblinAI
- **Enums**: State machine states
- **Methods**: Functions that do things
- **Properties**: Variables with special behavior
- **Coroutines**: Special methods that run over time

## 📚 Documentation Files

I created these guides for you:

1. **QUICK_START_ENEMIES.md** - Fast overview, get started quickly
2. **ENEMY_SETUP_GUIDE.md** - Detailed step-by-step instructions
3. **SCRIPTS_OVERVIEW.md** - This file! Technical details

## 🎮 Ready to Go!

All scripts are complete and ready to use. Just:
1. Create GameObjects in Unity
2. Add the components
3. Make prefabs
4. Test!

**Everything is designed to work with your existing game systems!** ✅

---

Need help? Check the other guide files or ask specific questions!
