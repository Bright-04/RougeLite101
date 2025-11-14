# Skeleton AI - System Architecture

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     Skeleton GameObject                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Core Unity Components                       │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  • Transform                                             │   │
│  │  • Rigidbody2D (Dynamic, Gravity: 0)                   │   │
│  │  • BoxCollider2D / CapsuleCollider2D                   │   │
│  │  • SpriteRenderer                                       │   │
│  │  • Animator                                             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │           Enemy-Specific Scripts (NEW)                   │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  ┌──────────────────────────────────────────────────┐   │   │
│  │  │  SkeletonAI.cs                                    │   │   │
│  │  │  ─────────────────────────────────────────────────   │   │
│  │  │  • State Machine (Idle/Approach/Retreat/etc.)     │   │   │
│  │  │  • Range Management                               │   │   │
│  │  │  • Attack Logic & Timing                          │   │   │
│  │  │  • Block Window Management                        │   │   │
│  │  │  • Player Detection & Facing                      │   │   │
│  │  │  • Animation Parameter Control                    │   │   │
│  │  └──────────────────────────────────────────────────┘   │   │
│  │                           ↕                              │   │
│  │  ┌──────────────────────────────────────────────────┐   │   │
│  │  │  SkeletonPathFinding.cs                          │   │   │
│  │  │  ─────────────────────────────────────────────────   │   │
│  │  │  • Movement Execution                             │   │   │
│  │  │  • Obstacle Detection (Raycasts)                 │   │   │
│  │  │  • Avoidance Calculation                         │   │   │
│  │  │  • Speed Control                                  │   │   │
│  │  │  • Rigidbody2D Movement                          │   │   │
│  │  └──────────────────────────────────────────────────┘   │   │
│  │                           ↕                              │   │
│  │  ┌──────────────────────────────────────────────────┐   │   │
│  │  │  SkeletonHealth.cs                               │   │   │
│  │  │  ─────────────────────────────────────────────────   │   │
│  │  │  • Health Management                              │   │   │
│  │  │  • Damage Calculation                             │   │   │
│  │  │  • Block Damage Reduction                        │   │   │
│  │  │  • Death Handling                                 │   │   │
│  │  │  • Animation Triggers (TakeDamage)               │   │   │
│  │  └──────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │         Shared System Scripts (EXISTING)                 │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  • Knockback.cs         (Physics pushback)               │   │
│  │  • Flash.cs             (Damage flash effect)            │   │
│  │  • EnemyDeathNotifier   (Dungeon progress tracking)      │   │
│  │  • EnemyDeathAnimation  (Death visuals & cleanup)        │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## State Machine Flow Diagram

```
                    ┌──────────────────┐
                    │   Game Start     │
                    └────────┬─────────┘
                             ↓
                    ┌─────────────────┐
          ┌────────→│      IDLE       │←─────────┐
          │         └────────┬────────┘          │
          │                  │                    │
          │         (Player detected)             │
          │                  ↓                    │
          │         ┌────────────────┐            │
          │    ┌───→│  Range Check   │            │
          │    │    └───┬────┬────┬──┘            │
          │    │        │    │    │               │
          │    │   Too  │    │    │  Too          │
          │    │   Far  │    │    │  Close        │
          │    │        ↓    │    ↓               │
          │    │   ┌────────┐│┌─────────┐         │
          │    │   │APPROACH│││ RETREAT │         │
          │    │   └───┬────┘│└────┬────┘         │
          │    │       │     │     │              │
          │    │       └─────┼─────┘              │
          │    │             │                    │
          │    │        (In Range)                │
          │    │             ↓                    │
          │    │     ┌──────────────┐             │
          │    └────→│   IN RANGE   │             │
          │          └──────┬───────┘             │
          │                 │                     │
          │         (In Attack Range)             │
          │                 ↓                     │
          │         ┌───────────────┐             │
          │         │   BLOCKING    │             │
          │         │  (0.25 sec)   │             │
          │         └───────┬───────┘             │
          │                 │                     │
          │          (Block Complete)             │
          │                 ↓                     │
          │         ┌───────────────┐             │
          │         │   ATTACKING   │             │
          │         │   (Slash!)    │             │
          │         └───────┬───────┘             │
          │                 │                     │
          │          (Attack Done)                │
          │                 ↓                     │
          │         ┌───────────────┐             │
          └─────────│   COOLDOWN    │─────────────┘
                    │   (1.2 sec)   │
                    └───────────────┘

                    ┌───────────────┐
                    │  If HP ≤ 0    │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │     DEATH     │
                    └───────────────┘
```

---

## Combat Sequence Timeline

```
Time →  0s         0.25s       0.5s        0.75s      1.2s       ∞
        │           │           │           │          │          │
Player: │    ───────────── Approaches Skeleton ────────────────→  │
        │                                                          │
        │                                                          │
Skeleton State:                                                   │
        │                                                          │
     ┌──┴──┐                                                       │
     │IDLE │                                                       │
     └──┬──┘                                                       │
        ↓                                                          │
     ┌─────────┐                                                   │
     │IN RANGE │                                                   │
     └────┬────┘                                                   │
          ↓                                                        │
     ┌─────────┐  ← Block window (damage reduced!)                │
     │BLOCKING │     Flash/Tint visual                            │
     └────┬────┘                                                   │
          ↓                                                        │
     ┌──────────┐                                                  │
     │ATTACKING │  ← Slash hitbox active                          │
     │  (SLASH) │     Damage dealt here!                          │
     └────┬─────┘                                                  │
          ↓                                                        │
     ┌──────────┐                                                  │
     │COOLDOWN  │  ← Cannot attack                                │
     │ (Wait)   │     Vulnerable period                           │
     └────┬─────┘                                                  │
          ↓                                                        │
     ┌──────────┐                                                  │
     │  IDLE    │  ← Ready for next cycle ────────────────────────→
     └──────────┘

Visual Feedback:
        │           │           │           │          │
        │  Normal   │  Flashing │  Attack   │  Normal  │
        │   Color   │  Blue     │   Anim    │  Color   │
        └───────────┴───────────┴───────────┴──────────┘
```

---

## Range Zones Diagram

```
                        Player Position
                              ●
                              │
                              │
     ┌────────────────────────┼────────────────────────┐
     │   Detection Zone       │                        │
     │   (10 units radius)    │                        │
     │                        │                        │
     │    ┌───────────────────┼───────────────────┐    │
     │    │  Attack Zone      │                   │    │
     │    │  (2.2 units)      │                   │    │
     │    │                   │                   │    │
     │    │   ┌───────────────┼───────────────┐   │    │
     │    │   │ Optimal Zone  │               │   │    │
     │    │   │ (1.2-2.0)     │               │   │    │
     │    │   │               │               │   │    │
     │    │   │      ┌────────┼────────┐      │   │    │
     │    │   │      │  Too   │        │      │   │    │
     │    │   │      │ Close  │        │      │   │    │
     │    │   │      │ (<1.2) │        │      │   │    │
     │    │   │      └────────┼────────┘      │   │    │
     │    │   │               ▼               │   │    │
     │    │   │          Skeleton             │   │    │
     │    │   │              ⚔                │   │    │
     │    │   └──────────────────────────────┘   │    │
     │    └──────────────────────────────────────┘    │
     └─────────────────────────────────────────────────┘

Behavior by Zone:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Zone                    │ Skeleton Behavior
━━━━━━━━━━━━━━━━━━━━━━━━┿━━━━━━━━━━━━━━━━━━━━━━━━━━━
Outside Detection       │ Idle (doesn't notice)
Inside Detection        │ Face player, assess range
Too Far (>2.0 units)    │ Approach
Too Close (<1.2 units)  │ Retreat
Optimal (1.2-2.0)       │ Stand ground, prepare attack
Attack Range (<2.2)     │ Block → Attack → Cooldown
━━━━━━━━━━━━━━━━━━━━━━━━┷━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Block Mechanic Flowchart

```
                Player attacks Skeleton
                         │
                         ↓
                ┌────────────────┐
                │ Is Blocking?   │
                └────┬──────┬────┘
                     │      │
                  YES│      │NO
                     │      │
                     ↓      ↓
        ┌────────────────┐ ┌────────────────┐
        │ Apply Damage   │ │ Apply Full     │
        │ Reduction      │ │ Damage         │
        │ (0.5x = 50%)   │ │ (1.0x = 100%)  │
        └────┬───────────┘ └────┬───────────┘
             │                  │
             ↓                  ↓
        ┌────────────────┐ ┌────────────────┐
        │ Spawn Block    │ │ Flash Effect   │
        │ VFX (spark)    │ │                │
        └────┬───────────┘ └────┬───────────┘
             │                  │
             ↓                  ↓
        ┌────────────────┐ ┌────────────────┐
        │ Reduced        │ │ Normal         │
        │ Knockback      │ │ Knockback      │
        │ (8 force)      │ │ (15 force)     │
        └────┬───────────┘ └────┬───────────┘
             │                  │
             ↓                  ↓
        ┌────────────────┐ ┌────────────────┐
        │ Stay in        │ │ Cancel Attack  │
        │ Current State  │ │ → Cooldown     │
        └────────────────┘ └────────────────┘

Example: Player deals 2 damage
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Scenario             │ Damage Taken
━━━━━━━━━━━━━━━━━━━━━┿━━━━━━━━━━━━
Not Blocking         │ 2 HP
Blocking (50% redux) │ 1 HP  ← Rewarding!
Blocking (0% redux)  │ 0 HP  ← Perfect parry!
━━━━━━━━━━━━━━━━━━━━━┷━━━━━━━━━━━━
```

---

## Script Communication Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                    External Systems                           │
├──────────────────────────────────────────────────────────────┤
│  PlayerController  →  Player position, transform              │
│  DungeonManager   ←   Enemy death notification                │
│  Physics2D        ↔   Collision, raycasts, overlap checks     │
└──────────────────────────────────────────────────────────────┘
                                ↕
┌──────────────────────────────────────────────────────────────┐
│                     SkeletonAI.cs                             │
├──────────────────────────────────────────────────────────────┤
│  Commands SkeletonPathFinding:                                │
│    • MoveTo(position)                                         │
│    • StopMoving()                                             │
│    • SetMoveSpeed(speed)                                      │
│                                                                │
│  Updates Animator:                                            │
│    • SetBool("IsMoving")                                      │
│    • SetFloat("MoveSpeed")                                    │
│    • SetTrigger("Attack")                                     │
│                                                                │
│  Provides to SkeletonHealth:                                  │
│    • GetBlockDamageReduction() → float                        │
│    • IsBlocking() → bool                                      │
│                                                                │
│  Receives from SkeletonHealth:                                │
│    • CancelAttack() ← called when hit                         │
└──────────────────────────────────────────────────────────────┘
                                ↕
┌──────────────────────────────────────────────────────────────┐
│                  SkeletonPathFinding.cs                       │
├──────────────────────────────────────────────────────────────┤
│  Receives Commands from AI:                                   │
│    • MoveTo(Vector2) ← target position                        │
│    • StopMoving() ← halt movement                             │
│    • SetMoveSpeed(float) ← adjust speed                       │
│                                                                │
│  Uses:                                                         │
│    • Rigidbody2D.MovePosition() → move skeleton               │
│    • Physics2D.Raycast() → detect obstacles                   │
│    • Physics2D.OverlapCircleAll() → check if stuck            │
│                                                                │
│  Respects:                                                     │
│    • Knockback.gettingKnockedBack ← pause movement            │
└──────────────────────────────────────────────────────────────┘
                                ↕
┌──────────────────────────────────────────────────────────────┐
│                    SkeletonHealth.cs                          │
├──────────────────────────────────────────────────────────────┤
│  Receives from Player:                                         │
│    • TakeDamage(int) ← via IDamageable interface              │
│                                                                │
│  Queries SkeletonAI:                                          │
│    • IsBlocking() ← check block state                         │
│    • GetBlockDamageReduction() ← get damage multiplier        │
│                                                                │
│  Commands SkeletonAI:                                         │
│    • CancelAttack() ← interrupt attack on hit                 │
│                                                                │
│  Updates Animator:                                            │
│    • SetTrigger("TakeDamage")                                 │
│                                                                │
│  Uses Shared Systems:                                          │
│    • Knockback.GetKnockedBack()                               │
│    • Flash.FlashRoutine()                                     │
│    • EnemyDeathNotifier.NotifyDied()                          │
│    • EnemyDeathAnimation.PlayDeathAnimation()                 │
└──────────────────────────────────────────────────────────────┘
```

---

## Key Design Principles

### 1. **Separation of Concerns**

-   **AI** = Decisions (what to do)
-   **PathFinding** = Movement (how to move)
-   **Health** = Survival (taking/dealing with damage)

### 2. **State-Driven Behavior**

-   Clear state machine makes behavior predictable
-   Each state has specific responsibilities
-   Transitions are explicit and logical

### 3. **Player-Focused Design**

-   Always faces player when detected
-   Maintains engaging combat distance
-   Telegraphed attacks reward player skill

### 4. **Modular Integration**

-   Reuses existing systems (Knockback, Flash, etc.)
-   Easy to extend with new mechanics
-   Inspector-tweakable for rapid iteration

### 5. **Performance Conscious**

-   10 FPS update rate (not every frame)
-   Raycasts only when moving
-   Simple distance checks for detection

---

## Customization Points

### Easy Modifications

1. **Change health**: `startingHealth` in SkeletonHealth
2. **Change damage**: `attackDamage` in SkeletonAI
3. **Change speed**: `moveSpeed` in SkeletonPathFinding
4. **Change range**: `optimalMinRange/MaxRange` in SkeletonAI
5. **Enable combo**: `useComboAttack = true` in SkeletonAI

### Advanced Modifications

1. **Add new state**: Extend `State` enum, add `HandleNewState()` method
2. **Multiple attacks**: Create attack variants in `PerformSlashAttack()`
3. **AI Difficulty**: Adjust timings based on game difficulty setting
4. **Formation behavior**: Add leader-follower logic
5. **Player prediction**: Attack where player will be, not where they are

---

This architecture ensures the Skeleton enemy is:
✅ Easy to understand
✅ Easy to modify
✅ Well-integrated with existing systems
✅ Performant
✅ Fun to fight!
