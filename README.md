# RougeLite101

RougeLite101 is a Unity 2D top-down roguelite prototype developed for the graduation thesis project:

**Top-Down Roguelite Game Featuring AI-Driven Adaptive Difficulty Scaling**

The project includes a playable combat loop, procedural dungeon flow, UI and inventory foundations, save-related systems, and early adaptive-difficulty telemetry. The full AI Director feedback loop is still incomplete, so the project should be understood as a playable research prototype rather than a finished commercial game.

## Latest Release

The latest thesis prototype release is:

[RL28 – Latest Capstone Prototype Build](https://github.com/Bright-04/RougeLite101/releases/latest)

Recommended GitHub release tag:

`RL28`

This release represents the latest stabilized prototype build prepared for thesis demonstration, documentation, and evaluation.

## Thesis Information

- **Thesis Title:** Top-Down Roguelite Game Featuring AI-Driven Adaptive Difficulty Scaling
- **Project Name:** RougeLite101
- **Project Type:** Graduation thesis / capstone prototype
- **Development Platform:** Unity 2D
- **Main Research Direction:** Adaptive difficulty in roguelite gameplay using runtime player-performance telemetry
- **Current Academic Status:** Playable prototype
- **Instructor:** Prof. Hoàng Văn Dũng

The thesis focuses on designing and implementing a top-down roguelite prototype with procedural dungeon generation, combat encounters, player progression, and a foundation for dynamic difficulty adjustment. The implemented Director system currently collects and evaluates gameplay-related metrics, but the complete feedback loop where enemy behavior dynamically reacts to those metrics remains a future improvement.

## Project Status

RougeLite101 is currently a playable prototype rather than a finished game.

The current priority is stabilizing the systems that already exist, documenting the implemented architecture, and preparing the project for thesis submission and demonstration. The long-term adaptive-difficulty goal remains part of the design direction, but the full AI Director-driven enemy behavior loop is not complete yet.

## Features

- Player movement and dash
- Combat with melee and projectile weapon systems
- Spell casting with multiple spell slots
- Enemy and boss encounters
- Procedural dungeon generation with themed room sets
- Inventory and item pickup systems
- Equipment, armor, and weapon loadout systems
- Save and auto-save related systems
- Run result and progression-related systems
- Combat metrics and intensity tracking through the Director system
- Early adaptive-difficulty telemetry foundation

## Architecture Overview

The project is organized around domain ownership rather than scene-specific or object-specific folders. The current documented ownership model is:

- `Core`: lifecycle, input, camera, and scene-flow infrastructure
- `Player`: player entity behavior and direct player state
- `Combat`: shared damage, projectiles, spells, feedback, and combat helpers
- `Weapons`: weapon runtime, weapon data, alignment, and weapon pickups
- `Equipment`: armor and equipment loadout logic
- `Inventory`: inventory data, item data, inventory UI, and generic pickups
- `Enemies`: enemy AI, movement, health, and shared enemy helpers
- `Dungeon`: procedural generation, room structure, themes, spawn profiles, exit flow, and boss encounter hooks
- `Run`: run result flow, rules, and session state
- `Director`: combat telemetry and intensity tracking
- `Progression`: experience progression and progression-related data
- `Save`: save/load runtime flow and save data transfer objects
- `UI`: presentation-facing UI behavior

Legacy folders and older structure remnants still exist in the repository, but the domain-based layout above is the current source of truth for new work.

## System Breakdown

- `Core` manages bootstrapping, input setup, camera follow, and scene transitions.
- `Player` contains movement, dash behavior, and player stats.
- `Combat`, `Weapons`, and `Equipment` work together to drive attacking, spell usage, weapon switching, damage, and loadouts.
- `Inventory` handles inventory data, inventory UI, and generic pickup flow.
- `Dungeon` owns room templates, theme selection, spawn profiles, layout generation, and exit/boss progression hooks.
- `Enemies` contains common enemy health/damage helpers plus slime, bat, and boss behaviors.
- `Run` and `UI/RunResults` handle end-of-run state, star rating, result presentation, and run flow.
- `Director` currently tracks combat runtime data and intensity bands, but it should be treated as an incomplete adaptive-difficulty foundation rather than a finished game director.
- `Save` and `Progression` support persistence and stat/experience growth systems already present in the prototype.

## Scene Overview

The current build settings include these main scenes:

- `MainMenu`: entry menu scene
- `GameHome`: hub/home scene used by current scene flow
- `Dungeon`: active gameplay scene for procedural dungeon runs
- `RunResultScene`: result scene used by run completion flow

Non-core scenes also exist in the repository, such as `WeaponAlignmentTest` and `Scene(b4maps)`, but they should not be treated as the main player flow.

## Requirements

- Unity `6000.3.13f1`
- Universal Render Pipeline, or URP
- Unity Input System package

## How to Run

1. Open the project in Unity `6000.3.13f1`.
2. Open `Assets/Scenes/MainMenu.unity` or `Assets/Scenes/GameHome.unity`.
3. Press Play in the Unity Editor.

## Controls

Verified keyboard and mouse controls from the current input actions asset:

- `W`, `A`, `S`, `D`: move
- `Space`: dash
- `Left Mouse Button`: attack
- `1`, `2`, `3`: cast spells
- `E`: interact
- `Q`: swap weapon
- `Esc`: open pause menu
- `Tab`: open or close stats
- `I`: open or close inventory

Some gamepad bindings also exist in the input actions asset, but they are not fully documented here.

## Documentation

- [Documentation Overview](./Documentation/README.md)
- [Current Project Organization](./Documentation/System%20Architecture/Current_Project_Organization.md)
- [System Overview](./Documentation/System%20Architecture/System_Overview.md)
- [Manual Tilemap Room Guide](./Documentation/Level%20Design/Manual_Tilemap_Room_Guide.md)

## Known Issues / Work In Progress

- The AI Director currently provides telemetry and intensity tracking, but the full adaptive feedback loop is still incomplete.
- Enemy behavior does not yet fully react to collected player-performance metrics.
- Architecture cleanup and responsibility refactoring are ongoing.
- Some legacy folders, scripts, and older documentation references still exist alongside the current ownership model.
- System stabilization is still in progress across dungeon, inventory, save, and run-result flows.
- Some UI, balancing, formatting, and content polish remain prototype-level.

## Roadmap

- Complete the adaptive difficulty feedback loop
- Connect Director scoring to runtime enemy, spawn, or encounter adjustment
- Continue architecture cleanup and responsibility alignment
- Reduce redundant systems and legacy folder overlap
- Stabilize save, inventory, and run-result integration
- Expand content and continue combat tuning
- Improve thesis-oriented documentation and diagrams

## Academic Note

This repository is maintained primarily as part of a graduation thesis prototype. The goal is to demonstrate system design, implementation, procedural generation, combat gameplay, telemetry collection, and the foundation of adaptive difficulty in a Unity roguelite game.

The project is not intended to represent a fully polished commercial release.

## License

This project is licensed under the [MIT License](./LICENSE).
