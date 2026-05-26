using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RoomPrefabSpawnProfileChecker
{
    private static readonly string[] BossHintTokens = { "boss", "king", "slimeking", "bosshealth" };

    [MenuItem("Tools/Validate/Room Prefab SpawnProfile Checker")]
    public static void RunCheck()
    {
        string[] themeGuids = AssetDatabase.FindAssets("t:ThemeSO");
        int issues = 0;

        foreach (var guid in themeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSO>(path);
            if (theme == null) continue;

            CheckList(theme.enemyRoomPrefabs, theme, false, ref issues);
            CheckList(theme.bossRoomPrefabs, theme, true, ref issues);
        }

        if (issues == 0)
        {
            EditorUtility.DisplayDialog("Room Prefab SpawnProfile Checker", "No issues found.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Room Prefab SpawnProfile Checker", $"Found {issues} potential issues. See Console for details.", "OK");
        }
    }

    private static void CheckList(GameObject[] prefabs, ThemeSO theme, bool expectBossProfile, ref int issues)
    {
        if (prefabs == null) return;

        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            var rt = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)?.GetComponent<RoomTemplate>();
            if (rt == null)
            {
                // If prefab has no RoomTemplate, skip (could be a structure-only room)
                continue;
            }

            var spawnProfile = rt.spawnProfile;
            bool profileIndicatesBoss = false;

            if (spawnProfile != null)
            {
                // Heuristics: spawnProfile name contains 'boss' OR any entry prefab name/component suggests boss
                string spName = spawnProfile.name.ToLowerInvariant();
                if (ContainsBossHint(spName))
                    profileIndicatesBoss = true;

                var entries = spawnProfile.entries;
                if (entries != null)
                {
                    foreach (var e in entries)
                    {
                        if (e.prefab == null) continue;
                        string entryName = e.prefab.name.ToLowerInvariant();
                        if (ContainsBossHint(entryName))
                        {
                            profileIndicatesBoss = true;
                            break;
                        }

                        if (PrefabHasBossMarker(e.prefab))
                        {
                            profileIndicatesBoss = true;
                            break;
                        }
                    }
                }
            }

            if (expectBossProfile && !profileIndicatesBoss)
            {
                Debug.LogWarning($"[Validation] Theme '{theme.themeName}': bossRoom prefab '{prefab.name}' does not appear to contain a boss spawn profile.", prefab);
                issues++;
            }

            if (!expectBossProfile && profileIndicatesBoss)
            {
                Debug.LogWarning($"[Validation] Theme '{theme.themeName}': enemyRoom prefab '{prefab.name}' appears to spawn a boss (spawnProfile or entries indicate boss). Consider moving it to Boss Rooms.", prefab);
                issues++;
            }
        }
    }

    private static bool PrefabHasBossMarker(GameObject prefab)
    {
        string entryPrefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(entryPrefabPath))
        {
            return false;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(entryPrefabPath);
        try
        {
            var comps = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true);
            return comps.Any(c => c != null && ContainsBossHint(c.GetType().Name.ToLowerInvariant()));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool ContainsBossHint(string value)
    {
        return BossHintTokens.Any(value.Contains);
    }
}
