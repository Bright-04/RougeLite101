using UnityEditor;
using UnityEngine;

public static class ValidateRestartMenu {
    [MenuItem("Tools/Validate Restart Flow")]
    public static void Execute() {
        var controller = RunResultController.Instance;
        if (controller != null) {
            controller.OnRestartPressed();
        } else {
            Debug.LogError("RunResultController instance not found.");
        }
    }
}
