using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonThemeTemplateCloner : EditorWindow
{
    private const string WindowTitle = "Theme Template Cloner";
    private const string RoomsRootFolder = "Assets/Prefabs/Rooms";
    private const string ThemesFolder = "Assets/ScriptableObjects/Themes";
    private const string MenuRoot = "Tools/RougeLite/Dungeon";

    private static readonly string[] InvalidThemeNameCharacters =
    {
        "/", "\\", ":", "*", "?", "\"", "<", ">", "|"
    };

    private static readonly BundleRole[] BundleRoleOrder =
    {
        BundleRole.Spawn,
        BundleRole.Exit,
        BundleRole.Enemy,
        BundleRole.Boss,
        BundleRole.Buff,
        BundleRole.Special,
        BundleRole.Corridor,
        BundleRole.BigDoor,
        BundleRole.SmallDoor,
        BundleRole.BigWall,
        BundleRole.SmallWall
    };

    private static readonly Dictionary<BundleRole, string> ExpectedSourcePrefabNames = new Dictionary<BundleRole, string>
    {
        { BundleRole.Spawn, "DFSpawn" },
        { BundleRole.Exit, "DFExitRoom" },
        { BundleRole.Enemy, "DFEnemyRoom" },
        { BundleRole.Boss, "DFBossRoom" },
        { BundleRole.Buff, "DFBuff" },
        { BundleRole.Special, "DFShop" },
        { BundleRole.Corridor, "DFCorridor" },
        { BundleRole.BigDoor, "DFBigDoor" },
        { BundleRole.SmallDoor, "DFSmallDoor" },
        { BundleRole.BigWall, "DFBigWall" },
        { BundleRole.SmallWall, "DFSmallWall" }
    };

    private static readonly Dictionary<BundleRole, string> GeneratedPrefabNames = new Dictionary<BundleRole, string>
    {
        { BundleRole.Spawn, "{0}Spawn" },
        { BundleRole.Exit, "{0}ExitRoom" },
        { BundleRole.Enemy, "{0}EnemyRoom" },
        { BundleRole.Boss, "{0}BossRoom" },
        { BundleRole.Buff, "{0}Buff" },
        { BundleRole.Special, "{0}Shop" },
        { BundleRole.Corridor, "{0}Corridor" },
        { BundleRole.BigDoor, "{0}BigDoor" },
        { BundleRole.SmallDoor, "{0}SmallDoor" },
        { BundleRole.BigWall, "{0}BigWall" },
        { BundleRole.SmallWall, "{0}SmallWall" }
    };

    private string newThemeName = "CrystalCave";
    private SourceBundle sourceBundle;
    private Vector2 scrollPosition;
    private string lastReport = string.Empty;

    [MenuItem(MenuRoot + "/Clone Theme From Template")]
    public static void ShowWindow()
    {
        var window = GetWindow<DungeonThemeTemplateCloner>(true, WindowTitle);
        window.minSize = new Vector2(540f, 520f);
        window.RefreshSourceBundle();
    }

    [MenuItem(MenuRoot + "/Validate All Themes")]
    public static void ValidateAllThemesMenu()
    {
        var bundle = DetectSourceBundle();
        var report = new ValidationReport("Validate All Themes");

        foreach (var theme in LoadAllThemes())
        {
            ValidateTheme(theme, bundle, report);
        }

        report.LogToConsole();
        EditorUtility.DisplayDialog("Validate All Themes", report.GetDialogSummary(), "OK");
    }

    private void OnEnable()
    {
        RefreshSourceBundle();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("DarkFantasy Template Theme Cloner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Clones the detected DarkFantasy prefab bundle into a new theme folder, creates a ThemeSO, rewires room door/wall references, and validates the result.",
            MessageType.Info);

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto-Detect DF Bundle", GUILayout.Height(24f)))
            {
                RefreshSourceBundle();
            }

            if (GUILayout.Button("Validate Detected Bundle", GUILayout.Height(24f)))
            {
                ValidateDetectedBundle();
            }
        }

        EditorGUILayout.Space();
        newThemeName = EditorGUILayout.TextField("New Theme Name", newThemeName);

        DrawSourceBundleInspector();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(sourceBundle == null))
        {
            if (GUILayout.Button("Clone Theme From Template", GUILayout.Height(32f)))
            {
                CloneThemeFromTemplate();
            }
        }

        if (!string.IsNullOrWhiteSpace(lastReport))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Report", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(lastReport, GUILayout.MinHeight(160f));
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSourceBundleInspector()
    {
        EditorGUILayout.LabelField("Detected Source Bundle", EditorStyles.boldLabel);

        if (sourceBundle == null)
        {
            EditorGUILayout.HelpBox("DarkFantasy source bundle not detected yet.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Bundle Folder", sourceBundle.BundleFolderPath ?? "<missing>");
        using (new EditorGUI.DisabledScope(true))
        {
            sourceBundle.InferredTheme = (ThemeSO)EditorGUILayout.ObjectField("Inferred ThemeSO", sourceBundle.InferredTheme, typeof(ThemeSO), false);
        }

        EditorGUILayout.Space(4f);

        foreach (var role in BundleRoleOrder)
        {
            sourceBundle.Prefabs.TryGetValue(role, out var prefab);
            sourceBundle.Prefabs[role] = (GameObject)EditorGUILayout.ObjectField(GetRoleLabel(role), prefab, typeof(GameObject), false);
        }
    }

    private void RefreshSourceBundle()
    {
        sourceBundle = DetectSourceBundle();
        lastReport = sourceBundle == null
            ? "DarkFantasy source bundle not found."
            : $"Detected source bundle: {sourceBundle.BundleFolderPath}";
        Repaint();
    }

    private void ValidateDetectedBundle()
    {
        if (sourceBundle == null)
        {
            EditorUtility.DisplayDialog("Validate Detected Bundle", "DarkFantasy source bundle could not be detected.", "OK");
            return;
        }

        var report = new ValidationReport("Validate Detected Bundle");
        ValidateBundleCompleteness(sourceBundle, report);

        foreach (var pair in sourceBundle.Prefabs)
        {
            var roomRole = ToRoomRole(pair.Key);
            if (roomRole.HasValue)
            {
                ValidateRoomPrefab(pair.Value, roomRole.Value, report);
            }
            else if (pair.Key == BundleRole.Corridor)
            {
                ValidateCorridorPrefab(pair.Value, report);
            }
        }

        report.LogToConsole();
        lastReport = report.GetDetailedText();
        EditorUtility.DisplayDialog("Validate Detected Bundle", report.GetDialogSummary(), "OK");
    }

    private void CloneThemeFromTemplate()
    {
        if (sourceBundle == null)
        {
            EditorUtility.DisplayDialog("Clone Theme From Template", "No DarkFantasy source prefab bundle is detected.", "OK");
            return;
        }

        if (!TrySanitizeThemeName(newThemeName, out var sanitizedThemeName, out var validationError))
        {
            EditorUtility.DisplayDialog("Clone Theme From Template", validationError, "OK");
            return;
        }

        var validationReport = new ValidationReport($"Clone Theme From Template - {sanitizedThemeName}");
        ValidateBundleCompleteness(sourceBundle, validationReport);

        if (validationReport.ErrorCount > 0)
        {
            validationReport.LogToConsole();
            lastReport = validationReport.GetDetailedText();
            EditorUtility.DisplayDialog("Clone Theme From Template", validationReport.GetDialogSummary(), "OK");
            return;
        }

        var generatedAssets = new Dictionary<BundleRole, GeneratedPrefabInfo>();
        ThemeSO generatedTheme = null;
        var generatedPrefabFolder = $"{RoomsRootFolder}/{sanitizedThemeName}";

        try
        {
            EnsureFolderExists(generatedPrefabFolder);
            EnsureFolderExists(ThemesFolder);

            foreach (var role in BundleRoleOrder)
            {
                var sourcePrefab = sourceBundle.Prefabs[role];
                if (sourcePrefab == null)
                {
                    continue;
                }

                var targetName = GetGeneratedPrefabName(role, sanitizedThemeName);
                var desiredPath = $"{generatedPrefabFolder}/{targetName}.prefab";
                var uniquePath = AssetDatabase.GenerateUniqueAssetPath(desiredPath);
                var sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);

                if (!AssetDatabase.CopyAsset(sourcePath, uniquePath))
                {
                    throw new InvalidOperationException($"Failed to copy prefab '{sourcePath}' to '{uniquePath}'.");
                }

                var clonedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(uniquePath);
                if (clonedPrefab == null)
                {
                    throw new InvalidOperationException($"Copied prefab could not be loaded at '{uniquePath}'.");
                }

                RenamePrefabRoot(uniquePath, Path.GetFileNameWithoutExtension(uniquePath));

                generatedAssets[role] = new GeneratedPrefabInfo
                {
                    Role = role,
                    AssetPath = uniquePath,
                    Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(uniquePath)
                };
            }

            RewireGeneratedPrefabs(sourceBundle, generatedAssets, validationReport);

            var themeAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{ThemesFolder}/{sanitizedThemeName}.asset");
            generatedTheme = ScriptableObject.CreateInstance<ThemeSO>();
            generatedTheme.themeName = sanitizedThemeName;
            generatedTheme.spawnRoomPrefab = GetGeneratedPrefab(BundleRole.Spawn, generatedAssets);
            generatedTheme.exitRoomPrefab = GetGeneratedPrefab(BundleRole.Exit, generatedAssets);
            generatedTheme.enemyRoomPrefabs = ToSingleElementArray(BundleRole.Enemy, generatedAssets);
            generatedTheme.specialRoomPrefabs = ToSingleElementArray(BundleRole.Special, generatedAssets);
            generatedTheme.buffRoomPrefabs = ToSingleElementArray(BundleRole.Buff, generatedAssets);
            generatedTheme.bossRoomPrefabs = ToSingleElementArray(BundleRole.Boss, generatedAssets);
            generatedTheme.corridorPrefab = GetGeneratedPrefab(BundleRole.Corridor, generatedAssets);
            generatedTheme.spawnPacks = sourceBundle.InferredTheme != null && sourceBundle.InferredTheme.spawnPacks != null
                ? sourceBundle.InferredTheme.spawnPacks.Where(pack => pack != null).ToArray()
                : Array.Empty<SpawnPackSO>();

            AssetDatabase.CreateAsset(generatedTheme, themeAssetPath);
            EditorUtility.SetDirty(generatedTheme);
            Undo.RegisterCreatedObjectUndo(generatedTheme, $"Create {sanitizedThemeName} Theme");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            generatedTheme = AssetDatabase.LoadAssetAtPath<ThemeSO>(themeAssetPath);
            ValidateTheme(generatedTheme, sourceBundle, validationReport);
            ValidateThemeLeakCheck(generatedTheme, sourceBundle, generatedAssets, validationReport);
            ReportReuse(generatedTheme, generatedAssets, validationReport);

            validationReport.AddInfo($"Generated theme asset: {themeAssetPath}");
            validationReport.AddInfo($"Generated prefab folder: {generatedPrefabFolder}");
            validationReport.AddInfo($"Source bundle preserved: no source DF/Forest asset mutation performed.");
            validationReport.LogToConsole();
            lastReport = validationReport.GetDetailedText();
            EditorGUIUtility.PingObject(generatedTheme);
            EditorUtility.DisplayDialog("Clone Theme From Template", validationReport.GetDialogSummary(), "OK");
        }
        catch (Exception ex)
        {
            validationReport.AddError($"Generation failed: {ex.Message}");
            Debug.LogException(ex);
            validationReport.LogToConsole();
            lastReport = validationReport.GetDetailedText();
            EditorUtility.DisplayDialog("Clone Theme From Template", validationReport.GetDialogSummary(), "OK");
        }
    }

    private static void RewireGeneratedPrefabs(
        SourceBundle bundle,
        IReadOnlyDictionary<BundleRole, GeneratedPrefabInfo> generatedAssets,
        ValidationReport report)
    {
        foreach (var pair in generatedAssets)
        {
            if (!IsRoomOrCorridor(pair.Key))
            {
                continue;
            }

            var prefabPath = pair.Value.AssetPath;
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                prefabRoot.name = Path.GetFileNameWithoutExtension(prefabPath);

                if (pair.Key == BundleRole.Corridor)
                {
                    RewireSerializedObjectReferences(prefabRoot, bundle, generatedAssets);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    continue;
                }

                var roomTemplate = prefabRoot.GetComponent<RoomTemplate>() ?? prefabRoot.GetComponentInChildren<RoomTemplate>(true);
                if (roomTemplate != null)
                {
                    roomTemplate.doorPrefab = ResolveGeneratedConnectionPrefab(roomTemplate.doorPrefab, bundle, generatedAssets);
                    roomTemplate.wallPrefab = ResolveGeneratedConnectionPrefab(roomTemplate.wallPrefab, bundle, generatedAssets);
                    EditorUtility.SetDirty(roomTemplate);
                }

                RewireSerializedObjectReferences(prefabRoot, bundle, generatedAssets);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        report.AddInfo("Generated prefabs rewired to cloned door/wall prefabs where applicable.");
    }

    private static void RewireSerializedObjectReferences(
        GameObject prefabRoot,
        SourceBundle bundle,
        IReadOnlyDictionary<BundleRole, GeneratedPrefabInfo> generatedAssets)
    {
        var transforms = prefabRoot.GetComponentsInChildren<Transform>(true);
        foreach (var transform in transforms)
        {
            var components = transform.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(component);
                var iterator = serializedObject.GetIterator();
                var changed = false;

                while (iterator.NextVisible(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    if (!(iterator.objectReferenceValue is GameObject referencedGameObject))
                    {
                        continue;
                    }

                    if (!TryMapSourceConnectionPrefab(referencedGameObject, bundle, out var role))
                    {
                        continue;
                    }

                    if (!generatedAssets.TryGetValue(role, out var generatedInfo) || generatedInfo.Prefab == null)
                    {
                        continue;
                    }

                    iterator.objectReferenceValue = generatedInfo.Prefab;
                    changed = true;
                }

                if (changed)
                {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(component);
                }
            }
        }
    }

    private static GameObject ResolveGeneratedConnectionPrefab(
        GameObject originalReference,
        SourceBundle bundle,
        IReadOnlyDictionary<BundleRole, GeneratedPrefabInfo> generatedAssets)
    {
        if (originalReference == null)
        {
            return null;
        }

        if (!TryMapSourceConnectionPrefab(originalReference, bundle, out var role))
        {
            return originalReference;
        }

        return generatedAssets.TryGetValue(role, out var generatedInfo) && generatedInfo.Prefab != null
            ? generatedInfo.Prefab
            : originalReference;
    }

    private static bool TryMapSourceConnectionPrefab(GameObject prefab, SourceBundle bundle, out BundleRole role)
    {
        var prefabPath = AssetDatabase.GetAssetPath(prefab);
        foreach (var candidate in new[] { BundleRole.BigDoor, BundleRole.SmallDoor, BundleRole.BigWall, BundleRole.SmallWall })
        {
            if (bundle.Prefabs.TryGetValue(candidate, out var sourcePrefab)
                && sourcePrefab != null
                && AssetDatabase.GetAssetPath(sourcePrefab) == prefabPath)
            {
                role = candidate;
                return true;
            }
        }

        role = BundleRole.Spawn;
        return false;
    }

    private static void RenamePrefabRoot(string prefabPath, string rootName)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            prefabRoot.name = rootName;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static SourceBundle DetectSourceBundle()
    {
        var bundle = new SourceBundle();

        foreach (var prefabPath in AssetDatabase.FindAssets("t:Prefab", new[] { RoomsRootFolder })
                     .Select(AssetDatabase.GUIDToAssetPath))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            foreach (var expected in ExpectedSourcePrefabNames)
            {
                if (prefab.name != expected.Value)
                {
                    continue;
                }

                bundle.BundleFolderPath = Path.GetDirectoryName(prefabPath)?.Replace("\\", "/");
                bundle.Prefabs[expected.Key] = prefab;
                break;
            }
        }

        if (bundle.Prefabs.Count == 0)
        {
            return null;
        }

        bundle.InferredTheme = FindMatchingThemeForBundle(bundle);
        return bundle;
    }

    private static ThemeSO FindMatchingThemeForBundle(SourceBundle bundle)
    {
        foreach (var theme in LoadAllThemes())
        {
            if (theme == null)
            {
                continue;
            }

            if (!MatchesBundle(theme.spawnRoomPrefab, bundle, BundleRole.Spawn))
            {
                continue;
            }

            if (!MatchesBundle(theme.exitRoomPrefab, bundle, BundleRole.Exit))
            {
                continue;
            }

            if (!MatchesBundle(theme.corridorPrefab, bundle, BundleRole.Corridor))
            {
                continue;
            }

            if (!MatchesBundle((theme.enemyRoomPrefabs ?? Array.Empty<GameObject>()).FirstOrDefault(), bundle, BundleRole.Enemy))
            {
                continue;
            }

            if (!MatchesBundle((theme.specialRoomPrefabs ?? Array.Empty<GameObject>()).FirstOrDefault(), bundle, BundleRole.Special))
            {
                continue;
            }

            if (!MatchesBundle((theme.buffRoomPrefabs ?? Array.Empty<GameObject>()).FirstOrDefault(), bundle, BundleRole.Buff))
            {
                continue;
            }

            if (!MatchesBundle((theme.bossRoomPrefabs ?? Array.Empty<GameObject>()).FirstOrDefault(), bundle, BundleRole.Boss))
            {
                continue;
            }

            return theme;
        }

        return null;
    }

    private static bool MatchesBundle(GameObject candidate, SourceBundle bundle, BundleRole role)
    {
        if (candidate == null || !bundle.Prefabs.TryGetValue(role, out var sourcePrefab) || sourcePrefab == null)
        {
            return false;
        }

        return AssetDatabase.GetAssetPath(candidate) == AssetDatabase.GetAssetPath(sourcePrefab);
    }

    private static IEnumerable<ThemeSO> LoadAllThemes()
    {
        return AssetDatabase.FindAssets("t:ThemeSO", new[] { ThemesFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ThemeSO>)
            .Where(theme => theme != null);
    }

    private static void EnsureFolderExists(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
        {
            return;
        }

        var normalized = assetFolderPath.Replace("\\", "/");
        var parts = normalized.Split('/');
        var current = parts[0];

        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static bool TrySanitizeThemeName(string rawName, out string sanitizedName, out string error)
    {
        sanitizedName = (rawName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            error = "Theme name cannot be empty or whitespace.";
            return false;
        }

        foreach (var invalidCharacter in InvalidThemeNameCharacters)
        {
            if (sanitizedName.Contains(invalidCharacter, StringComparison.Ordinal))
            {
                error = $"Theme name contains invalid character '{invalidCharacter}'.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static void ValidateBundleCompleteness(SourceBundle bundle, ValidationReport report)
    {
        if (bundle == null)
        {
            report.AddError("DarkFantasy source bundle was not detected.");
            return;
        }

        foreach (var expected in ExpectedSourcePrefabNames)
        {
            if (!bundle.Prefabs.TryGetValue(expected.Key, out var prefab) || prefab == null)
            {
                report.AddError($"Missing source prefab for role {GetRoleLabel(expected.Key)}. Expected prefab name '{expected.Value}'.");
            }
        }

        if (bundle.InferredTheme != null)
        {
            report.AddInfo($"Inferred source ThemeSO for spawn packs: {AssetDatabase.GetAssetPath(bundle.InferredTheme)}");
        }
        else
        {
            report.AddWarning("No matching source ThemeSO found for spawnPack inference. Generation can proceed with empty spawnPacks.");
        }
    }

    private static void ValidateTheme(ThemeSO theme, SourceBundle bundle, ValidationReport report)
    {
        if (theme == null)
        {
            report.AddError("ThemeSO is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(theme.themeName))
        {
            report.AddError($"Theme '{theme.name}' has an empty themeName.");
        }

        if (theme.spawnRoomPrefab == null)
        {
            report.AddError($"Theme '{theme.name}' is missing spawnRoomPrefab.");
        }

        if (theme.exitRoomPrefab == null)
        {
            report.AddError($"Theme '{theme.name}' is missing exitRoomPrefab.");
        }

        if (theme.enemyRoomPrefabs == null || theme.enemyRoomPrefabs.All(prefab => prefab == null))
        {
            report.AddError($"Theme '{theme.name}' has no enemyRoomPrefabs assigned.");
        }

        if (theme.corridorPrefab == null)
        {
            report.AddError($"Theme '{theme.name}' is missing corridorPrefab.");
        }

        if (theme.buffRoomPrefabs == null || theme.buffRoomPrefabs.All(prefab => prefab == null))
        {
            report.AddWarning($"Theme '{theme.name}' has no buffRoomPrefabs assigned.");
        }

        if (theme.specialRoomPrefabs == null || theme.specialRoomPrefabs.All(prefab => prefab == null))
        {
            report.AddWarning($"Theme '{theme.name}' has no specialRoomPrefabs assigned.");
        }

        if (theme.bossRoomPrefabs == null || theme.bossRoomPrefabs.All(prefab => prefab == null))
        {
            report.AddWarning($"Theme '{theme.name}' has no bossRoomPrefabs assigned.");
        }

        if (theme.spawnPacks == null || theme.spawnPacks.Length == 0 || theme.spawnPacks.All(pack => pack == null))
        {
            report.AddWarning($"Theme '{theme.name}' has no spawnPacks assigned.");
        }

        if (theme.spawnRoomPrefab != null)
        {
            ValidateRoomPrefab(theme.spawnRoomPrefab, RoomRole.Spawn, report);
        }

        if (theme.exitRoomPrefab != null)
        {
            ValidateRoomPrefab(theme.exitRoomPrefab, RoomRole.Exit, report);
        }

        if (theme.enemyRoomPrefabs != null)
        {
            foreach (var prefab in theme.enemyRoomPrefabs.Where(prefab => prefab != null))
            {
                ValidateRoomPrefab(prefab, RoomRole.Enemy, report);
            }
        }

        if (theme.specialRoomPrefabs != null)
        {
            foreach (var prefab in theme.specialRoomPrefabs.Where(prefab => prefab != null))
            {
                ValidateRoomPrefab(prefab, RoomRole.Special, report);
            }
        }

        if (theme.buffRoomPrefabs != null)
        {
            foreach (var prefab in theme.buffRoomPrefabs.Where(prefab => prefab != null))
            {
                ValidateRoomPrefab(prefab, RoomRole.Buff, report);
            }
        }

        if (theme.bossRoomPrefabs != null)
        {
            foreach (var prefab in theme.bossRoomPrefabs.Where(prefab => prefab != null))
            {
                ValidateRoomPrefab(prefab, RoomRole.Boss, report);
            }
        }

        if (theme.corridorPrefab != null)
        {
            ValidateCorridorPrefab(theme.corridorPrefab, report);
        }

        if (bundle != null)
        {
            ValidateThemeLeakCheck(theme, bundle, null, report);
        }
    }

    private static void ValidateRoomPrefab(GameObject prefab, RoomRole role, ValidationReport report)
    {
        if (prefab == null)
        {
            report.AddError($"Missing prefab for role {role}.");
            return;
        }

        var roomTemplate = prefab.GetComponent<RoomTemplate>() ?? prefab.GetComponentInChildren<RoomTemplate>(true);
        if (roomTemplate == null)
        {
            var message = $"Room prefab '{prefab.name}' is missing RoomTemplate.";
            if (role == RoomRole.Enemy)
            {
                report.AddError(message);
            }
            else
            {
                report.AddWarning(message);
            }

            return;
        }

        ValidateTransformReference(roomTemplate.center, roomTemplate, role, "center", report, role == RoomRole.Enemy);
        ValidateTransformReference(roomTemplate.northSocket, roomTemplate, role, "northSocket", report, role == RoomRole.Enemy);
        ValidateTransformReference(roomTemplate.southSocket, roomTemplate, role, "southSocket", report, role == RoomRole.Enemy);
        ValidateTransformReference(roomTemplate.eastSocket, roomTemplate, role, "eastSocket", report, role == RoomRole.Enemy);
        ValidateTransformReference(roomTemplate.westSocket, roomTemplate, role, "westSocket", report, role == RoomRole.Enemy);
        ValidateObjectReference(roomTemplate.doorPrefab, roomTemplate, role, "doorPrefab", report, role == RoomRole.Enemy);
        ValidateObjectReference(roomTemplate.wallPrefab, roomTemplate, role, "wallPrefab", report, role == RoomRole.Enemy);

        if (role == RoomRole.Spawn && roomTemplate.playerSpawn == null)
        {
            report.AddError($"Spawn room '{prefab.name}' is missing playerSpawn.");
        }

        if (role == RoomRole.Exit && roomTemplate.exitAnchor == null)
        {
            report.AddWarning($"Exit room '{prefab.name}' is missing exitAnchor.");
        }

        if ((role == RoomRole.Enemy || role == RoomRole.Boss) && prefab.GetComponent<BoxCollider2D>() == null)
        {
            if (role == RoomRole.Enemy)
            {
                report.AddError($"Enemy room '{prefab.name}' is missing BoxCollider2D required by room-bounds enemy spawning.");
            }
            else
            {
                report.AddWarning($"Boss room '{prefab.name}' is missing BoxCollider2D.");
            }
        }

        var collision = prefab.transform.Find("Collision");
        if (collision != null)
        {
            if (collision.GetComponent<TilemapCollider2D>() == null)
            {
                report.AddWarning($"Room '{prefab.name}' has a Collision child without TilemapCollider2D.");
            }

            var renderer = collision.GetComponent<TilemapRenderer>();
            if (renderer != null && renderer.enabled)
            {
                report.AddWarning($"Room '{prefab.name}' has Collision TilemapRenderer enabled.");
            }
        }
    }

    private static void ValidateCorridorPrefab(GameObject prefab, ValidationReport report)
    {
        if (prefab == null)
        {
            report.AddError("Corridor prefab is missing.");
            return;
        }

        var connectionPoints = prefab.GetComponentInChildren<ConnectionPoints>(true);
        if (connectionPoints == null)
        {
            report.AddError($"Corridor prefab '{prefab.name}' is missing ConnectionPoints.");
            return;
        }

        if (connectionPoints.center == null)
        {
            report.AddError($"Corridor prefab '{prefab.name}' has ConnectionPoints without center.");
        }

        var assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));
        if (!string.Equals(prefab.name, assetName, StringComparison.Ordinal))
        {
            report.AddWarning($"Corridor prefab root '{prefab.name}' does not match asset name '{assetName}'.");
        }
    }

    private static void ValidateThemeLeakCheck(
        ThemeSO theme,
        SourceBundle bundle,
        IReadOnlyDictionary<BundleRole, GeneratedPrefabInfo> generatedAssets,
        ValidationReport report)
    {
        if (theme == null || bundle == null)
        {
            return;
        }

        var isSourceTheme = bundle.InferredTheme != null
            && AssetDatabase.GetAssetPath(bundle.InferredTheme) == AssetDatabase.GetAssetPath(theme);

        if (!isSourceTheme)
        {
            ValidateThemePrefabLeak(theme.themeName, "spawnRoomPrefab", theme.spawnRoomPrefab, bundle, report);
            ValidateThemePrefabLeak(theme.themeName, "exitRoomPrefab", theme.exitRoomPrefab, bundle, report);
            ValidateThemePrefabLeak(theme.themeName, "corridorPrefab", theme.corridorPrefab, bundle, report);

            ValidateThemePrefabLeak(theme.themeName, "enemyRoomPrefabs", theme.enemyRoomPrefabs, bundle, report);
            ValidateThemePrefabLeak(theme.themeName, "specialRoomPrefabs", theme.specialRoomPrefabs, bundle, report);
            ValidateThemePrefabLeak(theme.themeName, "buffRoomPrefabs", theme.buffRoomPrefabs, bundle, report);
            ValidateThemePrefabLeak(theme.themeName, "bossRoomPrefabs", theme.bossRoomPrefabs, bundle, report);
            
            foreach (var roomPrefab in EnumerateThemeRooms(theme))
            {
                if (roomPrefab == null)
                {
                    continue;
                }

                var roomTemplate = roomPrefab.GetComponent<RoomTemplate>() ?? roomPrefab.GetComponentInChildren<RoomTemplate>(true);
                if (roomTemplate == null)
                {
                    continue;
                }

                if (IsSourceConnectionPrefab(roomTemplate.doorPrefab, bundle))
                {
                    report.AddError($"Theme '{theme.themeName}' room '{roomPrefab.name}' still references source door prefab '{roomTemplate.doorPrefab.name}'.");
                }

                if (IsSourceConnectionPrefab(roomTemplate.wallPrefab, bundle))
                {
                    report.AddError($"Theme '{theme.themeName}' room '{roomPrefab.name}' still references source wall prefab '{roomTemplate.wallPrefab.name}'.");
                }
            }
        }

        if (generatedAssets != null)
        {
            foreach (var generated in generatedAssets.Values)
            {
                if (generated.Prefab == null)
                {
                    continue;
                }

                if (IsSourceBundlePrefab(generated.Prefab, bundle))
                {
                    report.AddError($"Generated prefab '{generated.Prefab.name}' still resolves to a source bundle path.");
                }
            }
        }
    }

    private static void ValidateThemePrefabLeak(string themeName, string fieldName, IEnumerable<GameObject> prefabs, SourceBundle bundle, ValidationReport report)
    {
        if (prefabs == null)
        {
            return;
        }

        foreach (var prefab in prefabs.Where(prefab => prefab != null))
        {
            ValidateThemePrefabLeak(themeName, fieldName, prefab, bundle, report);
        }
    }

    private static void ValidateThemePrefabLeak(string themeName, string fieldName, GameObject prefab, SourceBundle bundle, ValidationReport report)
    {
        if (prefab == null)
        {
            return;
        }

        if (IsSourceRoomOrCorridorPrefab(prefab, bundle))
        {
            report.AddError($"Theme '{themeName}' field '{fieldName}' still references source prefab '{prefab.name}'.");
        }
    }

    private static bool IsSourceRoomOrCorridorPrefab(GameObject prefab, SourceBundle bundle)
    {
        return MatchesAnySourcePrefab(prefab, bundle, BundleRole.Spawn, BundleRole.Exit, BundleRole.Enemy, BundleRole.Boss, BundleRole.Buff, BundleRole.Special, BundleRole.Corridor);
    }

    private static bool IsSourceConnectionPrefab(GameObject prefab, SourceBundle bundle)
    {
        return MatchesAnySourcePrefab(prefab, bundle, BundleRole.BigDoor, BundleRole.SmallDoor, BundleRole.BigWall, BundleRole.SmallWall);
    }

    private static bool IsSourceBundlePrefab(GameObject prefab, SourceBundle bundle)
    {
        return MatchesAnySourcePrefab(prefab, bundle, BundleRoleOrder);
    }

    private static bool MatchesAnySourcePrefab(GameObject prefab, SourceBundle bundle, params BundleRole[] roles)
    {
        if (prefab == null)
        {
            return false;
        }

        var prefabPath = AssetDatabase.GetAssetPath(prefab);
        return roles.Any(role =>
            bundle.Prefabs.TryGetValue(role, out var sourcePrefab)
            && sourcePrefab != null
            && AssetDatabase.GetAssetPath(sourcePrefab) == prefabPath);
    }

    private static void ReportReuse(
        ThemeSO theme,
        IReadOnlyDictionary<BundleRole, GeneratedPrefabInfo> generatedAssets,
        ValidationReport report)
    {
        var reusedSpawnProfiles = new HashSet<string>();
        var reusedEnemyPrefabs = new HashSet<string>();
        var reusedSpawnPacks = new HashSet<string>();

        foreach (var roomPrefab in EnumerateThemeRooms(theme))
        {
            var roomTemplate = roomPrefab != null
                ? roomPrefab.GetComponent<RoomTemplate>() ?? roomPrefab.GetComponentInChildren<RoomTemplate>(true)
                : null;

            if (roomTemplate?.spawnProfile != null)
            {
                reusedSpawnProfiles.Add(AssetDatabase.GetAssetPath(roomTemplate.spawnProfile));
                foreach (var entry in roomTemplate.spawnProfile.entries ?? Array.Empty<RoomSpawnProfileSO.Entry>())
                {
                    if (entry?.prefab != null)
                    {
                        reusedEnemyPrefabs.Add(AssetDatabase.GetAssetPath(entry.prefab));
                    }
                }
            }
        }

        foreach (var spawnPack in theme.spawnPacks ?? Array.Empty<SpawnPackSO>())
        {
            if (spawnPack != null)
            {
                reusedSpawnPacks.Add(AssetDatabase.GetAssetPath(spawnPack));
            }
        }

        if (reusedSpawnProfiles.Count > 0)
        {
            report.AddInfo("Reused spawn profiles:");
            foreach (var path in reusedSpawnProfiles.OrderBy(path => path, StringComparer.Ordinal))
            {
                report.AddInfo($"  - {path}");
            }
        }

        if (reusedSpawnPacks.Count > 0)
        {
            report.AddInfo("Reused spawn packs:");
            foreach (var path in reusedSpawnPacks.OrderBy(path => path, StringComparer.Ordinal))
            {
                report.AddInfo($"  - {path}");
            }
        }

        if (reusedEnemyPrefabs.Count > 0)
        {
            report.AddInfo("Reused enemy prefabs:");
            foreach (var path in reusedEnemyPrefabs.OrderBy(path => path, StringComparer.Ordinal))
            {
                report.AddInfo($"  - {path}");
            }
        }

        report.AddInfo($"Generated prefab count: {generatedAssets.Count}");
    }

    private static IEnumerable<GameObject> EnumerateThemeRooms(ThemeSO theme)
    {
        if (theme == null)
        {
            yield break;
        }

        if (theme.spawnRoomPrefab != null)
        {
            yield return theme.spawnRoomPrefab;
        }

        if (theme.exitRoomPrefab != null)
        {
            yield return theme.exitRoomPrefab;
        }

        foreach (var prefab in theme.enemyRoomPrefabs ?? Array.Empty<GameObject>())
        {
            if (prefab != null)
            {
                yield return prefab;
            }
        }

        foreach (var prefab in theme.specialRoomPrefabs ?? Array.Empty<GameObject>())
        {
            if (prefab != null)
            {
                yield return prefab;
            }
        }

        foreach (var prefab in theme.buffRoomPrefabs ?? Array.Empty<GameObject>())
        {
            if (prefab != null)
            {
                yield return prefab;
            }
        }

        foreach (var prefab in theme.bossRoomPrefabs ?? Array.Empty<GameObject>())
        {
            if (prefab != null)
            {
                yield return prefab;
            }
        }
    }

    private static void ValidateTransformReference(
        Transform value,
        RoomTemplate roomTemplate,
        RoomRole role,
        string fieldName,
        ValidationReport report,
        bool error)
    {
        if (value != null)
        {
            return;
        }

        var message = $"Room '{roomTemplate.gameObject.name}' ({role}) is missing {fieldName}.";
        if (error)
        {
            report.AddError(message);
        }
        else
        {
            report.AddWarning(message);
        }
    }

    private static void ValidateObjectReference(
        UnityEngine.Object value,
        RoomTemplate roomTemplate,
        RoomRole role,
        string fieldName,
        ValidationReport report,
        bool error)
    {
        if (value != null)
        {
            return;
        }

        var message = $"Room '{roomTemplate.gameObject.name}' ({role}) is missing {fieldName}.";
        if (error)
        {
            report.AddError(message);
        }
        else
        {
            report.AddWarning(message);
        }
    }

    private static string GetGeneratedPrefabName(BundleRole role, string themeName)
    {
        return string.Format(GeneratedPrefabNames[role], themeName);
    }

    private static string GetRoleLabel(BundleRole role)
    {
        switch (role)
        {
            case BundleRole.Spawn:
                return "Spawn Room";
            case BundleRole.Exit:
                return "Exit Room";
            case BundleRole.Enemy:
                return "Enemy Room";
            case BundleRole.Boss:
                return "Boss Room";
            case BundleRole.Buff:
                return "Buff Room";
            case BundleRole.Special:
                return "Special/Shop Room";
            case BundleRole.Corridor:
                return "Corridor";
            case BundleRole.BigDoor:
                return "Big Door";
            case BundleRole.SmallDoor:
                return "Small Door";
            case BundleRole.BigWall:
                return "Big Wall";
            case BundleRole.SmallWall:
                return "Small Wall";
            default:
                return role.ToString();
        }
    }

    private static bool IsRoomOrCorridor(BundleRole role)
    {
        return role == BundleRole.Spawn
            || role == BundleRole.Exit
            || role == BundleRole.Enemy
            || role == BundleRole.Boss
            || role == BundleRole.Buff
            || role == BundleRole.Special
            || role == BundleRole.Corridor;
    }

    private static RoomRole? ToRoomRole(BundleRole role)
    {
        switch (role)
        {
            case BundleRole.Spawn:
                return RoomRole.Spawn;
            case BundleRole.Exit:
                return RoomRole.Exit;
            case BundleRole.Enemy:
                return RoomRole.Enemy;
            case BundleRole.Boss:
                return RoomRole.Boss;
            case BundleRole.Buff:
                return RoomRole.Buff;
            case BundleRole.Special:
                return RoomRole.Special;
            default:
                return null;
        }
    }

    private static GameObject GetGeneratedPrefab(BundleRole role, IReadOnlyDictionary<BundleRole, GeneratedPrefabInfo> generatedAssets)
    {
        return generatedAssets.TryGetValue(role, out var info) ? info.Prefab : null;
    }

    private static GameObject[] ToSingleElementArray(BundleRole role, IReadOnlyDictionary<BundleRole, GeneratedPrefabInfo> generatedAssets)
    {
        var prefab = GetGeneratedPrefab(role, generatedAssets);
        return prefab != null ? new[] { prefab } : Array.Empty<GameObject>();
    }

    private enum BundleRole
    {
        Spawn,
        Exit,
        Enemy,
        Boss,
        Buff,
        Special,
        Corridor,
        BigDoor,
        SmallDoor,
        BigWall,
        SmallWall
    }

    private enum RoomRole
    {
        Spawn,
        Exit,
        Enemy,
        Boss,
        Buff,
        Special
    }

    private sealed class SourceBundle
    {
        public readonly Dictionary<BundleRole, GameObject> Prefabs = new Dictionary<BundleRole, GameObject>();
        public string BundleFolderPath;
        public ThemeSO InferredTheme;
    }

    private sealed class GeneratedPrefabInfo
    {
        public BundleRole Role;
        public string AssetPath;
        public GameObject Prefab;
    }

    private sealed class ValidationReport
    {
        private readonly List<string> lines = new List<string>();
        private readonly string title;

        public ValidationReport(string title)
        {
            this.title = title;
            AddInfo($"[{title}]");
        }

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }

        public void AddError(string message)
        {
            ErrorCount++;
            lines.Add("[Error] " + message);
        }

        public void AddWarning(string message)
        {
            WarningCount++;
            lines.Add("[Warn] " + message);
        }

        public void AddInfo(string message)
        {
            lines.Add("[Info] " + message);
        }

        public void LogToConsole()
        {
            var text = GetDetailedText();
            if (ErrorCount > 0)
            {
                Debug.LogError(text);
            }
            else if (WarningCount > 0)
            {
                Debug.LogWarning(text);
            }
            else
            {
                Debug.Log(text);
            }
        }

        public string GetDialogSummary()
        {
            return $"{title}\nErrors: {ErrorCount}\nWarnings: {WarningCount}\nSee Console for details.";
        }

        public string GetDetailedText()
        {
            return string.Join("\n", lines);
        }
    }
}
