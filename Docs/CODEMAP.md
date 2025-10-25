# RougeLite101 Code Map

This is a pragmatic map of important scripts, their roles, and how they interconnect. Use it when you need to find the right place to add functionality or debug a flow.

## Managers

- Game state: `Assets/Scripts/Managers/GameManager.cs`
  - Singleton game state machine. Listens to player/enemy events; coordinates pause, victory, game over, respawn.
- Input: `Assets/Scripts/Managers/InputManager.cs`
  - Centralizes input handling for both Input Systems, buffering, bindings, and settings persistence.
- Audio: `Assets/Scripts/Managers/AudioManager.cs`
  - Audio SFX/BGM facade.
- Save: `Assets/Scripts/Managers/SaveManager.cs`
  - Save/load hooks (PlayerPrefs or files, depending on implementation).
- Scene transitions: `Assets/Scripts/Managers/SceneTransitionManager.cs`
  - Cross‑scene loading and transitions.
- UI: `Assets/Scripts/Managers/UIManager.cs`
  - Higher‑level coordination across UI widgets.

## Event System

- Event bus: `Assets/Scripts/Events/EventManager.cs`
  - Typed subscribe/broadcast; optional queue; logs.
- Base types: `Assets/Scripts/Events/GameEvent.cs`
  - `GameEvent`, `GameEvent<TData>`.
- Helpers: `Assets/Scripts/Events/EventBehaviour.cs`
  - Inherit to get safe bus access and helpers.
- Catalog: `Assets/Scripts/Events/GameEvents.cs`
  - Player/Enemy/Combat/Run/World events, including `PlayerDamagedEvent`, `SpellCastEvent`, `EnemyDeathEvent`, `ChunkGeneratedEvent`.

## Player

- Movement: `Assets/Scripts/Player/SimplePlayerMovement.cs`
  - Reads input, moves via Rigidbody2D or transform, emits `PlayerMovementEvent`.
- Facing/Combat orientation: `Assets/Scripts/Player/PlayerController.cs`
  - Mouse‑based facing and sprite flipping for attack direction.
- Stats & damage: `Assets/Scripts/Player/PlayerStats.cs`
  - HP/Mana/Stamina, regen, damage intake; emits Player health/mana events.
- Spells system: `Assets/Scripts/Player/SpellSystem/`
  - `Spell.cs` (SO), `SpellCaster.cs` (input → mana/cooldown → spawn prefab → `SpellCastEvent`), sample `FireballSpell.cs`, `LightningSpell.cs`.
- Melee: `Assets/Scripts/Player/Sword.cs` and related animation helpers.

## Combat

- Damage contract: `Assets/Scripts/Combat/IDamageable.cs`
- Base projectile: `Assets/Scripts/Combat/Projectile.cs`
  - Init, motion, trigger collision, hit processing, pooling lifecycle, events.
- Specialized projectiles: `FireProjectile.cs`, `IceProjectile.cs`, `LightningProjectile.cs`.
- Launchers: `Assets/Scripts/Combat/ProjectileLauncher.cs`, `SpellProjectileLauncher`.
- Pool manager: `Assets/Scripts/Combat/ProjectilePoolManager.cs` (named pools, stats, returns, warmup).

## Object Pooling

- Generic pool: `Assets/Scripts/ObjectPooling/ObjectPool.cs` (IPoolable, stats, warmup, clear).
- Effect helper: `Assets/Scripts/ObjectPooling/EffectPool.cs` and `EffectAutoReturn` (spawn/auto‑return of FX).

## Enemies

- AI: `Assets/Scripts/Enemies/SlimeAI.cs`
  - Roam/chase loop with squared‑distance checks; broadcasts death via `EnemyDeathEvent` from health.
- Health: `Assets/Scripts/Enemies/SlimeHealth.cs` implements `IDamageable`, knockback/flash hooks.
- Pathfinding: `Assets/Scripts/Enemies/SlimePathFinding.cs` moves to targets.
- Damage: `Assets/Scripts/Enemies/EnemyDamageSource.cs` for contact damage.

## World

- Infinite world: `Assets/Scripts/World/InfiniteWorldGenerator.cs`
  - Chunk streaming around player; biome‑adjusted content; chunk events emitted; optional LOD.
- World bounds + enemy lifecycle: `Assets/Scripts/World/WorldManager.cs` spawns/despawns near player.
- Biomes: `Assets/Scripts/World/BiomeDataSO.cs` + `BiomeData` struct fallback.
- Background: `Assets/Scripts/World/InfiniteBackground.cs` parallax/background.

## UI

- Health UI: `Assets/Scripts/UI/PlayerHealthBar.cs` listens to player health events.
- HUD: `Assets/Scripts/UI/PlayerUIManager.cs` composes health/mana + spell cooldowns.
- Minimap/Compass: `Assets/Scripts/UI/MinimapController.cs`, `Assets/Scripts/UI/CompassController.cs`.
- Damage numbers: `Assets/Scripts/UI/DamageNumber*.cs`.

## Camera

- `Assets/Scripts/Camera/CameraController.cs` player‑follow/focus logic.

## Debug Utilities

- `Assets/Scripts/Debug/EventDebugConsole.cs` listens and prints key events.
- Many systems include `OnGUI` or gizmos behind `RL_DEBUG_UI`.

## Example Traces

- Player damage
  1) `IDamageable.TakeDamage` on player called by projectile or enemy
  2) `PlayerStats` reduces HP, emits `PlayerDamagedEvent`
  3) UI updates (e.g., `PlayerHealthBar`), `GameManager` accumulates damage stats
  4) If HP <= 0 → `PlayerDeathEvent` → `GameManager` transitions to GameOver

- Spell cast
  1) Input digit key → `SpellCaster` checks cooldown/mana
  2) Instantiates spell prefab or launches projectile → `SpellCastEvent`
  3) Mana deducted → `PlayerManaUsedEvent` → UI updates

- Projectile hit
  1) `Projectile` `OnTriggerEnter2D` → `IDamageable.TakeDamage` on target
  2) Hit effects via `EffectPool`
  3) Return to pool via `ProjectilePoolManager`
