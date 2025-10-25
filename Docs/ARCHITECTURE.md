# RougeLite101 Architecture Overview

This document explains the project’s high‑level structure, major systems, and how data flows at runtime. It’s intended for new contributors and teammates who want a mental model of how things work end‑to‑end.

## Goals

- Event‑driven, loosely coupled systems via a central event bus
- Clear separation of responsibilities (Managers vs. Gameplay vs. UI)
- Reusable, high‑performance gameplay via object pooling
- Flexible content via ScriptableObjects (spells, biomes)

## Project Layout (Scripts)

- Camera: camera controllers
- Combat: damage, projectiles, launchers, pooling adapter
- Debug: on‑screen debug for events and systems
- Documentation: in‑project docs and guides
- Dungeon/Exploration/MainMenu: feature areas and scene logic
- Editors: Unity editor utilities (if present)
- Enemies: enemy AI, health, damage sources
- Events: event bus, base event types, strongly typed events
- Examples: sample behaviours and usage
- Managers: singletons coordinating game state, input, audio, saves, scenes, UI
- Misc: general utilities (e.g., Flash, Knockback)
- ObjectPooling: generic pooling system + effect pool helper
- Player: player controller, movement, stats, weapons, spells
- UI: runtime UI widgets and managers
- World: infinite world generator, world manager, biomes

## Architectural Patterns

- Event‑Driven Architecture
  - Central bus `Assets/Scripts/Events/EventManager.cs` routes strongly typed events to listeners.
  - Any `MonoBehaviour` can inherit `EventBehaviour` to get convenience helpers to broadcast/subscribe.
  - Most systems communicate via events (e.g., player damage, enemy death, spell cast), avoiding direct coupling.

- Manager Singletons
  - Game‑level orchestration lives in `Assets/Scripts/Managers/*.cs` (e.g., `GameManager`, `InputManager`).
  - Singletons are created on demand and marked `DontDestroyOnLoad`.

- Object Pooling
  - Generic pool: `Assets/Scripts/ObjectPooling/ObjectPool.cs` with `IPoolable` contract.
  - Specialized projectile management: `Assets/Scripts/Combat/ProjectilePoolManager.cs`.
  - Visual effect helper: `EffectPool` auto‑returns spawned effects.

- ScriptableObjects
  - Spells: `Assets/Scripts/Player/SpellSystem/Spell.cs` (name, mana cost, damage, cooldown, prefab).
  - Biomes: `Assets/Scripts/World/BiomeDataSO.cs` (SO‑backed biome configurations; struct fallback is also supported).

- Conditional Debug UI
  - Preprocessor symbol `RL_DEBUG_UI` enables on‑screen metrics in several systems (e.g., pool stats, world info).

## Key Systems

### Event System

- Core types: `GameEvent`, `GameEvent<TData>` define a base for events.
- Bus: `EventManager` manages listeners keyed by event type with O(1) lookup and optional queuing.
- Helpers: `EventBehaviour` caches the bus, provides safe `BroadcastEvent`, `SubscribeToEvent`, and `RegisterForEvent` wrappers.
- Events catalog: `GameEvents.cs` includes player, enemy, combat, run, and world events.

Benefits:
- Loose coupling between gameplay, UI, and managers
- Testability (listeners can be swapped in/out)
- Fewer direct references across systems

### Managers

- `GameManager`: global game state machine (MainMenu/Gameplay/Paused/Victory/GameOver). Listens to player/enemy events and advances state.
- `InputManager`: centralizes input across the new Input System and legacy input; supports keybinds and buffering.
- `AudioManager`, `SaveManager`, `SceneTransitionManager`, `UIManager`: specialized orchestration per domain.

### Player

- Stats: `PlayerStats` tracks HP/Mana/Stamina, regen, damage intake, and broadcasts health/mana events.
- Controller: `PlayerController` owns facing direction and combat‑oriented sprite flipping (movement delegated).
- Movement: `SimplePlayerMovement` reads input (new Input System first, graceful fallback) and moves via Rigidbody2D or transform. Broadcasts `PlayerMovementEvent`.
- Spells: `SpellCaster` consumes `Spell` assets, checks mana and cooldowns, spawns spell prefabs, and emits `SpellCastEvent`.

### Combat

- Damage Contract: `IDamageable` implemented by things that can be hit.
- Projectiles: `Projectile` base handles init, flight, trigger collisions, damage application, effects, and returning to pool. Specialized types (Fire/Ice/Lightning) extend behaviour.
- Launchers: `ProjectileLauncher` and `SpellProjectileLauncher` fire from pools and emit projectile/spell events.
- Pooling: `ProjectilePoolManager` hosts multiple named pools for different projectile types.

### UI

- Event‑driven health/mana UI (`PlayerHealthBar`, `PlayerUIManager`) subscribe to player events rather than polling player state. `PlayerUIManager` also shows spell cooldowns.

### World

- Infinite generation: `InfiniteWorldGenerator` creates chunks around the player with biome‑adjusted content (terrain/enemies/items/structures/decorations). Emits chunk events.
- World bounds and enemy lifecycle: `WorldManager` constrains positions, spawns/despawns enemies near the player, and broadcasts spawn/despawn events.

## Runtime Data Flow (High Level)

1) Boot
- Managers instantiate as singletons on first access.
- If needed, `EventBehaviour` will auto‑create an `EventManager` (in Debug builds).

2) Gameplay Loop
- Input read (new Input System preferred) → movement via `SimplePlayerMovement` → optional `PlayerMovementEvent`.
- Combat input (spell keys) → `SpellCaster` validates mana/cooldowns → instantiates prefab/projectile → events emitted.
- Projectiles trigger `OnTriggerEnter2D`, call `IDamageable.TakeDamage` on targets, then return to pool.
- Player/enemy damage updates emit typed events; UI/Managers react.

3) World Streaming
- Player crosses chunk boundary → `InfiniteWorldGenerator` generates/unloads chunks and updates LOD. Chunk events emitted.

4) State Transitions
- `GameManager` listens for `PlayerDeathEvent`, `EnemyDeathEvent` and advances to GameOver/Victory or respawns player per settings.

## Extensibility Guidelines

- New UI reaction: Subscribe to the relevant event type in a `MonoBehaviour` that inherits `EventBehaviour`.
- New projectile: Derive from `Projectile`, register a pool in `ProjectilePoolManager`, and use `ProjectileLauncher`.
- New enemy: Implement `IDamageable`, broadcast `EnemyDeathEvent` when defeated, and add a spawn entry to `WorldManager` or world gen.
- New spell: Create a `Spell` asset, hook it into a `SpellCaster` slot, and optionally use a projectile prefab.
- New biome/content: Create a `BiomeDataSO` and assign prefabs and rates; reference in `InfiniteWorldGenerator`.

## Performance Notes

- Prefer trigger colliders for projectiles; avoids double collision processing.
- Use pooling for all repeating objects (projectiles, effects).
- Use squared distance where possible in AI/LOD calculations.
- Gate debug GUIs behind `RL_DEBUG_UI`.

---

See also:
- Docs/EVENT_SYSTEM_GUIDE.md and Docs/EVENT_SYSTEM_QUICK_REFERENCE.md
- Docs/MOVEMENT_SYSTEM_FIX.md and Docs/MOUSE_BASED_COMBAT_GUIDE.md
- Docs/DIAGRAMS.md (visuals for core systems)
