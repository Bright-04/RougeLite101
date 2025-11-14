# Skeleton Sword Enemy - Setup Guide

## Overview

The Skeleton Sword enemy is a melee combat enemy that maintains optimal sword range, can block/parry attacks, and executes deliberate slash attacks. It's designed to feel like a "sword duel" opponent rather than a mindless attacker.

## Scripts Created

1. **SkeletonAI.cs** - Main behavior controller
2. **SkeletonPathFinding.cs** - Movement and obstacle avoidance
3. **SkeletonHealth.cs** - Health management with blocking mechanic

---

## GameObject Setup

### Required Components

1. **Transform** (default)
2. **Rigidbody2D**

    - Body Type: Dynamic
    - Gravity Scale: 0
    - Collision Detection: Continuous
    - Constraints: Freeze Rotation Z ✓

3. **Collider2D** (BoxCollider2D or CapsuleCollider2D)

    - Set appropriate size for skeleton

4. **SpriteRenderer**

    - Assign Skeleton sprite
    - Sorting Layer: Set appropriately (e.g., "Enemies")

5. **Animator**
    - Create Animator Controller with 5 states:
        - **Idle** (Skeleton1_idle)
        - **Moving** (Skeleton1_movement)
        - **Attack** (Skeleton1_attack)
        - **TakeDamage** (Skeleton1_take_damage)
        - **Death** (Skeleton1_death)

### Animation Parameters Required

Add these parameters to your Animator Controller:

| Parameter Name | Type    | Description                               |
| -------------- | ------- | ----------------------------------------- |
| `isMoving`     | Bool    | Whether skeleton is moving                |
| `attack`       | Trigger | Triggers attack animation                 |
| `hurt`         | Trigger | Triggers damage animation                 |
| `die`          | Trigger | (Already exists, handled by death script) |

**Note**: Your animator already has these parameters! No changes needed.

### Animation Transitions

-   **Idle ↔ Moving**: Condition: `isMoving` true/false
-   **Any State → Attack**: Condition: `attack` trigger
-   **Any State → TakeDamage**: Condition: `hurt` trigger
-   **Any State → Death**: Condition: `die` trigger (handled by death script)

---

## Component Configuration

### SkeletonAI Script

#### Detection & Range

-   **Detection Range**: `10f` - How far skeleton can see player
-   **Optimal Min Range**: `1.2f` - Minimum comfortable sword distance
-   **Optimal Max Range**: `2.0f` - Maximum comfortable sword distance
-   **Attack Range**: `2.2f` - Range to initiate attack

#### Movement

-   **Approach Speed**: `2.5f` - Speed when moving toward player
-   **Retreat Speed**: `2.0f` - Speed when backing away (slightly slower)

#### Attack Settings

-   **Attack Prepare Time**: `0.3f` - Wind-up before slash
-   **Attack Duration**: `0.5f` - How long attack animation lasts
-   **Attack Cooldown**: `1.2f` - Recovery time between attacks
-   **Attack Damage**: `1` - Damage per hit
-   **Use Combo Attack**: `false` - Toggle for 2-hit combo (leave false for simple gameplay)
-   **Combo Delay**: `0.25f` - Time between combo hits (if enabled)

#### Block/Parry Mechanic

-   **Enable Block Mechanic**: `true` - Enable the pseudo-block system
-   **Block Window Duration**: `0.25f` - How long the block window lasts (before attack)
-   **Block Damage Reduction**: `0.5f` - Multiplier (0 = full block, 1 = no reduction)
    -   Example: `0.5f` = 50% damage reduction, `0.0f` = complete block
-   **Block Tint Color**: Light blue `(0.8, 0.8, 1.0, 1.0)` - Visual feedback color
-   **Block VFX Prefab**: (Optional) Assign a spark/glow particle effect

#### Attack Hitbox

-   **Slash Offset**: `(1.5, 0)` - Forward offset from skeleton position
-   **Slash Radius**: `1.8f` - Radius of slash attack circle
-   **Player Layer**: "Player" layer (auto-detected)

#### Visual Feedback

-   **Block Flash Speed**: `15f` - How fast to flash during block window

### SkeletonPathFinding Script

#### Movement

-   **Move Speed**: `2.5f` - Base movement speed (moderate, between Slime and Bat)

#### Obstacle Avoidance

-   **Obstacle Detection Distance**: `0.8f`
-   **Avoidance Force**: `3.5f`
-   **Obstacle Layer**: Auto-detected ("Default", "InvisibleWall")
-   **Ray Count**: `7` - Number of detection rays

#### Debug

-   **Show Debug Rays**: `false` - Enable to visualize obstacle detection

### SkeletonHealth Script

-   **Starting Health**: `4` - More durable than Bat (2) and Slime (3)
-   **Health Bar**: Assign your `EnemyHealthBar` prefab
-   **Block Spark VFX Prefab**: (Optional) Particle effect when block succeeds

---

## Additional Required Components

### From Existing Systems

1. **Knockback** (script from your project)

    - Handles knockback physics when damaged

2. **Flash** (script from your project)

    - White flash effect when taking damage

3. **EnemyDeathNotifier** (script from your project)

    - Notifies DungeonManager when enemy dies

4. **EnemyDeathAnimation** (script from your project)

    - Handles death animation and cleanup

5. **EnemyHealthBar** (prefab from your project)
    - Visual health bar above enemy

---

## Behavior Description

### State Machine Flow

```
┌─────────┐
│  Idle   │ ←─────────────────────────┐
└────┬────┘                            │
     │ (Player detected)               │
     ├─→ Too far?  → Approaching ──────┤
     ├─→ Too close? → Retreating ──────┤
     └─→ Just right? ↓                 │
                                       │
     ┌──────────┐                      │
     │ In Range │                      │
     └─────┬────┘                      │
           │ (Player in attack range)  │
           ↓                            │
     ┌──────────┐                      │
     │ Blocking │ (Brief window)       │
     └─────┬────┘                      │
           ↓                            │
     ┌───────────┐                     │
     │ Attacking │ (Slash attack)      │
     └─────┬─────┘                     │
           ↓                            │
     ┌──────────┐                      │
     │ Cooldown │ ─────────────────────┘
     └──────────┘
```

### Key Behaviors

1. **Range Management**: Skeleton actively maintains optimal sword distance

    - Advances if player too far
    - Retreats if player too close
    - Creates tactical spacing feel

2. **Block Window**: Before attacking, skeleton has brief "blocking" state

    - Damage taken during this window is reduced
    - Visual feedback (tint + flash)
    - Optional VFX spark effect
    - Makes timing important for player

3. **Deliberate Attacks**: Attacks are telegraphed with prepare time

    - Not spammy like Slime
    - Player can interrupt with well-timed hits
    - Feels like a proper duel

4. **Face Tracking**: Always faces player when in detection range

5. **Cancelable Attacks**: If hit during attack animation, skeleton is interrupted
    - Rewards aggressive, skilled play
    - Can be disabled by commenting out in SkeletonHealth.cs

---

## Animation Integration

### Animator Setup

Your animator should use these transitions:

```
Idle State:
- Loop: Skeleton1_idle animation
- Transition to Moving when isMoving == true

Moving State:
- Loop: Skeleton1_movement animation
- Transition to Idle when isMoving == false

Attack State:
- Play: Skeleton1_attack animation
- Triggered by: attack trigger
- Transition back to Idle when complete

TakeDamage State:
- Play: Skeleton1_take_damage animation
- Triggered by: hurt trigger
- Quick transition back to previous state

Death State:
- Play: Skeleton1_death animation
- Triggered by: die trigger
- No exit transition (handled by death script)
```

---

## Layering & Collision Setup

### Required Layers

-   **Player**: Player character layer
-   **Enemies**: Skeleton should be on this layer
-   **Default**: Ground/walls
-   **InvisibleWall**: Invisible boundaries

### Collision Matrix (Physics2D Settings)

Ensure these layers collide:

-   Enemies ↔ Default (walls)
-   Enemies ↔ InvisibleWall
-   Player ↔ Enemies (for contact damage if needed)

---

## Testing Checklist

### Basic Functionality

-   [ ] Skeleton spawns and idles correctly
-   [ ] Detects player within detection range
-   [ ] Faces player when detected
-   [ ] Moves toward player when too far
-   [ ] Backs away when too close
-   [ ] Stops at optimal range

### Combat

-   [ ] Enters blocking state before attack
-   [ ] Tint/flash effect visible during block
-   [ ] Attack animation triggers correctly
-   [ ] Slash hitbox detects player
-   [ ] Damage dealt to player
-   [ ] Knockback applied to player
-   [ ] Cooldown period after attack

### Defense

-   [ ] Takes damage normally outside block window
-   [ ] Reduces damage during block window
-   [ ] Block VFX spawns (if assigned)
-   [ ] Flash effect plays when hit
-   [ ] Knockback applied when hit
-   [ ] Health bar updates correctly
-   [ ] Dies at 0 health with death animation

### Edge Cases

-   [ ] Handles player death/disappearance
-   [ ] Navigates around obstacles
-   [ ] Doesn't get stuck in walls
-   [ ] Multiple skeletons don't overlap too much

---

## Tuning Tips

### Make Skeleton Easier

-   Increase `attackPrepareTime` (more telegraphing)
-   Increase `attackCooldown` (fewer attacks)
-   Reduce `attackDamage`
-   Increase `blockWindowDuration` for more forgiving timing
-   Reduce `startingHealth`

### Make Skeleton Harder

-   Reduce `attackPrepareTime` (faster attacks)
-   Reduce `attackCooldown` (more aggressive)
-   Enable `useComboAttack` for 2-hit combos
-   Increase `attackDamage`
-   Reduce `blockDamageReduction` (better blocking)
-   Increase `startingHealth`

### Adjust Feel

-   **More defensive**: Increase retreat speed, wider optimal range
-   **More aggressive**: Increase approach speed, tighter optimal range
-   **Parry master**: Reduce block window but set damage reduction to 0.0f
-   **Tank**: High health, low damage, slow movement

---

## Visual Effects (Optional)

### Recommended VFX to Create

1. **Block Spark Effect**

    - Small particle burst
    - Blue/white color
    - Quick, sharp flash
    - Attach to `blockVFXPrefab` in SkeletonAI
    - Attach to `blockSparkVFXPrefab` in SkeletonHealth

2. **Slash Trail**

    - Add to attack animation
    - Sword sweep effect
    - Red/white trail

3. **Death Particles**
    - Bones scattering
    - Dust cloud
    - Handle via EnemyDeathAnimation script

---

## Common Issues & Solutions

### Skeleton doesn't move

-   **Check**: Rigidbody2D is set to Dynamic, not Kinematic
-   **Check**: Knockback component not stuck in "getting knocked back" state
-   **Check**: Obstacle layer mask includes walls

### Attacks don't hit player

-   **Check**: Player layer is correctly assigned in SkeletonAI
-   **Check**: Slash offset and radius are appropriate
-   **Check**: Player has collider on "Player" layer
-   **Check**: Attack animation duration matches `attackDuration`

### Block doesn't work

-   **Check**: `enableBlockMechanic` is true
-   **Check**: SkeletonAI component is attached
-   **Check**: Block window duration is reasonable (0.2-0.3s)
-   **Check**: Damage reduction value is < 1.0f

### Animations don't play

-   **Check**: Animator Controller is assigned
-   **Check**: Parameter names match exactly (case-sensitive)
-   **Check**: Animation clips are assigned to states
-   **Check**: Transitions have correct conditions

### Skeleton gets stuck

-   **Check**: Obstacle detection distance is appropriate
-   **Check**: Collider size isn't too large
-   **Check**: Debug rays to visualize detection (`showDebugRays = true`)

---

## Advanced Customization

### Adding Sound Effects

Add these in appropriate methods:

```csharp
// In HandleAttacking(), when attack triggers:
AudioSource.PlayClipAtPoint(swordSlashSound, transform.position);

// In HandleBlocking(), when entering block:
AudioSource.PlayClipAtPoint(blockSound, transform.position);

// In SkeletonHealth.TakeDamage(), when blocking:
if (wasBlocking && damageMultiplier < 1f)
{
    AudioSource.PlayClipAtPoint(parrySpark Sound, transform.position);
}
```

### Adding Hit Stop/Freeze Frame

In `PerformSlashAttack()` when hit is detected:

```csharp
if (damageable != null)
{
    damageable.TakeDamage(attackDamage);
    Time.timeScale = 0.1f; // Slow motion
    Invoke("ResetTimeScale", 0.05f); // Reset after 0.05 real seconds
}

void ResetTimeScale()
{
    Time.timeScale = 1f;
}
```

### Making Block Require Player Input

Modify to only block if player is attacking:

```csharp
// In HandleBlocking(), add check:
if (PlayerIsAttacking()) // Implement this check
{
    isBlocking = true;
    // Spawn VFX, etc.
}
```

---

## Credits & Notes

**Created**: November 2025  
**Unity Version**: Compatible with Unity 2020.3+  
**Dependencies**: Requires existing Knockback, Flash, and Enemy death systems

**Design Philosophy**:

-   Emphasizes player skill over enemy HP sponge
-   Rewards timing and positioning
-   Creates "duel" feeling vs mindless combat
-   Telegraphed attacks but punishing if ignored

---

## Quick Reference: Default Values

```
Health: 4
Detection: 10 units
Optimal Range: 1.2 - 2.0 units
Attack Range: 2.2 units
Move Speed: 2.5 units/sec
Attack Damage: 1
Attack Cooldown: 1.2 sec
Block Window: 0.25 sec
Block Reduction: 50%
```

**That's it! Your Skeleton Sword enemy is ready to duel! ⚔️**
