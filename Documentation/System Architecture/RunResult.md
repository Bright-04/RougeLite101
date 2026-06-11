# Run Result System

_Branch documentation version: origin/RL30-RefactorAndNewSaveSystem

## Overview
The Run Result system handles the outcome of a player's dungeon run, determining whether the run ended in a win or loss, calculating a star rating based on performance, and presenting the result to the player before returning them to the game hub.

## Meaning
A **Run Result** represents the final state of a completed run, capturing:
- **Result Type**: `Win` or `Lose`
- **Star Rating**: 1–3 stars (based on remaining HP ratio)
- **Summary Text**: Descriptive message shown in the UI
- **Active State**: Whether the result is currently displayed and blocking gameplay

This system separates pure decision logic from Unity-specific orchestration, making it testable and maintainable.

## Implementation
Located under `Assets/Scripts/Run/Results/`, the system consists of:

### 1. Data Structures
- `RunResultType.cs` – `enum { Win, Lose }`
- `RunResultSession.cs` – Static class holding the current result:
  ```csharp
  public static bool HasResult { get; private set; }
  public static RunResultType ResultType { get; private set; }
  public static int Stars { get; private set; }
  public static string Summary { get; private set; }
  public static void SetResult(RunResultType type, int stars, string summary);
  public static void Clear();
  ```

### 2. Pure Decision Logic
- `RunResultRules.cs` – Contains all rule-based decisions without engine dependencies:
  - `CanShowWin`, `CanShowLose` – Determine if win/lose UI should appear
  - `ShouldIgnoreBossClear`, `ShouldContinueRunAfterBossClear` – Handle boss clear edge cases
  - `BuildSummaryText` – Creates summary string from result type, stars, and player stats
  - `IsResultBlockingGameplay` – Returns whether the result blocks further input

### 3. Star Rating Calculator
- `RunStarRatingCalculator.cs` – Serializable MonoBehaviour (used as a field) that calculates stars based on the ratio of current HP to max HP:
  - 1 star: `oneStarMinHpRatio` (default 0%)
  - 2 stars: `twoStarMinHpRatio` (default 40%)
  - 3 stars: `threeStarMinHpRatio` (default 70%)

### 4. Orchestration Controller
- `RunResultController.cs` – Main MonoBehaviour that:
  - Listens for `BossEncounterController.BossCleared` events
  - Checks exit portal completion (`TryCompleteRunFromExitPortal`)
  - Evaluates win/lose conditions via `RunResultRules`
  - Computes stars via `RunStarRatingCalculator`
  - Builds summary via `RunResultRules`
  - Stores result in `RunResultSession`
  - Pauses gameplay (`Time.timeScale = 0`) and switches input to UI mode
  - Loads the result scene (`RunResultScene`)

### 5. Result Scene & UI
- `RunResultSceneController.cs` – Runs in `RunResultScene`:
  - Validates that `RunResultSession.HasResult` is true
  - Reads `ResultType`, `Stars`, and `Summary`
  - Displays the result via `EndGameResultUI`
  - Provides a "Return to Hub" button that clears the session and reloads the hub
- `EndGameResultUI.cs` – Visual presentation:
  - Shows appropriate win/lose panel
  - Displays star images (filled/unfilled based on star count)
  - Shows summary text
  - Buttons: Restart (Return to Hub), Next (only for wins), Close

## Flow
1. **Trigger** – A run ends via:
   - Boss clear event (from `BossEncounterController`)
   - Exit portal activation (player steps on final floor portal)
   - Implicit lose condition (player death checked in win/lose gates)

2. **Validation** – `RunResultController` checks:
   - `IsRunFinished` – Prevents double processing
   - Boss clear ignores via `RunResultRules.TryGetBossClearBlockReason` (already finished, missing managers, non‑boss floor, dead player)
   - Win/lose eligibility via `CanShowWin` / `CanShowLose`

3. **Calculation** – If valid:
   - `stars = RunStarRatingCalculator.CalculateStars(playerStats)`
   - `summary = RunResultRules.BuildSummaryText(resultType, stars, playerStats)`
   - `resultType` set to `Win` or `Lose`

4. **Storage & Transition** – `RunResultController`:
   - Calls `RunResultSession.SetResult(resultType, stars, summary)`
   - Pauses time, hides pause menu, enables UI input map
   - Loads `RunResultScene`

5. **Display** – In `RunResultScene`:
   - `RunResultSceneController.Start()` confirms session data exists
   - Calls `EndGameResultUI.TryShow(resultType, stars, showNextButton, summary)`
   - UI shows win/lose panel, star rating, summary text, and buttons

6. **Return to Hub** – Any button press:
   - `RunResultSceneController.ReturnToHub()` clears session data
   - Restores `Time.timeScale = 1`, re-enables gameplay input
   - Moves player to hub spawn point
   - Loads hub scene (`GameHome`)

## Architectural Notes
- **Separation of Concerns**: `RunResultRules` contains no UnityEngine dependencies, enabling unit testing.
- **Event‑Driven**: Reacts to game events (boss clear, portal trigger) rather than polling.
- **State Isolation**: Uses a simple static session class to pass data between scenes without tight coupling.
- **Configurable**: Star thresholds and UI references are set via the Inspector, allowing designers to tune difficulty and presentation without code changes.
- **Safe Defaults**: Null checks and fallbacks (e.g., default to 1 star if player stats missing) prevent crashes.

This design keeps the run result system reliable, easy to modify, and isolated from other gameplay systems such as combat, inventory, or dungeon generation.