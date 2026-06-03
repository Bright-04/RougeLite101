using UnityEngine;
using UnityEditor;

public class ValidateExitDoorBlocked : MonoBehaviour
{
    [MenuItem("Tools/Validate ExitDoor Blocked After Win")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("ValidateExitDoorBlocked must be executed in Play mode.");
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

        var runCtrl = RunResultController.Instance;
        if (runCtrl == null || !runCtrl.IsResultActive || !runCtrl.IsRunFinished)
        {
            Debug.LogError("Result UI is not active/finished; cannot run blocked ExitDoor test.");
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
            // Floor should NOT change
            Debug.Assert(floorAfter == floorBefore, $"Floor must NOT advance when result UI is active (before: {floorBefore}, after: {floorAfter}).");
            // Result state must stay finished and active
            Debug.Assert(runCtrl.IsRunFinished, "Run should remain finished after ignored door trigger.");
            Debug.Assert(runCtrl.IsResultActive, "Result UI should remain active after ignored door trigger.");
            Debug.Log($"[ValidateExitDoorBlocked] PASS – Door ignored while result UI active. Floor unchanged at {floorAfter}.");
        };
    }

    private static int GetCurrentFloor(DungeonManager mgr)
    {
        var type = mgr.GetType();
        var field = type.GetField("currentFloor");
        if (field != null) return (int)field.GetValue(mgr);
        var prop = type.GetProperty("currentFloor");
        if (prop != null) return (int)prop.GetValue(mgr);
        Debug.LogError("DungeonManager does not expose currentFloor.");
        return -1;
    }
}
