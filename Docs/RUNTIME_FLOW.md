# RougeLite101 Runtime Flow

This guide traces what happens from boot to gameplay, how input/physics/combat events propagate, and where to hook in for new features.

## Boot & Initialization

1) Unity loads the first scene
- If no `EventManager` exists and an `EventBehaviour` awakens (in Debug builds), it creates one.
- Singletons are lazily created on first access (e.g., `GameManager.Instance`, `ProjectilePoolManager.Instance`).

2) Managers initialize
- `GameManager` sets initial state (defaults to `Gameplay` in current setup) and subscribes to key events.
- `InputManager` loads user input settings, sets up maps/bindings.
- Pool managers warm or create pools as needed.

## Input → Movement

- `SimplePlayerMovement`
  - Reads from new Input System actions if assigned; otherwise falls back to `Keyboard.current` and finally legacy `Input.GetKey`.
  - Applies velocity via `Rigidbody2D.linearVelocity` (if configured) or `transform.Translate`.
  - Broadcasts `PlayerMovementEvent` with current velocity and positions for other systems to react (e.g., AI perception, VFX).

- `PlayerController`
  - Computes mouse‑based facing and flips the sprite for combat orientation (movement is intentionally delegated to `SimplePlayerMovement`).

## Combat: Spells & Projectiles

1) Input (digit keys) triggers `SpellCaster` via new Input System callback.
2) `SpellCaster` verifies:
   - Slot index and spell asset assigned
   - Cooldown timers
   - `PlayerStats.currentMana >= Spell.manaCost`
3) On success:
   - Plays optional animation
   - Spawns the spell’s prefab (e.g., fireball near player; lightning at mouse) and points it toward mouse
   - Emits `SpellCastEvent`
   - Calls `PlayerStats.UseMana` → emits `PlayerManaUsedEvent`

4) Projectile lifecycle (for projectile‑based spells or ranged enemies):
   - `Projectile.Initialize` sets position, direction, speed, rotation, and resets lifetime
   - `OnTriggerEnter2D` → validates target layer → `IDamageable.TakeDamage`
   - Hit FX spawned via `EffectPool`
   - Returns to pool through `ProjectilePoolManager` (destroy‑on‑hit or timeout)

## Damage & Health

- Player damage: `PlayerStats.TakeDamage`
  - Applies defense, damage cooldown, reduces HP, emits `PlayerDamagedEvent`
  - If HP <= 0, `Die()` → emits `PlayerDeathEvent` (via `GameManager` listener)

- Enemy damage: e.g., `SlimeHealth`
  - Implements `IDamageable`, plays knockback/flash, reduces integer HP
  - On death, emits `EnemyDeathEvent` and destroys the GO

## UI Reactivity

- `PlayerHealthBar` subscribes to `PlayerDamagedEvent`, `PlayerHealedEvent`, `PlayerDeathEvent` and updates the slider/text/colors.
- `PlayerUIManager` subscribes to health/mana events via `RegisterForEvent` and pulls cooldowns from `SpellCaster` each frame to update overlays.

## World Streaming & LOD

- `InfiniteWorldGenerator` determines the player’s current chunk; when chunk changes:
  - Generates missing chunks around the player and unloads distant ones
  - Emits `ChunkGeneratedEvent`/`ChunkUnloadedEvent`
  - Optionally toggles chunk object active state based on a LOD distance

- `WorldManager` spawns/despawns enemies based on player proximity and world bounds; emits `EnemySpawnedEvent`/`EnemyDespawnedEvent`.

## Game State Transitions

- `GameManager` reacts to:
  - `PlayerDeathEvent` → pause time, mark `GameOver`, optional respawn after delay
  - `EnemyDeathEvent` → track kills → `Victory` when threshold reached
  - Esc key → toggles `Paused` ↔ `Gameplay`

## Common Extension Points

- New HUD reaction: derive from `EventBehaviour` and `SubscribeToEvent<T>` for the event you need.
- New enemy: add `IDamageable`, broadcast `EnemyDeathEvent`, include in spawn lists or world gen.
- New projectile/spell: create pool config or ScriptableObject; launch via `ProjectileLauncher` or `SpellCaster`.

