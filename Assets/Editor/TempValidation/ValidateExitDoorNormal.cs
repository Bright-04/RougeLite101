using UnityEngine;
using UnityEditor;

public class ValidateExitDoorNormal : MonoBehaviour
{
    [MenuItem("Tools/Validate ExitDoor Normal Floor")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("ValidateExitDoorNormal must be executed in Play mode.");
            return;
        }

        var exitDoor = Object.FindFirstObjectByType<ExitDoor>();
        if (exitDoor == null)
        {
            Debug.LogError("ExitDoor not found in the scene.");
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player GameObject not found.");
            return;
        }

        var dungeonMgr = Object.FindFirstObjectByType<DungeonManager>();
        if (dungeonMgr == null)
        {
            Debug.LogError("DungeonManager not found.");
            return;
        }

        // Ensure result UI is NOT active
        var runCtrl = RunResultController.Instance;
        if (runCtrl != null && runCtrl.IsResultActive)
        {
            Debug.LogError("Result UI is active; cannot run normal floor test.");
            return;
        }

        int floorBefore = GetCurrentFloor(dungeonMgr);

        // Move player into the door's trigger bounds
        var col = exitDoor.GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("ExitDoor missing Collider2D.");
            return;
        }
        player.transform.position = col.bounds.center;

        // Allow physics to process a frame via delayed call
        EditorApplication.delayCall += () =>
        {
            int floorAfter = GetCurrentFloor(dungeonMgr);
            Debug.Assert(floorAfter == floorBefore + 1, "Floor should advance by one on normal ExitDoor trigger.");
            // Result UI should still be hidden
            var afterCtrl = RunResultController.Instance;
            bool resultActive = afterCtrl != null && afterCtrl.IsResultActive;
            Debug.Assert(!resultActive, "Result UI must remain hidden on normal floor.");
            Debug.Log($"[ValidateExitDoorNormal] PASS – Floor advanced from {floorBefore} to {floorAfter}.");
        };
    }

    private static int GetCurrentFloor(DungeonManager mgr)
    {
        // Attempt to read a public field/property named currentFloor
        var type = mgr.GetType();
        var field = type.GetField("currentFloor");
        if (field != null) return (int)field.GetValue(mgr);
        var prop = type.GetProperty("currentFloor");
        if (prop != null) return (int)prop.GetValue(mgr);
        Debug.LogError("DungeonManager does not expose currentFloor.");
        return -1;
    }
}
