using UnityEngine;
using UnityEditor;

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
        var bossNotifier = dm.GetBossDeathNotifier();
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
