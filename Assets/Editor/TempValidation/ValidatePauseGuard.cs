using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class ValidatePauseGuard : MonoBehaviour
{
    // Menu item to run the validation while Play mode is active
    [MenuItem("Tools/Validate Pause Guard")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("ValidatePauseGuard must be executed in Play mode.");
            return;
        }

        // 1️⃣ Trigger lose result via fatal damage
        var playerStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found on Player.");
            return;
        }
        playerStats.TakeDamage(9999f);

        // 2️⃣ Ensure result UI is active
        var runCtrl = RunResultController.Instance;
        if (runCtrl == null || !runCtrl.IsResultActive)
        {
            Debug.LogError("Result UI not active after fatal damage.");
            return;
        }

        // 3️⃣ Attempt to pause via the real pause method
        var pauseMenu = Object.FindFirstObjectByType<PauseMenu>();
        if (pauseMenu == null)
        {
            Debug.LogError("PauseMenu not found.");
            return;
        }
        pauseMenu.Pause(); // should be blocked while result UI is active

        // 4️⃣ Verify pause canvas stayed inactive
        bool canvasActive = pauseMenu.pauseCanvas != null && pauseMenu.pauseCanvas.activeSelf;
        Debug.Assert(!canvasActive, "PauseMenu should NOT open while result UI is active.");
        Debug.Log("[ValidatePauseGuard] PASS – PauseMenu remained closed during result UI.");

        // 5️⃣ Invoke real restart flow
        runCtrl.OnRestartPressed();

        // 6️⃣ Re‑query objects after scene reload (GameHome should be active now)
        var activeScene = SceneManager.GetActiveScene();
        Debug.Log($"[ValidatePauseGuard] Scene after restart: {activeScene.name}");

        var pauseMenuAfter = Object.FindFirstObjectByType<PauseMenu>();
        var runCtrlAfter = RunResultController.Instance;
        var canvasAfter = pauseMenuAfter?.pauseCanvas;
        var inputMgrAfter = InputManager.Instance;
        var canvasUI = GameObject.Find("Canvas_UI");

        if (pauseMenuAfter == null || runCtrlAfter == null || canvasAfter == null)
        {
            Debug.LogError("Failed to re‑acquire required objects after restart.");
            return;
        }

        // 7️⃣ Verify result UI is no longer active
        Debug.Assert(!runCtrlAfter.IsResultActive, "Result UI should be hidden after restart.");

        // 8️⃣ Now pause should work normally
        pauseMenuAfter.Pause();
        bool canvasNowActive = pauseMenuAfter.pauseCanvas != null && pauseMenuAfter.pauseCanvas.activeSelf;
        Debug.Assert(canvasNowActive, "PauseMenu should open after restart.");
        Debug.Log("[ValidatePauseGuard] PASS – Pause works normally after restart.");
    }
}
