using UnityEditor;
using UnityEngine;

public static class LogDungeonComponents
{
    [MenuItem("Tools/Scene Fix/Log Dungeon Components")]
    public static void LogComponents()
    {
        var go = GameObject.Find("Dungeon");
        if (go == null)
        {
            Debug.LogWarning("Dungeon GameObject not found in scene.");
            return;
        }

        var comps = go.GetComponents<Component>();
        Debug.Log($"Components on '{go.name}': count={comps.Length}");
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            if (c == null)
            {
                Debug.LogWarning($"[{i}] Missing script at component slot {i} on '{go.name}'");
                continue;
            }

            var mb = c as MonoBehaviour;
            if (mb != null)
            {
                var script = MonoScript.FromMonoBehaviour(mb);
                string guid = script != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script)) : "<no-monoscript>";
                Debug.Log($"[{i}] {c.GetType().FullName} (MonoScript GUID={guid})");
            }
            else
            {
                Debug.Log($"[{i}] {c.GetType().FullName}");
            }
        }
    }
}
