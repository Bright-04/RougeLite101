# Item / Equipment Identity Map

_Branch documentation version: RL20.06-RefactoringSystem_

This project currently uses two different identity layers. That is intentional for now, but it must stay explicit so runtime inventory matching is not confused with save/load identity.

## Current Identity Model

- `ItemSO.ID` is runtime-only because it is based on `GetInstanceID()`.
- `InventorySO` currently uses `ItemSO.ID` for runtime stack matching and slot merging.
- `EquipmentDefinitionSO` is the shared authored equipment base and carries the stable authored identity fields.
- `WeaponDefinitionSO` exposes its stable authored identity through `weaponId` / `WeaponId`, which currently resolves to `EquipmentId`.
- `ArmorDefinitionSO` uses the stable authored equipment identity through `equipmentId` / `EquipmentId`.
- `SaveSystem` and `PlayerStatsData` should persist only stable string IDs for loadout data.

`ArchitectureIdentityValidator` now enforces the stable authored ID policy at the asset level by checking weapon and armor definitions, registry entries, and duplicate or empty IDs.

Weapon and armor registry entries are validated for non-empty IDs and duplicate IDs. That validation is descriptive only; it does not auto-fix or rewrite assets.

## Rules To Preserve

- Do not use `ItemSO.ID` for save/load data.
- Do not rename serialized fields yet.
- Do not merge `ItemSO`, `EquipmentDefinitionSO`, `WeaponDefinitionSO`, or `ArmorDefinitionSO` as part of this phase.
- Keep runtime inventory matching separate from authored identity until a migration plan exists.

## Stable Authored Identity

The stable authored identity should remain string-based.

- Weapons: `WeaponDefinitionSO` should continue to use `WeaponId` as the public authored key, backed by the shared equipment identity field.
- Armor: `ArmorDefinitionSO` should continue to use `EquipmentId` as the authored key unless a later migration introduces a dedicated armor alias.
- Shared authored equipment data belongs in `EquipmentDefinitionSO`.

## Runtime Matching vs Persistence

`InventorySO` still performs runtime matching using item instance identity. That is acceptable for the current implementation, but it should not be treated as the canonical persisted identity model.

Existing reference-based comparisons are intentionally preserved in equip behavior where the current runtime logic depends on them. ID-based equivalence should not be introduced into equip flows unless it is explicitly designed, tested, and migration-safe.

`SaveSystem` and `PlayerStatsData` already follow the safer pattern: they persist stable string IDs for weapons and armor loadout state. That is the pattern future code should keep.

## Future Target

The long-term target is one canonical stable ID policy for authored items and equipment, with a separate explicit runtime identity for transient inventory behavior. The migration should be incremental and serialization-safe.

## Migration Note

If future cleanup needs a canonical ID helper, add a new read-only adapter or validation path instead of changing the existing serialized field names in-place.
