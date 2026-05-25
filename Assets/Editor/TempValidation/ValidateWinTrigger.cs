using UnityEngine;
using UnityEditor;
using System.Reflection;

public class ValidateWinTrigger : MonoBehaviour
{
    [MenuItem("Tools/Validate Win Trigger")] 
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("ValidateWinTrigger must be run in Play mode.");
            return;
        }
        var dm = Object.FindFirstObjectByType<DungeonManager>();
        if (dm == null)
        {
            Debug.LogError("DungeonManager not found.");
            return;
        }

        dm.currentFloor = dm.bossEveryXFloor;
        var startMethod = typeof(DungeonManager).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
        startMethod?.Invoke(dm, null);
        dm.GenerateFloor();

        var encounter = Object.FindFirstObjectByType<BossEncounterController>();
        if (encounter == null)
        {
            Debug.LogError("BossEncounterController not found.");
            return;
        }

        var bossNotifierField = typeof(BossEncounterController).GetField("registeredBossNotifier", BindingFlags.NonPublic | BindingFlags.Instance);
        var bossNotifier = bossNotifierField?.GetValue(encounter) as EnemyDeathNotifier;
        if (bossNotifier == null)
        {
            Debug.LogError("Boss Death Notifier not found.");
            return;
        }
        // Trigger the notifier through its public API to simulate boss defeat.
        bossNotifier.NotifyDied();
        Debug.Log("[ValidateWinTrigger] Boss death notifier invoked.");
    }
}
