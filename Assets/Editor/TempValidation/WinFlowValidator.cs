using UnityEditor;
using UnityEngine;

public static class WinFlowValidator {
    [MenuItem("Tools/Validate Win Flow")]
    public static void Execute() {
        // Ensure Dungeon scene is loaded
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Dungeon.unity");
        var dungeon = Object.FindFirstObjectByType<DungeonManager>();
        if (dungeon == null) { Debug.LogError("DungeonManager not found"); return; }
        // Force boss floor
        dungeon.currentFloor = dungeon.bossEveryXFloor; // assume bossEveryXFloor == 5
        dungeon.GenerateFloor();
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        var playerStats = playerObj?.GetComponent<PlayerStats>();
        if (playerStats == null) { Debug.LogError("PlayerStats not found"); return; }
        var bossNotifier = dungeon.GetBossDeathNotifier();
        if (bossNotifier == null) { Debug.LogError("Boss death notifier not found"); return; }

        // Helper to trigger win and log stars based on HP ratio
        void TestWin(float hpRatio, string label) {
            playerStats.currentHP = playerStats.GetTotalMaxHP() * hpRatio;
            // Reset controller state if needed
            var controller = RunResultController.Instance;
            if (controller != null) {
                // Ensure clean state
                controller.OnRestartPressed();
            }
            bossNotifier.NotifyDied();
            // Log result
            var resultActive = RunResultController.Instance?.IsResultActive ?? false;
            Debug.Log($"[WinFlow {label}] ResultActive={resultActive} HPratio={hpRatio}");
            // Restart to hub for next test
            RunResultController.Instance?.OnRestartPressed();
        }

        TestWin(1.0f, "FullHP"); // expect 3 stars
        TestWin(0.5f, "MidHP");  // expect 2 stars
        TestWin(0.3f, "LowHP");  // expect 1 star
    }
}
