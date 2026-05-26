# Manager / Controller Responsibility Map

_Branch documentation version: RL20.06-RefactoringSystem_

This map documents the current responsibilities of the main manager/controller classes so future cleanup can separate ownership from orchestration without breaking serialization.

| Class | Current role | Classification | Why renaming is unsafe right now | Suggested future name |
|---|---|---|---|---|
| `GameManager` | Global runtime singleton and scene cleanup coordinator | State owner, orchestrator | Serialized scene references and singleton usage already depend on the current class name | `GameSessionManager` |
| `InputManager` | Owns input action maps and switches UI/gameplay control state | Input adapter, state owner | Scene/prefab wiring depends on the existing singleton and class name | `InputMapManager` |
| `ExpManager` | Handles experience gain, leveling, and stat growth | State owner, progression orchestrator | Existing code may reference the singleton and current type directly | `ExperienceProgressionManager` |
| `DungeonManager` | Builds floors, spawns rooms, and advances dungeon progression | Orchestrator, factory/spawner, state owner | Scene configuration and generation flow already reference this type | `DungeonFlowOrchestrator` |
| `BossEncounterController` | Relays boss death state and clears the encounter | Encounter state owner, event relay | Scene bindings and run-clear flow already depend on the current class name | `BossEncounterStateRelay` |
| `RunResultController` | Drives win/lose UI and return-to-hub flow | UI controller, state owner | Serialized UI hookups in scenes and prefabs already target this type | `RunResultUIController` |
| `PauseMenu` | Controls pause overlay and quit behavior | UI controller, input adapter | Prefab and scene event wiring depend on the current class name | `PauseOverlayController` |
| `AutoSaveManager` | Runs periodic save checks and captures live player state | Persistence service, orchestrator | Save flow references and serialized scene/prefab wiring would be fragile to rename now | `AutoSaveCoordinator` |
| `SaveSystem` | Static save/load facade for player state files | Persistence service | Save/load API is a stable integration point and should not be churned lightly | `PlayerSaveService` |
| `WeaponController` | Builds the runtime weapon visual pose and rig state | Runtime component | Prefab composition and runtime weapon setup already rely on this type | `WeaponPoseController` |
| `EquipmentManager` | Owns weapon slots, pickup flow, and active weapon instantiation | State owner, orchestrator, factory/spawner | Serialized scene/prefab references and input flow already target this component | `WeaponLoadoutManager` |
| `EquipmentController` | Owns armor slot state and load/replay of equipped armor | State owner, persistence bridge | Serialized scene/prefab references and save replay logic already target this component | `ArmorLoadoutController` |
| `InventoryController` | Bridges inventory UI, scene switching, and item actions | UI controller, input adapter | UI and scene event wiring already depend on the current class name | `InventoryUIController` |

## Notes

- `GameManager`, `InputManager`, `ExpManager`, `DungeonManager`, `EquipmentManager`, and `EquipmentController` are especially risky to rename because they are currently part of serialized scene and prefab wiring.
- `SaveSystem` is not a scene component, but changing it now would still create unnecessary persistence churn.
- The suggested future names are documentation only. They are not a recommendation to rename code in this phase.
- `EquipmentManager` remains the serialized MonoBehaviour entry point for weapon loadout orchestration. `WeaponLoadoutRules` owns only pure weapon slot, stable-ID, and fallback decision logic.
- `EquipmentController` remains the serialized MonoBehaviour entry point for armor loadout orchestration. `ArmorLoadoutRules` owns only pure armor slot, stable-ID, and reference-check decision logic.
- The helper rule classes must remain pure. They should not instantiate or destroy Unity objects, access scene objects, trigger UI, call `SaveSystem`, or mutate ScriptableObject assets.

## Rule Helper Boundary

- `WeaponLoadoutRules` is decision-only code for pickup acceptance, slot choice, fallback slot selection, and stable weapon identity comparisons.
- `ArmorLoadoutRules` is decision-only code for armor acceptance, slot mapping, stable armor identity comparisons, and slot lookups.
- Any behavior that touches transforms, prefabs, input, UI, save/load, or event dispatch stays in the MonoBehaviour entry points.
