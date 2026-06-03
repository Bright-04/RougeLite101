using UnityEditor;
using UnityEngine;
using System.Reflection;

public static class WinFlowValidator {
    [MenuItem("Tools/Validate Win Flow")]
    public static void Execute() {
        // Ensure Dungeon scene is loaded
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Dungeon.unity");
        var dungeon = Object.FindFirstObjectByType<DungeonManager>();
        if (dungeon == null) { Debug.LogError("DungeonManager not found"); return; }
        var encounter = Object.FindFirstObjectByType<BossEncounterController>();
        if (encounter == null) { Debug.LogError("BossEncounterController not found"); return; }
        var startMethod = typeof(DungeonManager).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
        startMethod?.Invoke(dungeon, null);
        // Force boss floor
        dungeon.currentFloor = dungeon.bossEveryXFloor; // assume bossEveryXFloor == 5
        dungeon.GenerateFloor();
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        var playerStats = playerObj?.GetComponent<PlayerStats>();
        if (playerStats == null) { Debug.LogError("PlayerStats not found"); return; }
        var bossNotifierField = typeof(BossEncounterController).GetField("registeredBossNotifier", BindingFlags.NonPublic | BindingFlags.Instance);
        var bossNotifier = bossNotifierField?.GetValue(encounter) as EnemyDeathNotifier;
        if (bossNotifier == null) { Debug.LogError("Boss death notifier not found"); return; }

        // Helper to trigger win and log stars based on HP ratio
        void TestWin(float hpRatio, string label) {
            playerStats.WinFlowSetCurrentHP(hpRatio);
            int bossClearedCount = 0;
            void OnBossCleared() => bossClearedCount++;
            encounter.BossCleared += OnBossCleared;
            // Reset controller state if needed
            var controller = RunResultController.Instance;
            if (controller != null) {
                // Ensure clean state
                controller.OnRestartPressed();
            }
            bossNotifier.NotifyDied();
            encounter.BossCleared -= OnBossCleared;
            Debug.Assert(bossClearedCount == 1, $"BossCleared should fire exactly once for {label}, but fired {bossClearedCount} times.");
            // Log result
            var resultActive = RunResultController.Instance?.IsResultActive ?? false;
            Debug.Log($"[WinFlow {label}] ResultActive={resultActive} HPratio={hpRatio}");
            // Restart to hub for next test
            if (label != "LowHP")
            {
                RunResultController.Instance?.OnRestartPressed();
            }
        }

        TestWin(1.0f, "FullHP"); // expect 3 stars
        TestWin(0.5f, "MidHP");  // expect 2 stars
        TestWin(0.3f, "LowHP");  // expect 1 star
    }
}
