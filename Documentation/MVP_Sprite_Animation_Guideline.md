# MVP Sprite & Animation Guideline

## 1. Document Purpose

This document defines the general sprite and animation guideline for the project.  
The goal is to standardize how player, enemy, weapon, projectile, and VFX assets are designed and implemented during the MVP stage.

The project does not aim for high-quality commercial-level animation. Instead, the asset direction prioritizes:

- gameplay readability
- fast production
- visual consistency
- reusable animation structure
- simple Unity integration
- AI-assisted asset generation

The main principle is:

> Different enemies may have different appearances and behaviors, but they should follow the same shared animation structure whenever possible.

---

## 2. MVP Animation Philosophy

For this project, animation should only be created when it helps communicate gameplay state.

Animation is required when it helps the player understand:

- whether an entity is idle
- whether an entity is moving
- whether an enemy is preparing to attack
- when damage or projectile spawning happens
- when an enemy is defeated
- when a hit successfully connects

Animation should not be created only for visual polish during the MVP stage.

### MVP Rule

> Use fewer animation states, but make each state clear and functional.

---

## 3. General Visual Standard

### 3.1 Perspective

All sprites should follow a consistent top-down or three-quarter top-down perspective.

Recommended style:

```text
Top-down / three-quarter top-down pixel art
````

Avoid using sprites that look like side-scroller assets, because they may not match the top-down gameplay view.

---

### 3.2 Recommended Sprite Sizes

| Asset Type    |  Recommended Size |
| ------------- | ----------------: |
| Player        |          48x48 px |
| Small Enemy   |          32x32 px |
| Normal Enemy  |          48x48 px |
| Large Enemy   |          64x64 px |
| Weapon Sprite |          32x32 px |
| Projectile    |          16x16 px |
| VFX           | 32x32 or 48x48 px |

For the MVP, most enemies should use 48x48 px unless they are intentionally small or large.

---

### 3.3 Sprite Style Rules

All sprites should follow these rules:

| Category    | Guideline                            |
| ----------- | ------------------------------------ |
| Perspective | Top-down / three-quarter top-down    |
| Outline     | Simple and consistent outline        |
| Scale       | Consistent relative to player size   |
| Palette     | Limited and consistent color palette |
| Detail      | Avoid unnecessary small details      |
| Silhouette  | Must be readable during gameplay     |
| Background  | Transparent                          |
| Pivot       | Bottom center                        |

---

### 3.4 Unity Import Settings

Recommended Unity import settings:

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Multiple if using sprite sheets
Pixels Per Unit: consistent across project
Filter Mode: Point
Compression: None
Pivot: Bottom Center
```

The recommended pivot is:

```text
Bottom Center
```

This makes top-down sorting, grounding, and positioning easier to control.

---

## 4. Shared Animation Structure

All enemies should follow a shared animation structure unless their gameplay role requires otherwise.

### Core Animation States

```text
Idle
Move
Attack / Shoot
Death
```

### Optional State

```text
Hurt
```

However, for MVP production, Hurt should usually be implemented using code-based flash effects instead of a dedicated sprite animation.

---

## 5. Standard Frame Count

To keep production fast and consistent, use a low frame count for all animations.

| Animation State | Recommended Frames | Notes                                            |
| --------------- | -----------------: | ------------------------------------------------ |
| Idle            |                  4 | Breathing, floating, pulsing, or subtle movement |
| Move            |                  4 | Walk, hop, crawl, fly, or float                  |
| Attack / Shoot  |                  4 | Must include clear anticipation and release      |
| Death           |                0–4 | Prefer shared death VFX for most enemies         |
| Hurt            |                  0 | Use code flash and hit VFX                       |

### MVP Frame Rule

```text
4 frames per animation is enough for MVP.
```

Avoid 6–8 frame animations unless the asset is already available or generated cleanly by AI.

---

## 6. Player Animation Guideline

The player does not need many animations for the MVP.
Since weapons can rotate independently toward the aim direction, the player body only needs to communicate movement clearly.

### Required Player Animations

| Animation |    Direction |          Frames | Required | Notes                               |
| --------- | -----------: | --------------: | -------- | ----------------------------------- |
| Idle      |  1 direction |               4 | Yes      | Subtle breathing or standing motion |
| Walk      | 4 directions | 4 per direction | Yes      | Used for movement readability       |
| Hurt      |         None |               0 | Yes      | Implement with color/material flash |
| Death     |  1 direction |             1–4 | Optional | Can be replaced with fade out       |

### Recommended MVP Player Set

```text
Player_Idle: 4 frames, 1 direction
Player_Walk: 4 frames x 4 directions
Player_Hurt: code-based flash
Player_Death: fade out or 4-frame animation
```

### Not Required for MVP

```text
Pickup animation
Interact animation
Reload animation
Detailed skill cast animation
8-direction animation
Complex death animation
```

---

## 7. Enemy Animation Guideline

Enemy variety should be created mainly through:

* silhouette
* size
* color
* movement speed
* health value
* attack range
* projectile pattern
* AI behavior
* difficulty-scaling parameters

Enemy variety should not require a completely new animation structure for every enemy.

### General Enemy Rule

> Every new enemy should be mapped to an existing enemy archetype before creating animation assets.

Instead of asking:

```text
What unique animations does this enemy need?
```

Ask:

```text
Which existing archetype does this enemy belong to?
```

---

## 8. Shared Enemy Animation States

### 8.1 Idle

#### Purpose

Idle animation shows that the enemy is active and alive.

#### Required For

All enemies.

#### Recommended Frames

```text
4 frames
```

#### Examples

| Enemy Type | Idle Style                |
| ---------- | ------------------------- |
| Slime      | Squash and stretch        |
| Bat        | Wing flap                 |
| Ghost      | Floating up and down      |
| Mushroom   | Subtle breathing          |
| Eye        | Pupil movement or pulsing |
| Golem      | Minimal body movement     |

#### MVP Guideline

Idle animation should be simple.
It only needs enough movement to prevent the enemy from looking static.

---

### 8.2 Move

#### Purpose

Move animation shows that the enemy is chasing, patrolling, repositioning, or approaching the player.

#### Required For

Enemies that move.

#### Optional For

Stationary enemies such as turrets, mushrooms, traps, or crystals.

#### Recommended Frames

```text
4 frames
```

#### Examples

| Enemy Type | Move Style       |
| ---------- | ---------------- |
| Slime      | Hop              |
| Bat        | Faster wing flap |
| Ghost      | Floating drift   |
| Spider     | Crawl            |
| Skeleton   | Basic walk       |
| Golem      | Heavy step       |

#### MVP Shortcut

For floating enemies, Idle and Move can reuse the same animation.

```text
Floating enemy Idle animation can also be used as Move animation.
```

This reduces production time while still keeping the enemy readable.

---

### 8.3 Attack / Shoot

#### Purpose

Attack or Shoot animation communicates that the enemy is about to damage the player.

This is one of the most important animation states for gameplay readability.

#### Required For

All enemies that can attack.

#### Recommended Frames

```text
4 frames
```

#### Standard Attack Timing

```text
Frame 1: Anticipation / wind-up
Frame 2: Stronger wind-up
Frame 3: Damage frame / projectile spawn frame
Frame 4: Recovery
```

The most important frame is:

```text
Frame 3 = damage or projectile spawn timing
```

#### Melee Attack Guideline

Melee enemies should show a clear physical action such as:

* lunge
* bite
* slam
* body impact
* claw swipe

Example structure:

| Frame   | Meaning                 |
| ------- | ----------------------- |
| Frame 1 | Enemy pulls back        |
| Frame 2 | Enemy prepares attack   |
| Frame 3 | Hitbox becomes active   |
| Frame 4 | Enemy returns to normal |

#### Ranged Attack Guideline

Ranged enemies should show charge-up or firing motion.

Example structure:

| Frame   | Meaning                |
| ------- | ---------------------- |
| Frame 1 | Enemy starts charging  |
| Frame 2 | Charge becomes visible |
| Frame 3 | Projectile is spawned  |
| Frame 4 | Recovery               |

#### MVP Guideline

Attack animation does not need to be beautiful, but it must have a readable telegraph.

> The player should be able to understand when an enemy is about to attack.

---

### 8.4 Hurt

#### Purpose

Hurt feedback shows that an enemy has taken damage.

#### MVP Recommendation

Do not create dedicated hurt animations for most enemies.

Use:

```text
Sprite flash
Small hit VFX
Minor knockback
```

Recommended implementation:

```text
Enemy takes damage
→ Sprite flashes white or red
→ HitImpact VFX appears
→ Enemy receives small knockback
```

This is faster, reusable, and clearer than drawing a unique hurt animation for every enemy.

---

### 8.5 Death

#### Purpose

Death feedback shows that an enemy has been defeated.

#### MVP Recommendation

Use a shared death effect for most enemies.

Recommended flow:

```text
Enemy HP reaches 0
→ Enemy sprite disappears
→ DeathPoof VFX appears
→ Enemy object is destroyed or disabled
```

### Death Options

| Option                    | Description                               | Recommended Use      |
| ------------------------- | ----------------------------------------- | -------------------- |
| Shared Death VFX          | One reusable death effect for all enemies | Most MVP enemies     |
| Dedicated Death Animation | Unique animation for a specific enemy     | Important enemy only |
| Fade Out                  | Enemy gradually disappears                | Simple fallback      |

#### MVP Guideline

```text
Use shared DeathPoof VFX for most enemies.
```

Only create unique death animations if the enemy is important for the demo.

---

## 9. Enemy Archetype Guideline

Enemies should be categorized by gameplay role.

Each archetype uses a predefined animation requirement.

---

### 9.1 Melee Chaser

#### Gameplay Role

Melee Chaser enemies move toward the player and attack at close range.

#### Required Animations

```text
Idle: 4 frames
Move: 4 frames
Attack: 4 frames
Death: Shared DeathPoof VFX
```

#### Example Enemies

```text
Slime
Spider
Small demon
Skeleton melee
Rat
```

#### Adaptive Difficulty Parameters

This archetype is useful for demonstrating adaptive difficulty because the AI Director can modify:

```text
Move speed
Attack cooldown
Detection range
Aggression level
Spawn count
```

---

### 9.2 Fast Chaser

#### Gameplay Role

Fast Chaser enemies have low health but high speed.
They pressure the player and force quick movement.

#### Required Animations

```text
Idle: 4 frames
Move: 4 frames
Attack: 4 frames
Death: Shared DeathPoof VFX
```

#### Example Enemies

```text
Bat
Ghost
Crawler
Small wolf
Fast slime
```

#### MVP Shortcut

Fast Chaser can reuse the same animation structure as Melee Chaser.

The difference can come from:

```text
Smaller scale
Higher move speed
Lower HP
Shorter attack cooldown
```

---

### 9.3 Ranged Shooter

#### Gameplay Role

Ranged Shooter enemies attack the player from a distance using projectiles.

#### Required Animations

```text
Idle: 4 frames
Shoot: 4 frames
Death: Shared DeathPoof VFX
Projectile: 1 sprite
```

#### Optional Animation

```text
Move: 4 frames
```

If the enemy is stationary, Move animation is not required.

#### Example Enemies

```text
Mushroom shooter
Floating eye
Ghost mage
Crystal caster
Skeleton archer
```

#### Adaptive Difficulty Parameters

The AI Director can modify:

```text
Fire rate
Projectile speed
Projectile count
Projectile spread
Attack cooldown
Accuracy
```

---

### 9.4 Tank Enemy

#### Gameplay Role

Tank enemies are slow but durable.
They create spatial pressure and force the player to reposition.

#### Required Animations

```text
Idle: 4 frames
Move: 4 frames
Attack: 4 frames
Death: Shared DeathPoof VFX
```

#### Example Enemies

```text
Golem
Armored slime
Large skeleton
Stone creature
Heavy knight
```

#### MVP Shortcut

Tank enemies can use the same animation structure as Melee Chaser.

The difference can come from:

```text
Larger scale
More HP
Slower movement speed
Higher damage
Higher knockback resistance
```

---

### 9.5 Stationary Shooter / Turret

#### Gameplay Role

Stationary Shooter enemies do not move.
They attack from a fixed position.

#### Required Animations

```text
Idle: 4 frames
Shoot: 4 frames
Death: Shared DeathPoof VFX
Projectile: 1 sprite
```

#### Not Required

```text
Move animation
```

#### Example Enemies

```text
Mushroom
Crystal turret
Totem
Eye tower
Plant shooter
```

#### MVP Value

This archetype is highly recommended for MVP because it creates enemy variety with very low animation cost.

---

## 10. Recommended MVP Enemy Set

For the MVP, the project can include multiple enemy types while still using a small shared animation guideline.

Recommended set:

| Enemy           | Archetype          | Animation Cost | Gameplay Value |
| --------------- | ------------------ | -------------: | -------------- |
| Slime           | Melee Chaser       |            Low | High           |
| Bat / Ghost     | Fast Chaser        |            Low | Medium         |
| Mushroom / Eye  | Ranged Shooter     |            Low | High           |
| Golem           | Tank Enemy         |         Medium | Medium         |
| Crystal / Totem | Stationary Shooter |            Low | Medium         |

This creates enemy variety without requiring a unique animation system for every enemy.

---

## 11. Shared Enemy Animation Matrix

| Animation State | Required             | Frames | Used By        | Notes                                |
| --------------- | -------------------- | -----: | -------------- | ------------------------------------ |
| Idle            | Yes                  |      4 | All enemies    | Basic active state                   |
| Move            | Conditional          |      4 | Moving enemies | Can reuse Idle for floating enemies  |
| Attack          | Conditional          |      4 | Melee enemies  | Damage usually happens on frame 3    |
| Shoot           | Conditional          |      4 | Ranged enemies | Projectile usually spawns on frame 3 |
| Hurt            | No dedicated sprite  |      0 | All enemies    | Use flash + hit VFX                  |
| Death           | Shared VFX preferred |    0–4 | All enemies    | Use shared DeathPoof for MVP         |

---

## 12. Weapon Animation Guideline

Weapons should be simple and reusable during the MVP stage.

### 12.1 General Rule

Weapons should not require full-body animation.

Recommended structure:

```text
Player body handles movement.
Weapon sprite rotates toward aim direction.
Projectile spawns from FirePoint.
```

Recommended Unity hierarchy:

```text
Player
├── BodySprite
├── WeaponPivot
│   └── WeaponSprite
├── FirePoint
└── VFXSpawnPoint
```

---

### 12.2 Weapon Asset Requirements

| Weapon Type   | Required Sprite  | Required Animation  |
| ------------- | ---------------- | ------------------- |
| Ranged Weapon | 1 in-hand sprite | None                |
| Projectile    | 1 sprite         | None                |
| Melee Weapon  | 1 in-hand sprite | Optional slash VFX  |
| Magic Weapon  | 1 in-hand sprite | Optional cast flash |

### MVP Weapon Rule

```text
Use static weapon sprites and rotate them through code.
```

Do not create complex weapon animations unless necessary for readability.

---

### 12.3 Not Required for MVP

```text
Reload animation
Draw animation
Charge animation
Weapon-specific body animation
Unique animation for every weapon
```

---

## 13. Projectile Guideline

Projectiles should be readable, simple, and easy to rotate.

### Projectile Requirements

| Projectile Type  | Sprite Requirement | Animation Requirement |
| ---------------- | ------------------ | --------------------- |
| Arrow            | 1 sprite           | None                  |
| Bullet           | 1 sprite           | None                  |
| Magic bolt       | 1–2 sprites        | Optional              |
| Fireball         | 2–4 frames         | Optional              |
| Enemy projectile | 1 sprite           | None                  |

### MVP Rule

```text
Most projectiles only need one sprite.
```

Motion should be handled through code, not through animation.

---

## 14. VFX Guideline

VFX should replace many dedicated animations during MVP production.

### Required VFX

| VFX                     | Frames | Purpose                    |
| ----------------------- | -----: | -------------------------- |
| HitImpact               |      4 | Confirms successful hit    |
| DeathPoof               |    4–6 | Shared enemy death effect  |
| SlashArc                |    4–6 | Shows melee attack range   |
| MuzzleFlash / CastFlash |    2–4 | Shows ranged attack origin |

---

### 14.1 Reusable VFX Principle

A single VFX should be reused across multiple entities when possible.

Example:

```text
HitImpact:
- enemy hit
- projectile impact
- melee hit

DeathPoof:
- slime death
- bat death
- mushroom death
- generic enemy death
```

Avoid creating unique VFX for every enemy unless required for a specific boss or important enemy.

---

## 15. Animation Timing Guideline

### 15.1 Attack Timing

All attack and shoot animations should follow a consistent timing structure.

```text
Frame 1-2: Telegraph / preparation
Frame 3: Damage / projectile spawn
Frame 4: Recovery
```

### 15.2 Gameplay Synchronization

The damage or projectile should not happen randomly during the animation.

Recommended timing:

```text
Melee Attack:
Damage hitbox activates on frame 3.

Ranged Attack:
Projectile spawns on frame 3.

VFX:
HitImpact spawns when damage is confirmed.
DeathPoof spawns when enemy HP reaches 0.
```

---

## 16. Unity Implementation Guideline

### 16.1 Shared Logical States

Different enemies may have different animation clips, but they should map to the same logical states.

Recommended shared states:

```text
Idle
Move
Attack
Dead
```

Examples:

```text
Slime_Attack      -> Attack
Bat_Attack        -> Attack
Mushroom_Shoot    -> Attack
Golem_HeavyAttack -> Attack
```

This makes enemy logic easier to standardize.

---

### 16.2 Animator Parameters

Recommended enemy Animator parameters:

```text
IsMoving
IsAttacking
IsDead
```

Optional:

```text
MoveX
MoveY
AttackType
```

For MVP, keep Animator parameters minimal.

---

### 16.3 Damage Feedback

Hurt feedback should be handled through scripts.

Recommended damage feedback:

```text
Flash sprite color
Spawn HitImpact VFX
Apply small knockback
Play hit sound
```

This avoids the need for individual hurt animations.

---

## 17. AI-Assisted Asset Generation Guideline

AI can be used to generate base sprites and simple sprite sheets, but all outputs should follow the same standard.

### 17.1 Generate by Archetype

Prompt AI based on enemy archetype, not overly specific individual details.

Example prompt:

```text
Create a 2D top-down pixel art enemy sprite sheet for a melee chaser enemy.
48x48 pixels per frame.
Transparent background.
Consistent outline.
Simple readable silhouette.
4-frame idle animation.
4-frame movement animation.
4-frame attack animation with clear wind-up.
MVP game asset style.
```

Example for ranged enemy:

```text
Create a 2D top-down pixel art enemy sprite sheet for a stationary ranged mushroom enemy.
48x48 pixels per frame.
Transparent background.
Consistent outline.
Simple readable silhouette.
4-frame idle animation.
4-frame shooting animation with projectile release on frame 3.
MVP game asset style.
```

---

### 17.2 AI Output Validation

Every AI-generated asset must be checked before import.

| Check                     | Requirement |
| ------------------------- | ----------- |
| Transparent background    | Required    |
| Correct frame size        | Required    |
| Consistent scale          | Required    |
| Clear silhouette          | Required    |
| Top-down perspective      | Required    |
| No excessive noise        | Required    |
| Usable animation sequence | Required    |
| Consistent outline        | Recommended |

---

### 17.3 AI Production Workflow

Recommended workflow:

```text
1. Generate one base enemy sprite sheet.
2. Clean up frame size and transparency.
3. Import into Unity.
4. Test readability in gameplay.
5. Generate additional enemies using the same format.
```

Do not generate too many enemies before testing the first one in Unity.

---

## 18. Naming Convention

### 18.1 Sprite File Naming

Recommended format:

```text
[category]_[name]_[state]_[frame].png
```

Examples:

```text
enemy_slime_idle_00.png
enemy_slime_idle_01.png
enemy_slime_move_00.png
enemy_slime_attack_02.png

enemy_mushroom_idle_00.png
enemy_mushroom_shoot_02.png

player_walk_down_00.png
player_walk_left_03.png
```

---

### 18.2 Animation Clip Naming

Recommended format:

```text
AC_[Category]_[Name]_[State]
```

Examples:

```text
AC_Player_Idle
AC_Player_Walk_Down
AC_Enemy_Slime_Idle
AC_Enemy_Slime_Move
AC_Enemy_Slime_Attack
AC_Enemy_Mushroom_Shoot
```

---

### 18.3 Animator Controller Naming

Recommended format:

```text
AN_[Category]_[Name]
```

Examples:

```text
AN_Player
AN_Enemy_Slime
AN_Enemy_Mushroom
AN_Enemy_Golem
```

---

### 18.4 VFX Naming

Recommended format:

```text
VFX_[EffectName]
```

Examples:

```text
VFX_HitImpact
VFX_DeathPoof
VFX_SlashArc
VFX_MuzzleFlash
```

---

## 19. Folder Structure Guideline

Recommended Unity folder structure:

```text
Assets/
├── Art/
│   ├── Characters/
│   │   └── Player/
│   ├── Enemies/
│   │   ├── Slime/
│   │   ├── Bat/
│   │   ├── Mushroom/
│   │   ├── Golem/
│   │   └── Shared/
│   ├── Weapons/
│   ├── Projectiles/
│   └── VFX/
│
├── Animations/
│   ├── Player/
│   ├── Enemies/
│   └── VFX/
│
└── AnimatorControllers/
    ├── Player/
    └── Enemies/
```

Shared effects should be placed in shared folders instead of duplicated per enemy.

---

## 20. MVP Asset Scope

The recommended MVP asset scope is:

### Player

```text
Idle: 4 frames, 1 direction
Walk: 4 frames x 4 directions
Hurt: code flash
Death: fade out or 4 frames
```

### Enemy Archetypes

```text
Melee Chaser:
- Idle
- Move
- Attack
- Shared DeathPoof

Fast Chaser:
- Idle
- Move
- Attack
- Shared DeathPoof

Ranged Shooter:
- Idle
- Shoot
- Projectile
- Shared DeathPoof

Tank Enemy:
- Idle
- Move
- Attack
- Shared DeathPoof

Stationary Shooter:
- Idle
- Shoot
- Projectile
- Shared DeathPoof
```

### Weapons

```text
Ranged weapon sprite
Melee weapon sprite
Projectile sprite
Slash arc VFX
Muzzle / cast flash VFX
```

### VFX

```text
HitImpact
DeathPoof
SlashArc
MuzzleFlash / CastFlash
```

---

## 21. Production Priority

### Priority 1: Gameplay Readability

Must be completed first:

```text
Player idle and walk
Basic enemy idle and move
Enemy attack / shoot animation
Projectile sprite
Hit feedback
Enemy death feedback
```

### Priority 2: Enemy Variety

Add more enemy types using the same shared animation structure.

Recommended order:

```text
1. Melee Chaser
2. Ranged Shooter
3. Fast Chaser
4. Tank Enemy
5. Stationary Shooter
```

### Priority 3: Visual Polish

Only polish after the core gameplay and adaptive difficulty loop are functional.

Possible polish:

```text
Better attack VFX
Improved death effect
Simple projectile trail
Animation speed tuning
Better color palette consistency
```

---

## 22. Scope Control Rules

To prevent the asset scope from becoming too large, follow these rules:

### Rule 1

```text
Do not create a new animation category unless the gameplay requires it.
```

### Rule 2

```text
Do not create unique hurt animations for every enemy.
```

### Rule 3

```text
Do not create 8-direction animation for MVP.
```

### Rule 4

```text
Use shared VFX wherever possible.
```

### Rule 5

```text
Enemy variety should come from behavior and stats first, animation second.
```

---

## 23. Final Guideline Summary

The project should use a shared animation guideline for all MVP enemies.
Most enemies should be built from the same basic animation states:

```text
Idle
Move
Attack / Shoot
Death
```

Hurt feedback should be handled through code-based flash effects and shared hit VFX.
Death should usually be handled through a shared DeathPoof effect.

This approach allows the project to include multiple enemy types while keeping the asset workload manageable. It also supports the main technical goal of the game: demonstrating combat, procedural dungeon generation, player metric tracking, and adaptive difficulty scaling.

The recommended production direction is:

```text
Many enemy types.
Few shared animation states.
Reusable VFX.
Simple Unity integration.
MVP-level visual quality.
```


