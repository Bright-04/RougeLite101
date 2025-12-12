using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor utility to validate and fix room prefabs
/// </summary>
public class RoomTemplateValidator : EditorWindow
{
    [MenuItem("Tools/Dungeon/Validate Room Templates")]
    public static void ShowWindow()
    {
        GetWindow<RoomTemplateValidator>("Room Template Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Room Template Validation", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Validate All Room Prefabs"))
        {
            ValidateAllRoomPrefabs();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Fix Selected Room Prefabs"))
        {
            FixSelectedRoomPrefabs();
        }

        GUILayout.Space(10);
        
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Click 'Validate All Room Prefabs' to check all room prefabs in the project");
        GUILayout.Label("2. Select room prefabs in the Project window and click 'Fix Selected Room Prefabs' to automatically add missing RoomTemplate components");
    }

    private void ValidateAllRoomPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Rooms" });
        int validatedCount = 0;
        int issuesFound = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                validatedCount++;
                var roomTemplate = prefab.GetComponent<RoomTemplate>();
                
                if (roomTemplate == null)
                {
                    Debug.LogWarning($"Room prefab '{prefab.name}' at '{path}' is missing RoomTemplate component!", prefab);
                    issuesFound++;
                }
                else
                {
                    // Check for missing references
                    if (roomTemplate.playerSpawn == null)
                    {
                        Debug.LogWarning($"Room '{prefab.name}' has no player spawn point set.", prefab);
                        issuesFound++;
                    }
                    if (roomTemplate.exitAnchor == null)
                    {
                        Debug.LogWarning($"Room '{prefab.name}' has no exit anchor set.", prefab);
                        issuesFound++;
                    }
                    if (roomTemplate.enemySpawns == null || roomTemplate.enemySpawns.Length == 0)
                    {
                        Debug.LogWarning($"Room '{prefab.name}' has no enemy spawn points set.", prefab);
                        issuesFound++;
                    }
                }
            }
        }

        Debug.Log($"Validation complete! Checked {validatedCount} room prefabs. Found {issuesFound} issues.");
    }

    private void FixSelectedRoomPrefabs()
    {
        var selectedObjects = Selection.gameObjects;
        int fixedCount = 0;

        foreach (var obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".prefab"))
            {
                var roomTemplate = obj.GetComponent<RoomTemplate>();
                if (roomTemplate == null)
                {
                    roomTemplate = obj.AddComponent<RoomTemplate>();
                    EditorUtility.SetDirty(obj);
                    Debug.Log($"Added RoomTemplate component to '{obj.name}'", obj);
                    fixedCount++;
                }
            }
        }

        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Fixed {fixedCount} room prefabs by adding RoomTemplate components.");
        }
        else
        {
            Debug.Log("No room prefabs needed fixing, or no prefabs were selected.");
        }
    }
}
#endif