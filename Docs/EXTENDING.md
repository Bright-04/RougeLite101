# RougeLite101 Extending the Project

This guide shows how to add new gameplay features in a way that fits the project’s patterns.

## Add a New Spell

1) Create a `Spell` asset
- Right‑click in the Project window → Create → Spells → Spell.
- Fill in name, mana cost, damage, cooldown, and an optional projectile prefab.

2) Hook into the player
- Assign the `Spell` asset to an open slot in `SpellCaster.spellSlots` on the player.
- If the spell uses a projectile, ensure its prefab has the expected behaviour (e.g., `FireballSpell` or a `Projectile`‑based script).

3) UI
- `PlayerUIManager` automatically reads slot names and cooldowns if slots are set.

## Add a New Projectile Type

1) Derive from `Projectile`
- Create `MyProjectile.cs` that extends `Projectile` and override `HandleHit` for custom logic.

2) Pool configuration
- In `ProjectilePoolManager`, either:
  - Add a new entry to the serialized `poolConfigs` in the inspector, or
  - Call `RegisterPool("MyProjectile", prefab, initial, max)` at runtime.

3) Launching
- Use `ProjectileLauncher` (on player/enemy) to `Fire` or `FireAt` with the correct pool name.

## Add a New Enemy

1) Health and damage
- Implement `IDamageable` (e.g., copy `SlimeHealth`) and emit `EnemyDeathEvent` when killed.

2) AI
- Add an AI script (e.g., `MoveTo`/roam/chase) and gate expensive checks using squared distances.

3) Spawning
- Add the prefab to `WorldManager.enemyPrefabs` or the generator’s biome lists.

## React to Events in UI or Systems

1) Inherit from `EventBehaviour`
- Subscribe: `SubscribeToEvent<PlayerDamagedEvent>(this)` or `RegisterForEvent<PlayerDamagedEvent>(OnDamaged)`.

2) Implement handler
- Interface pattern: `void OnEventReceived(PlayerDamagedEvent e)`
- Action pattern: private void `OnDamaged(PlayerDamagedEvent e)`

3) Unsubscribe
- In `OnDestroy`, call the corresponding Unsubscribe/Unregister helper.

## Add a New Biome

1) Create `BiomeDataSO`
- Fill terrain/enemy/item/structure/decor prefabs and spawn rates.

2) Assign to generator
- Add to `InfiniteWorldGenerator.biomeDataSOs` and enable `useBiomes`.

## Tips

- Prefer events over direct references between systems.
- Keep managers thin; push gameplay into feature scripts.
- Use pooling for anything frequently spawned (VFX, projectiles, debris).
- Hide runtime debug UI behind `RL_DEBUG_UI`.

