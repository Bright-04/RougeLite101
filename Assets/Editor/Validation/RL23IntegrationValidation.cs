using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RL23IntegrationValidation
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/GameHome.unity",
        "Assets/Scenes/Dungeon.unity"
    };

    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/Scenes Management/Player.prefab",
        "Assets/Prefabs/InventoryUI/InventoryUI.prefab",
        "Assets/Prefabs/Items/pickableItem.prefab",
        "Assets/Prefabs/Rooms/DarkFantasy/DFShop.prefab",
        "Assets/Prefabs/Rooms/Forests/Shop.prefab"
    };

    public static void Run()
    {
        var report = new StringBuilder();
        int missingScriptCount = 0;
        int missingReferenceCount = 0;

        foreach (string scenePath in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int sceneMissingScripts = 0;
            int sceneMissingReferences = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                sceneMissingScripts += CountMissingScripts(root);
                sceneMissingReferences += CountMissingReferences(root, scenePath, report);
            }

            missingScriptCount += sceneMissingScripts;
            missingReferenceCount += sceneMissingReferences;
            report.AppendLine($"SCENE {scenePath}: missingScripts={sceneMissingScripts}, missingReferences={sceneMissingReferences}");
        }

        foreach (string prefabPath in PrefabPaths)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            int prefabMissingScripts = CountMissingScripts(prefabRoot);
            int prefabMissingReferences = CountMissingReferences(prefabRoot, prefabPath, report);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            missingScriptCount += prefabMissingScripts;
            missingReferenceCount += prefabMissingReferences;
            report.AppendLine($"PREFAB {prefabPath}: missingScripts={prefabMissingScripts}, missingReferences={prefabMissingReferences}");
        }

        report.AppendLine($"SUMMARY missingScripts={missingScriptCount}, missingReferences={missingReferenceCount}");
        if (missingScriptCount == 0 && missingReferenceCount == 0)
        {
            Debug.Log(report.ToString());
            EditorApplication.Exit(0);
            return;
        }

        Debug.LogError(report.ToString());
        EditorApplication.Exit(2);
    }

    private static int CountMissingScripts(GameObject root)
    {
        int total = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            total += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
        }

        return total;
    }

    private static int CountMissingReferences(GameObject root, string ownerPath, StringBuilder report)
    {
        int total = 0;
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                continue;
            }

            var serializedObject = new SerializedObject(component);
            SerializedProperty iterator = serializedObject.GetIterator();
            while (iterator.NextVisible(true))
            {
                if (iterator.propertyPath == "m_Script" || iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.objectReferenceInstanceIDValue == 0 || iterator.objectReferenceValue != null)
                {
                    continue;
                }

                total++;
                report.AppendLine($"MISSING_REF {ownerPath} :: {component.GetType().Name} on {GetHierarchyPath(component.transform)} :: {iterator.propertyPath}");
            }
        }

        return total;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var segments = new List<string>();
        Transform current = transform;
        while (current != null)
        {
            segments.Add(current.name);
            current = current.parent;
        }

        segments.Reverse();
        return string.Join("/", segments);
    }
}
