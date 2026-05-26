using System;
using UnityEngine;
using RougeLite.Run;

// Pure decision rules only.
// No Unity object orchestration, scene loading, UI toggles, coroutine control, or save/load side effects belongs here.
public static class RunResultRules
{
    public static bool CanShowLose(bool isRunFinished)
    {
        return !isRunFinished;
    }
    public static bool CanShowWin(bool isRunFinished, PlayerStats playerStats)
    {
        return !isRunFinished && playerStats != null && !playerStats.IsDead;
    }

    public static bool ShouldIgnoreBossClear(bool isRunFinished)
    {
        return isRunFinished;
    }

    public static bool ShouldIgnoreBossClearForMissingDungeonManager(bool hasDungeonManager)
    {
        return !hasDungeonManager;
    }

    public static bool ShouldIgnoreBossClearForNonBossFloor(bool isCurrentFloorBossFloor)
    {
        return !isCurrentFloorBossFloor;
    }

    // Determines if the run should continue after a boss clear.
    public static bool ShouldContinueRunAfterBossClear(int currentFloor, int maxFloor)
    {
        return currentFloor < maxFloor;
    }

    public static bool ShouldCompleteRunFromExitPortal(int currentFloor, int maxFloor)
    {
        return currentFloor >= maxFloor;
    }

    // Handles boss clear decision checks and provides a log message for ignored cases.
    // Returns true if processing should stop (i.e., ignore the boss clear), false otherwise.
    // Also outputs whether the log should be a warning.
    public static bool TryGetBossClearBlockReason(bool isRunFinished, DungeonManager dungeonManager, PlayerStats playerStats, out string logMessage, out bool isWarning)
    {
        // Initialize outputs.
        logMessage = null;
        isWarning = false;

        // If run already finished, ignore silently.
        if (isRunFinished)
        {
            return true; // ignore, no log needed.
        }

        // Missing DungeonManager.
        if (dungeonManager == null)
        {
            logMessage = "RunResultController: Boss cleared but DungeonManager was not found.";
            isWarning = true;
            return true;
        }

        // Non‑boss floor.
        if (!dungeonManager.IsCurrentFloorBossFloor)
        {
            logMessage = $"RunResultController: Ignoring BossCleared on non-boss floor {dungeonManager.currentFloor}.";
            isWarning = true;
            return true;
        }

        // Player dead or null.
        if (playerStats == null || playerStats.IsDead || playerStats.currentHP <= 0f)
        {
            logMessage = "RunResultController: Boss cleared event arrived, but player is already dead.";
            // This was originally a Debug.Log (not warning).
            isWarning = false;
            return true;
        }

        // All checks passed; do not ignore.
        return false;
    }

    public static bool CanResolveBossClearToWin(bool isRunFinished, bool hasDungeonManager, bool isCurrentFloorBossFloor, int currentFloor, int maxFloor, PlayerStats playerStats)
    {
        if (isRunFinished || !hasDungeonManager || !isCurrentFloorBossFloor)
        {
            return false;
        }

        if (ShouldContinueRunAfterBossClear(currentFloor, maxFloor))
        {
            return false;
        }

        return playerStats != null && !playerStats.IsDead && playerStats.currentHP > 0f;
    }

    public static string BuildSummaryText(RunResultType resultType, int stars, PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            return resultType == RunResultType.Win ? $"Stars Earned: {stars}" : "Try again from the hub.";
        }

        int currentHp = Mathf.Max(0, Mathf.RoundToInt(playerStats.currentHP));
        int maxHp = Mathf.Max(1, Mathf.RoundToInt(playerStats.GetTotalMaxHP()));

        if (resultType == RunResultType.Win)
        {
            return $"Stars Earned: {stars}\nHP Remaining: {currentHp}/{maxHp}";
        }

        return $"HP Remaining: {currentHp}/{maxHp}";
    }

    public static bool IsResultBlockingGameplay(bool isResultActive)
    {
        return isResultActive;
    }
}
