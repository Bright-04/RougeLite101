# Phase 1.5 Folder Layout Note

## Purpose

This note records the post-Phase-1 domain-oriented script layout and the items that were intentionally deferred during stabilization.

## Runtime Layout

`Assets/Scripts` is now organized primarily by domain:

- `Core`
- `Player`
- `Combat`
- `Weapons`
- `Equipment`
- `Inventory`
- `Enemies`
- `Dungeon`
- `Run`
- `Director`
- `Progression`
- `Save`
- `UI`

This layout was introduced with Unity-safe asset moves only. No class renames, namespace changes, serialized field changes, ScriptableObject type changes, or asmdefs were part of Phase 1.

## Deferred Items

The following items remain intentionally deferred and should not be moved casually:

- `Assets/Scripts/Player/Player Controls.cs`
- `Assets/Scripts/Player/Player Controls.inputactions`
- `Assets/Editor/ValidateRestartMenu.cs`
- `Assets/Editor/Validation/ArchitectureIdentityValidator.cs`
- `Assets/Editor/SceneFixers`
- `Assets/Editor/TempValidation`
- `Assets/Editor/RunRestartValidator.ps1`

`Player Controls.cs` and its `.inputactions` asset stay in place until there is a dedicated Input System migration plan, because the generated wrapper is path-sensitive and easy to break with an uncoordinated move.

## Stabilization Follow-Up

`Assets/Scripts/Dungeon/RoomS` is currently treated as a stabilization follow-up item rather than an empty-folder cleanup target. Git still tracks room scripts beneath that path, so it needs a dedicated casing or naming migration plan instead of deletion during Phase 1.5.

## Phase 2 Direction

Safe future candidates include:

- gradual namespace adoption by domain
- planned class renames with Unity serialization migration
- clearer controller and manager responsibility boundaries
- a separate asset-only migration for `IventoryItems` naming cleanup
- deferred editor utility organization once the runtime layout is stable
