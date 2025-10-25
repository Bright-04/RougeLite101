# RougeLite101 Refactor Plan

Goal: Make the project easier to navigate and extend by fixing correctness issues first, then decoupling, organizing, and trimming unused complexity. Changes are split into small, testable steps.

## Phase 0 – Validation Setup
- [ ] Create a lightweight test scene to spawn a Slime + Player + basic projectile loop.
- [ ] Add a toggle (`RL_DEBUG_UI`) to gate OnGUI debug panels.

## Phase 1 – Critical Fixes (Low-risk)
- [ ] Slime roaming bug: return a proper world position (radius offset) in `SlimeAI.GetRoamingPosition()`.
- [ ] Object pool growth: enforce `TotalCount < maxPoolSize` even when `allowGrowth == true`.
- [ ] Projectile physics: standardize on trigger-based collision and `Rigidbody2D.velocity` for broader compatibility.
- [ ] Fix garbled arrow in `GameManager.GameStateChangedEvent.GetDebugInfo()`.

Deliverables:
- Slime patrols correctly without drifting to origin.
- Pool does not grow unbounded or churn on return.
- Projectiles behave consistently; no duplicate collision handlers.
- Cleaner debug output.

## Phase 2 – Decouple Damage Pipeline
- [ ] Extract `IDamageable` to `Assets/Scripts/Combat/IDamageable.cs`.
- [ ] Implement `IDamageable` on `PlayerStats` and `SlimeHealth`.
- [ ] Update `Projectile.ApplyDamage` to prefer the interface; remove hard-coded component knowledge.

Deliverables:
- Projectile system no longer depends on specific concrete components.
- Clear, reusable contract for anything that can take damage.

## Phase 3 – File Organization & Namespaces
- [ ] Split `Projectile.cs` into: `Projectile.cs`, `FireProjectile.cs`, `IceProjectile.cs`, `LightningProjectile.cs`.
- [ ] Add/normalize namespaces: `RougeLite.Enemies`, `RougeLite.Misc`, `RougeLite.Player`.
- [ ] Move example/demo scripts under `Assets/Scripts/Examples` and exclude from release builds if possible (assembly definition or build filters).

Deliverables:
- One class/interface per file where sensible.
- Consistent namespaces to improve discovery.
- Examples no longer confuse runtime code.

## Phase 4 – Event System Right-Sizing
- [ ] Move unused or demo-only events to `Examples` or a `Docs` sample folder.
- [ ] Replace reflection-based queued dispatch with a direct dispatch approach (e.g., queued struct capturing a `System.Action<EventManager>` to invoke, or a small `IQueuedEvent` interface).
- [ ] Remove `EventBehaviour` auto-create fallback in production; fail fast when `EventManager` is missing.

Deliverables:
- Smaller, faster event queue.
- Clearer separation between example and runtime events.

## Phase 5 – Performance Polish (Optional, data-driven)
- [ ] Pool common hit VFX (simple `ObjectPool<GameObject>` or prewarmed particle systems).
- [ ] Audit Update/Coroutine frequencies; ensure no unnecessary allocations.

Deliverables:
- Fewer spikes on hits; smoother frame time.

## Phase 6 – Documentation & Guardrails
- [ ] Update README with system map and coding conventions.
- [ ] Document damage pipeline and event usage patterns.
- [ ] Add brief CONTRIBUTING notes for adding new enemies/projectiles/events.

---

## Notes on Safe Iteration
- Each bullet should be a small PR or commit with clear before/after behavior and in-Editor validation (play mode smoke test).
- Prefer additive changes first (e.g., add `IDamageable`, implement it) before removing compatibility paths. Remove legacy branches only after scene/prefab bindings are updated and tested.

