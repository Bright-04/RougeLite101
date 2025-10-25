# RougeLite101 Codebase Structure

This document outlines the repository layout with emphasis on `Assets/Scripts/` and notable files.

## Top Level

- `Assets/` – Unity project assets
  - `Scripts/` – All runtime/editor code, organized by feature area
  - Other art/prefab/scenes folders omitted here
- `Packages/`, `ProjectSettings/`, `UserSettings/` – Unity project config
- `Docs/` – Developer documentation (this folder)

## Scripts Breakdown

- `Assets/Scripts/Camera/`
  - `CameraController.cs` – Camera follow/focus logic

- `Assets/Scripts/Combat/`
  - `IDamageable.cs` – Damage contract interface
  - `Projectile.cs` – Base projectile (movement, collisions, damage, pooling lifecycle)
  - `FireProjectile.cs`, `IceProjectile.cs`, `LightningProjectile.cs` – Specialized behaviours
  - `ProjectileLauncher.cs` – Fire/Spread/Burst; `SpellProjectileLauncher` adds mana costs/events
  - `ProjectilePoolManager.cs` – Named projectile pools, warmup, stats

- `Assets/Scripts/Debug/`
  - `EventDebugConsole.cs` – Logs received events (useful during development)

- `Assets/Scripts/Documentation/` – In-project guides and references

- `Assets/Scripts/Dungeon/`, `Assets/Scripts/Exploration/`, `Assets/Scripts/MainMenu/`
  - Scene/feature-specific logic (varies by project growth)

- `Assets/Scripts/Editor/`
  - Editor utilities (if any) – custom inspectors, tooling

- `Assets/Scripts/Enemies/`
  - `SlimeAI.cs` – Roam/Chase finite state logic
  - `SlimeHealth.cs` – Implements `IDamageable` (int HP), knockback/flash hooks, broadcasts `EnemyDeathEvent`
  - `SlimePathFinding.cs` – Movement utility
  - `EnemyDamageSource.cs` – Contact damage behaviour

- `Assets/Scripts/Events/`
  - `EventManager.cs` – Typed event bus with optional queue and logging
  - `EventBehaviour.cs` – Helper base for broadcast/subscribe/register
  - `GameEvent.cs` – `GameEvent` / `GameEvent<T>` base event types
  - `GameEvents.cs` – Catalog of concrete events and data structs:
    - Player: `PlayerDamagedEvent`, `PlayerDeathEvent`, `PlayerManaUsedEvent`, `PlayerMovementEvent`...
    - Enemy: `EnemySpawnedEvent`, `EnemyDeathEvent`...
    - World: `ChunkGeneratedEvent`, `ChunkUnloadedEvent`, `ChunkChangedEvent`
    - Game state: `GamePausedEvent`, `GameResumedEvent`, `GameOverEvent`...
  - Documentation: `EVENT_SYSTEM_GUIDE.md`, `EVENT_SYSTEM_QUICK_REFERENCE.md`

- `Assets/Scripts/Examples/`
  - Samples of usage patterns

- `Assets/Scripts/Managers/`
  - `GameManager.cs` – Singleton state machine; pause/game over/victory; listens for player/enemy events
  - `InputManager.cs` – New+Legacy input, buffering, key bindings, settings persistence
  - `AudioManager.cs`, `SaveManager.cs`, `SceneTransitionManager.cs`, `UIManager.cs` – Domain-specific managers
  - `ManagersDocumentation.md` – Notes for manager systems

- `Assets/Scripts/Misc/`
  - Utilities (e.g., `Knockback`, `Flash`) referenced by enemies or player

- `Assets/Scripts/ObjectPooling/`
  - `ObjectPool.cs` – Generic pool for `IPoolable` `Component`s; stats, warmup, clear
  - `EffectPool.cs` – Convenience for spawning VFX that auto-return
  - `ObjectPoolingDocumentation.md` – Pooling guide

- `Assets/Scripts/Player/`
  - `PlayerStats.cs` – HP/Mana/Stamina, regen, damage intake; emits player events
  - `PlayerController.cs` – Mouse-based facing; movement is intentionally delegated
  - `SimplePlayerMovement.cs` – Input-driven movement (Rigidbody2D/transform), broadcasts movement events
  - `Sword.cs`, `SlashAnim.cs` – Melee components
  - `SpellSystem/` – Spell content
    - `Spell.cs` – ScriptableObject (name, mana, damage, cooldown, prefab, range)
    - `SpellCaster.cs` – Input→validation→instantiate→events; exposes cooldown timers for UI
    - Sample assets and spell scripts (e.g., `FireballSpell.cs`, `LightningSpell.cs`)

- `Assets/Scripts/UI/`
  - `PlayerHealthBar.cs` – Listens to player health events, updates slider/text/colors
  - `PlayerUIManager.cs` – Composes HUD (health, mana, spell cooldown overlays)
  - `MinimapController.cs`, `CompassController.cs`, `DamageNumber*.cs` – Additional UI widgets

- `Assets/Scripts/World/`
  - `InfiniteWorldGenerator.cs` – Chunk streaming around player; biomes; LOD; chunk events
  - `WorldManager.cs` – Bounds; spawn/despawn near player; emits spawn/despawn events
  - `BiomeDataSO.cs` – ScriptableObject per-biome configuration (terrain/enemy/item/decor)
  - `InfiniteBackground.cs` – Background/parallax

## Conventions & Notes

- Prefer event-driven communication over direct component references
- All repeating objects (projectiles, effects) should use pooling
- New gameplay features should ideally:
  - Emit/subscribe to typed events in `GameEvents.cs`
  - Avoid tight coupling with `GameManager`/other systems
  - Be isolated in their respective feature folder

