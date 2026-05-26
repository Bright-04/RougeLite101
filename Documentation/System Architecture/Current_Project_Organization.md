# Current Project Organization

_Branch documentation version: RL20.06-RefactoringSystem_

## Status

Current checkpoint: post-folder reorganization and low-risk namespace pass.
Branch: `RL20.06-RefactoringSystem`.
Validation baseline: compile passes, missing scripts `0`, missing references `0`.
Further structural refactors are out of scope for this checkpoint.

## Refactor Summary

| Area | Current State |
|---|---|
| Script folders | Reorganized by domain and system ownership |
| `Player` folder | Limited to player entity code and direct player state |
| Dungeon rooms folder | `RoomS` normalized to `Rooms` |
| Inventory item assets | `IventoryItems` normalized to `InventoryItems` |
| Namespaces | Added only to low-risk plain C# types |
| Gameplay behavior | No intentional behavior changes |

## Current Folder Ownership

| Folder | Owns | Does Not Own | Examples |
|---|---|---|---|
| `Assets/Scripts/Core` | Global lifecycle, input, camera, scene-flow infrastructure | Feature-specific combat, inventory, dungeon, or UI ownership | `GameManager`, `InputManager`, `ChangeSceneOnCollide`, `CameraFollow` |
| `Assets/Scripts/Player` | Player entity behavior and direct player state | Weapon systems, inventory systems, dungeon systems, general UI | `PlayerMovement`, `PlayerStats`, `Player Controls.cs`, `Player Controls.inputactions` |
| `Assets/Scripts/Combat` | Shared combat behavior used across player/enemy systems | Weapon loadout orchestration, dungeon generation, general menu UI | `SpellCaster`, `FireballSpell`, `LightningSpell`, `WeaponProjectile`, `DamageSource`, `ScarecrowTestDummy` |
| `Assets/Scripts/Weapons` | Weapon runtime, weapon data, alignment, testing, weapon pickups | Armor ownership, general inventory UI, non-weapon dungeon flow | `WeaponController`, `WeaponRig`, `MeleeWeapon`, `ProjectileWeapon`, `WeaponPickup`, `WeaponDefinitionSO` |
| `Assets/Scripts/Equipment` | Armor/equipment loadout logic and rule helpers | Generic combat damage handling, inventory presentation, dungeon generation | `EquipmentManager`, `EquipmentController`, `ArmorLoadoutRules`, `EquipmentDefinitionSO`, `ArmorDefinitionSO` |
| `Assets/Scripts/Inventory` | Inventory data, item data, inventory UI, generic item pickup flow | Weapon-specific equip flow, dungeon orchestration, global lifecycle code | `InventoryController`, `InventoryUI`, `InventorySO`, `ItemSO`, `PickUpSystem`, `Item` |
| `Assets/Scripts/Enemies` | Enemy AI, movement, health, shared enemy helpers | Shared player weapon ownership, run result flow, generic UI logic | `SlimeAI`, `SlimeHealth`, `BatHealth`, `EnemyDeathNotifier`, `SlimeKingHealth`, `BnnyHealth` |
| `Assets/Scripts/Dungeon` | Dungeon generation, room structure, themes, spawn profiles, exit flow, boss encounter hooks | Generic combat helpers, inventory UI, player-only behavior | `DungeonManager`, `RoomTemplate`, `RoomNode`, `ConnectionPoints`, `ExitDoor`, `BossEncounterController`, `ThemeSO`, `RoomSpawnProfileSO` |
| `Assets/Scripts/Run` | Run result flow, rules, session state, run-level data | Global scene flow ownership, generic pause UI, unrelated save systems | `RunResultController`, `RunResultRules`, `RunResultSession`, `RunResultType`, `RunStarRatingCalculator`, `RunStateData` |
| `Assets/Scripts/Director` | Adaptive difficulty telemetry and intensity tracking | General enemy AI, UI presentation, unrelated dungeon generation logic | `AIDirector`, `RuntimeCombatData` |
| `Assets/Scripts/Progression` | Experience progression and progression-related data | Save-file serialization ownership, generic combat logic, scene flow | `ExpManager`, `MetaProgressionData`, `RewardData`, `SinData` |
| `Assets/Scripts/Save` | Save/load runtime flow and save DTOs | Generic inventory ownership, authored progression content, dungeon generation | `SaveSystem`, `AutoSaveManager`, `PlayerStatsData` |
| `Assets/Scripts/UI` | Presentation-facing UI behavior and UI scene helpers | Core gameplay ownership, dungeon generation, persistence orchestration | `EndGameResultUI`, `RunResultSceneController`, `PauseMenu`, `WeaponHUDUI`, `SpellCasterUI` |
| `Assets/Editor` | Editor-only tooling, validators, preview helpers, temporary validation tools | Runtime gameplay logic or scene-owned MonoBehaviours | `RoomTemplateValidator`, `RunResultUIPreviewEditor`, weapon validation tools |
| `Assets/ScriptableObjects` | Authored asset instances and registries | Runtime MonoBehaviour scripts or editor tooling | `WeaponRegistry.asset`, `ArmorRegistry.asset`, `Themes/*`, `SpawnProfiles/*`, `InventoryItems/*` |

Older leftover script folders still exist in the repo; they are legacy locations and not the target ownership model for new work.

## Ownership Rules

- `Player`: player entity behavior and direct player state.
- `Weapons`: weapon runtime, weapon data, alignment, weapon pickups.
- `Equipment`: armor/equipment loadout state and rules.
- `Combat`: shared damage, projectiles, spells, feedback, combat test helpers.
- `Inventory`: inventory data, item data, inventory UI, generic item pickup flow.
- `Dungeon`: generation, room structure, room templates, spawn profiles, themes, exit flow, boss encounter hooks.
- `Run`: run result flow, session state, rules, and run-level helpers.
- `Director`: adaptive difficulty telemetry and intensity tracking.
- `UI`: presentation logic where possible.
- `Core`: lifecycle, input, camera, and scene-flow infrastructure.

## Namespace Status

Namespaced so far:

- `RougeLite.Run`
- `RougeLite.Director`
- `RougeLite.Combat.Damage`

Not namespaced yet:

- Most MonoBehaviours
- Most ScriptableObjects
- Save DTOs
- Serialized enums
- Generated Input System files

## System Flow Overview

### Startup / scene flow

```text
Scene starts
-> GameManager resolves singleton runtime state
-> InputManager creates and switches PlayerControls maps
-> Scene systems locate required managers in Awake/Start
-> Scene flow scripts and UI trigger scene changes
```

### Player combat

```text
Player input
-> EquipmentManager selects active weapon
-> Weapon runtime executes attack
-> DamageSource / WeaponProjectile hits target
-> IDamageable receives damage
-> Enemy health handles damage and death
```

### Inventory / pickup

```text
World pickup touched or interacted with
-> Item + PickUpSystem add generic items to InventorySO
-> WeaponPickup routes weapon/armor pickups into equipment systems
-> InventoryController selects active inventory data
-> InventoryUI refreshes from InventorySO state
```

### Dungeon generation

```text
DungeonManager.GenerateFloor()
-> ThemeSO selected
-> RoomNode layout built
-> RoomTemplate / ConnectionPoints resolved
-> Rooms and corridors spawned
-> ExitDoor and BossEncounterController drive progression
```

### Run result

```text
Boss clear or final exit
-> RunResultController determines run completion
-> RunStarRatingCalculator computes stars
-> RunResultRules builds decision data
-> RunResultSession stores plain result state
-> RunResultSceneController reads session data
-> EndGameResultUI presents result
```

### AI Director

```text
Combat telemetry event
-> AIDirector records damage / kill metrics
-> RuntimeCombatData accumulates recent values
-> Intensity score changes
-> Band shifts between Low / Mid / High
```

## Naming and Change Safety

- Do not rename MonoBehaviours without a migration plan.
- Do not rename ScriptableObjects without a migration plan.
- Do not rename serialized fields without `[FormerlySerializedAs]`.
- Keep file names aligned with class names where possible.
- Place new scripts by owning system, not by the GameObject that uses them.
- Do not place new weapon, equipment, or inventory systems under `Player` unless they are player entity behavior.
- Do not mix architecture cleanup with gameplay changes.
- Validate compile and references after moves.
- Use Unity or `AssetDatabase` for GUID-sensitive moves.
- Isolate risky migrations.

Documentation-only deferred rename examples:

- `WorldItemPickup`
- `InventoryPickupCollector`
- `RunResultFlowController`

## Deferred Work

- Generated Input System files remain in place
- MonoBehaviour namespaces
- ScriptableObject namespaces
- Class/file renames
- Manager/controller responsibility splits
- Save DTO namespace migration
- Serialized enum namespace migration
- asmdefs

## Validation Baseline

- Compile: pass
- Missing scripts: `0`
- Missing references: `0`
- `Dungeon`: validated
- `GameHome`: validated
- Active scene: `Assets/Scenes/Dungeon.unity`

## Refactor Boundary

Architecture refactoring is paused at this baseline. Further structural changes should be planned and reviewed separately before implementation.
