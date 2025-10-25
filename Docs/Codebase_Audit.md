# RougeLite101 Codebase Audit

This audit summarizes key issues found in the current Unity project and provides prioritized recommendations. It focuses on correctness, maintainability, performance, and consistency.

## Project Snapshot
- Unity project with custom event system, object pooling, projectile framework, and simple enemy AI (Slime).
- Notable systems: `Events`, `ObjectPooling`, `Combat (Projectile)`, `Managers`, `Enemies`.
- Example/demo scripts coexist with runtime code under `Assets/Scripts`.

## Key Findings

### 1) Tight Coupling in Damage Flow
- File: `Assets/Scripts/Combat/Projectile.cs`
  - `ApplyDamage` checks `PlayerStats` and `SlimeHealth` explicitly and only optionally uses `IDamageable` (which is defined inside `Projectile.cs`).
  - Effect: Adds hard dependencies to specific components and makes reuse of the projectile system harder.
- Recommendation: Move `IDamageable` to its own file and ensure targets implement it (`PlayerStats`, `SlimeHealth`). Update `Projectile` to rely solely on `IDamageable` where possible.

### 2) Bug: Roaming AI picks a direction, not a position
- File: `Assets/Scripts/Enemies/SlimeAI.cs`
  - `GetRoamingPosition()` returns a unit direction vector, but `SlimePathFinding.MoveTo(Vector2)` expects a world position. This can cause enemies to head towards coordinates near the origin.
- Recommendation: Return a position offset from current location using a roam radius and randomized target point.

### 3) Pool growth logic disregards max size when allowed to grow
- File: `Assets/Scripts/ObjectPooling/ObjectPool.cs`
  - In `Get()`: `if (allowGrowth || TotalCount < maxPoolSize)` means when `allowGrowth == true`, the pool grows indefinitely and ignores `maxPoolSize`.
  - Later, `Return()` trims by destroying items when `availableObjects.Count >= maxPoolSize`, causing churn.
- Recommendation: Enforce `TotalCount < maxPoolSize` regardless; use `allowGrowth` to gate growth only up to `maxPoolSize`.

### 4) Multiple classes and interfaces in a single file (organization)
- File: `Assets/Scripts/Combat/Projectile.cs`
  - Contains `Projectile`, `IDamageable`, and three derived classes (`FireProjectile`, `IceProjectile`, `LightningProjectile`).
- Recommendation: Split into separate files (1: `IDamageable`, 2: `Projectile`, 3+: specialized projectiles) for clarity and compile-time hygiene.

### 5) Namespace inconsistency
- Many scripts (e.g., `SlimeAI`, `SlimeHealth`, `SlimePathFinding`, `Flash`, `Knockback`) are in the global namespace, while others use `RougeLite.*` namespaces.
- Recommendation: Standardize namespaces (e.g., `RougeLite.Enemies`, `RougeLite.Misc`, `RougeLite.Player`) to improve discoverability and avoid collisions.

### 6) Event system is oversized for current use
- Files: `Assets/Scripts/Events/GameEvents.cs`, `EventManager.cs`, `EventBehaviour.cs`.
  - Very large set of events; many appear used only by `Examples/`.
  - `EventManager.ProcessQueuedEvents()` uses reflection to broadcast queued events, adding overhead and GC pressure.
  - `EventBehaviour` auto-creates an `EventManager` when missing, which can lead to hidden runtime objects.
- Recommendations:
  - Separate example/demo events into an `Examples` assembly or folder excluded from builds.
  - Avoid reflection in the critical path: queued events can store an `Action<EventManager>` or a simple interface with a `Dispatch(EventManager mgr)` method.
  - Prefer explicit `EventManager` placement over auto-creation to fail fast in misconfigurations.

### 7) Unused or redundant code
- `PoolManager` (generic pool registry) is not referenced outside `ObjectPool.cs` and documentation.
  - Files: `Assets/Scripts/ObjectPooling/ObjectPool.cs` (class `PoolManager`).
  - Recommendation: Remove or move to `Examples/` if not used.
- `ProjectilePoolManager.OnProjectileDestroyed` callback is registered but empty; consider removing the subscription or emitting only when listeners exist.

### 8) Physics/collision robustness
- `Projectile.cs` uses both `OnTriggerEnter2D` and `OnCollisionEnter2D`. Behavior depends on collider setup; having both can be confusing.
- `Rigidbody2D.linearVelocity` is used; `velocity` is the common property and more widely supported across Unity versions. Consider using `velocity` for compatibility.
- Recommendation: Pick one collision mode (trigger is typical for projectiles), ensure colliders are configured, and use consistent Rigidbody2D APIs.

### 9) VFX instantiation per hit
- File: `Assets/Scripts/Combat/Projectile.cs`
  - `SpawnHitEffect` instantiates/destroys objects on every hit.
- Recommendation: Pool common VFX or use `ParticleSystem.Play()` with preplaced pool to avoid allocation spikes.

### 10) Minor quality issues
- File: `Assets/Scripts/Managers/GameManager.cs`
  - String arrow in `GameStateChangedEvent.GetDebugInfo()` shows a garbled character. Replace with `->`.
- Many runtime scripts include `OnGUI` debugging; consider guarding with defines or a global debug toggle to avoid frame-cost in production builds.

## Risk and Impact
- Fixes above are primarily low-to-medium risk. The largest breaking changes are:
  - Switching strictly to `IDamageable` requires implementing it on damage receivers and adjusting prefabs.
  - Namespace changes require scene/prefab rebinds if types move; mitigated with gradual, opt-in migration.

## Prioritized Recommendations
1) Correctness first
   - Fix Slime AI roaming target.
   - Correct pool growth logic.
   - Normalize projectile physics API and collision mode.
   - Replace garbled arrow in `GameManager` debug string.

2) Decouple damage pipeline
   - Extract `IDamageable` and implement on `PlayerStats` and `SlimeHealth`.
   - Simplify `Projectile.ApplyDamage` to prefer interface-based damage.

3) Organization and hygiene
   - Split large files; standardize namespaces.
   - Move unused systems (`PoolManager`, example events) to `Examples/` or exclude from builds.

4) Performance polish
   - Pool hit VFX if profiling shows spikes.
   - Replace event reflection in queue with direct dispatch.

## Suggested Next Steps
- Implement Phase 1 fixes behind small PRs.
- Add minimal play-mode test scene to sanity-check projectile hits and AI roaming.
- Gate debug OnGUI under a define (e.g., `#if RL_DEBUG_UI`).

