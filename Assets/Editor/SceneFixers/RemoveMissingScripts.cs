using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class RemoveMissingScripts
{
    [MenuItem("Tools/Scene Fix/Remove Missing Scripts In Open Scene")]
    public static void RemoveMissingFromOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogWarning("No scene is currently loaded.");
            return;
        }

        int totalRemoved = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            if (removed > 0)
            {
                Debug.Log($"Removed {removed} missing script components from '{root.name}'");
                totalRemoved += removed;
            }
        }

        if (totalRemoved > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene fix complete. Total missing components removed: {totalRemoved}");
        }
        else
        {
            Debug.Log("No missing script components found in the open scene.");
        }
    }
}
