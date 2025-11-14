# Skeleton Enemy - Quick Integration Checklist

## Step-by-Step Setup (5 minutes)

### 1. GameObject Setup

-   [ ] Create new GameObject named "Skeleton"
-   [ ] Add Rigidbody2D (Body Type: Dynamic, Gravity: 0, Freeze Rotation Z)
-   [ ] Add BoxCollider2D or CapsuleCollider2D (adjust size)
-   [ ] Add SpriteRenderer (assign skeleton sprite)
-   [ ] Add Animator (assign Skeleton1_AOC controller)

### 2. Add Core Scripts

-   [ ] Add `SkeletonAI.cs`
-   [ ] Add `SkeletonPathFinding.cs`
-   [ ] Add `SkeletonHealth.cs`

### 3. Add Existing System Scripts

-   [ ] Add `Knockback.cs` (from your project)
-   [ ] Add `Flash.cs` (from your project)
-   [ ] Add `EnemyDeathNotifier.cs` (from your project)
-   [ ] Add `EnemyDeathAnimation.cs` (from your project)

### 4. Configure SkeletonAI Inspector

```
Detection & Range:
├─ Detection Range: 10
├─ Optimal Min Range: 1.2
├─ Optimal Max Range: 2.0
└─ Attack Range: 2.2

Movement:
├─ Approach Speed: 2.5
└─ Retreat Speed: 2.0

Attack Settings:
├─ Attack Prepare Time: 0.3
├─ Attack Duration: 0.5
├─ Attack Cooldown: 1.2
├─ Attack Damage: 1
├─ Use Combo Attack: ☐ (unchecked)
└─ Combo Delay: 0.25

Block/Parry Mechanic:
├─ Enable Block Mechanic: ☑ (checked)
├─ Block Window Duration: 0.25
├─ Block Damage Reduction: 0.5
├─ Block Tint Color: (0.8, 0.8, 1.0, 1.0)
└─ Block VFX Prefab: (Optional - assign if you have one)

Attack Hitbox:
├─ Slash Offset: (1.5, 0)
├─ Slash Radius: 1.8
└─ Player Layer: "Player" (auto-detected)

Visual Feedback:
└─ Block Flash Speed: 15
```

### 5. Configure SkeletonPathFinding Inspector

```
Movement:
└─ Move Speed: 2.5

Obstacle Avoidance:
├─ Obstacle Detection Distance: 0.8
├─ Avoidance Force: 3.5
├─ Obstacle Layer: (auto-detected)
└─ Ray Count: 7

Debug:
└─ Show Debug Rays: ☐ (uncheck for production)
```

### 6. Configure SkeletonHealth Inspector

```
├─ Starting Health: 4
├─ Health Bar: (Assign EnemyHealthBar prefab)
└─ Block Spark VFX Prefab: (Optional)
```

### 7. Animator Controller Setup

**Required Parameters (Already in your animator!):**
| Name | Type | Default |
|------|------|---------||
| isMoving | Bool | false |
| attack | Trigger | - |
| hurt | Trigger | - |
| die | Trigger | - |

✅ **Good news**: Your animator already has all these parameters!

**Animation States:**

1. Idle → `Skeleton1_idle` (loop)
2. Moving → `Skeleton1_movement` (loop)
3. Attack → `Skeleton1_attack` (play once)
4. TakeDamage → `Skeleton1_take_damage` (play once)
5. Death → `Skeleton1_death` (play once)

**Transitions:**

-   Idle ↔ Moving: `isMoving` condition
-   Any State → Attack: `attack` trigger
-   Any State → TakeDamage: `hurt` trigger
-   Any State → Death: `die` trigger
-   Attack → Idle: Exit time
-   TakeDamage → (previous state): Exit time

### 8. Layer & Tag Setup

-   [ ] Set GameObject tag to "Enemy" (if needed)
-   [ ] Set GameObject layer to "Enemies"
-   [ ] Ensure Player is on "Player" layer

### 9. Test in Scene

-   [ ] Place Skeleton in scene
-   [ ] Run game
-   [ ] Verify skeleton idles correctly
-   [ ] Move player close (should approach/retreat)
-   [ ] Let skeleton attack (should see block → attack → cooldown)
-   [ ] Hit skeleton during block window (damage reduced)
-   [ ] Hit skeleton outside block (normal damage)
-   [ ] Verify death at 0 HP

### 10. Save as Prefab

-   [ ] Drag GameObject to Prefabs folder
-   [ ] Name: "Skeleton_Sword" or similar
-   [ ] Delete from scene (or keep for testing)

---

## Troubleshooting Quick Fixes

| Issue                 | Quick Fix                                            |
| --------------------- | ---------------------------------------------------- |
| Doesn't move          | Check Rigidbody2D is Dynamic, not Kinematic          |
| Doesn't attack        | Check Attack Range and Player Layer in SkeletonAI    |
| Attack doesn't hit    | Increase Slash Radius or adjust Slash Offset         |
| Block not working     | Verify Enable Block Mechanic is checked              |
| Animations don't play | Check Animator parameters spelling (case-sensitive!) |
| Gets stuck in walls   | Check Obstacle Layer includes walls                  |
| Health bar missing    | Assign EnemyHealthBar prefab in SkeletonHealth       |

---

## Testing Commands (Debug)

Add these temporary buttons in Inspector (optional):

```csharp
// In SkeletonAI.cs, add at bottom:
#if UNITY_EDITOR
    [ContextMenu("Force Attack")]
    void Debug_ForceAttack()
    {
        state = State.Attacking;
        stateTimer = attackPrepareTime;
    }

    [ContextMenu("Force Block")]
    void Debug_ForceBlock()
    {
        state = State.Blocking;
        stateTimer = blockWindowDuration;
    }
#endif
```

Right-click component in Inspector → select command to test states.

---

## Performance Notes

✅ **Optimized:**

-   State updates at 10 FPS (0.1s intervals) - very lightweight
-   Obstacle detection only when moving
-   Animation parameters updated only when changed

⚠️ **For Many Enemies:**

-   Consider pooling system for spawning/despawning
-   Reduce detection range for distant enemies
-   Disable AI when off-screen (optional optimization)

---

## Quick Values Reference

### Easy Mode (Beginner-Friendly)

```
Starting Health: 3
Attack Damage: 1
Attack Prepare Time: 0.5 (more telegraphing)
Attack Cooldown: 1.5 (slower attacks)
Block Damage Reduction: 0.7 (weaker block)
```

### Normal Mode (Default)

```
Starting Health: 4
Attack Damage: 1
Attack Prepare Time: 0.3
Attack Cooldown: 1.2
Block Damage Reduction: 0.5
```

### Hard Mode (Skilled Players)

```
Starting Health: 5
Attack Damage: 2
Attack Prepare Time: 0.2 (fast!)
Attack Cooldown: 0.9 (aggressive)
Block Damage Reduction: 0.3 (strong block)
Use Combo Attack: ☑ (checked)
```

---

## Done! 🎉

Your Skeleton Sword enemy is ready to fight!

**Next Steps:**

1. Create prefab variant for different skeleton types (spear, axe, etc.)
2. Add sound effects
3. Polish VFX
4. Tune for your game's difficulty curve

See `SKELETON_SETUP_GUIDE.md` for detailed documentation.
