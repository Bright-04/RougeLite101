using UnityEditor;
using UnityEngine;

public static class WeaponRuntimeValidationMenu
{
    private const float SafeHeightRatioTarget = 0.65f;

    private const string WoodenBowPath = "Assets/ScriptableObjects/Weapons/Definitions/wooden_bow.asset";
    private const string WoodenSwordPath = "Assets/ScriptableObjects/Weapons/Definitions/wooden_sword.asset";
    private const string WoodenStaffPath = "Assets/ScriptableObjects/Weapons/Definitions/wooden_staff.asset";
    private const string WoodenTridenPath = "Assets/ScriptableObjects/Weapons/Definitions/wooden_triden.asset";

    [MenuItem("Tools/Weapons/Validation/Equip Wooden Bow In Play Mode")]
    private static void EquipWoodenBowInPlayMode() => EquipWeaponInPlayMode(WoodenBowPath);

    [MenuItem("Tools/Weapons/Validation/Equip Wooden Sword In Play Mode")]
    private static void EquipWoodenSwordInPlayMode() => EquipWeaponInPlayMode(WoodenSwordPath);

    [MenuItem("Tools/Weapons/Validation/Equip Wooden Staff In Play Mode")]
    private static void EquipWoodenStaffInPlayMode() => EquipWeaponInPlayMode(WoodenStaffPath);

    [MenuItem("Tools/Weapons/Validation/Equip Wooden Triden In Play Mode")]
    private static void EquipWoodenTridenInPlayMode() => EquipWeaponInPlayMode(WoodenTridenPath);

    [MenuItem("Tools/Weapons/Validation/Log Active Weapon Diagnostics")]
    private static void LogActiveWeaponDiagnostics()
    {
        WeaponController controller = Object.FindAnyObjectByType<WeaponController>();
        EquipmentManager equipmentManager = Object.FindAnyObjectByType<EquipmentManager>();
        if (controller == null)
        {
            Debug.LogError($"WeaponRuntimeValidationMenu: No {nameof(WeaponController)} found in play mode.");
            return;
        }

        WeaponAlignmentPose pose = controller.CurrentPose;
        float gripDistance = Vector3.Distance(pose.WeaponAnchorPosition, pose.GripPoint);
        float actualGripDistance = controller.CurrentActualRenderedGripDistance;
        SpriteRenderer displayedRenderer = controller.CurrentDisplayedWeaponRenderer;
        WeaponRenderBoundsReport boundsReport = WeaponRenderBoundsUtility.CalculateRenderedBoundsRatio(
            displayedRenderer,
            controller.transform,
            WeaponRenderBoundsMode.BodyRendererOnly);
        float safeResetVisualScale = CalculateSafeRuntimeVisualScale(
            equipmentManager != null ? equipmentManager.GetWeaponDefinition(equipmentManager.GetActiveSlot()) : null,
            boundsReport);
        string weaponId = equipmentManager != null
            ? equipmentManager.GetWeaponDefinition(equipmentManager.GetActiveSlot())?.WeaponId ?? "None"
            : "Unknown";
        Debug.Log(
            $"[WeaponRuntimeDiagnostics] " +
            $"weaponId={weaponId} " +
            $"rigMode={pose.RigSourceMode} " +
            $"H={FormatVector(pose.WeaponAnchorPosition)} " +
            $"G={FormatVector(pose.GripPoint)} " +
            $"distance={gripDistance:0.###} " +
            $"actualGrip={FormatVector(controller.CurrentActualRenderedGripPoint)} " +
            $"actualDistance={actualGripDistance:0.###} " +
            $"displayedRenderer={(displayedRenderer != null ? WeaponRenderBoundsUtility.GetTransformPath(displayedRenderer.transform) : "None")} " +
            $"rendererBoundsCenter={(displayedRenderer != null ? FormatVector(displayedRenderer.bounds.center) : "n/a")} " +
            $"rendererBoundsSize={(displayedRenderer != null ? FormatVector(displayedRenderer.bounds.size) : "n/a")} " +
            $"heightRatio={(boundsReport.IsValid ? boundsReport.Ratio.y.ToString("0.###") : "n/a")} " +
            $"safeResetVisualScale={(float.IsFinite(safeResetVisualScale) ? safeResetVisualScale.ToString("0.###") : "n/a")} " +
            $"P={FormatVector(pose.ProjectileSpawnPoint)} " +
            $"projectileSource={pose.ProjectileSource}");
    }

    [MenuItem("Tools/Weapons/Validation/Log Active Weapon Hierarchy")]
    private static void LogActiveWeaponHierarchy()
    {
        WeaponController controller = Object.FindAnyObjectByType<WeaponController>();
        if (controller == null)
        {
            Debug.LogError($"WeaponRuntimeValidationMenu: No {nameof(WeaponController)} found in play mode.");
            return;
        }

        Debug.Log(controller.BuildCurrentWeaponHierarchyReport());
    }

    [MenuItem("Tools/Weapons/Validation/Log Test Weapon Scale Recommendations")]
    private static void LogTestWeaponScaleRecommendations()
    {
        LogScaleRecommendation(WoodenSwordPath);
        LogScaleRecommendation(WoodenTridenPath);
        LogScaleRecommendation(WoodenStaffPath);
        LogScaleRecommendation(WoodenBowPath);
    }

    [MenuItem("Tools/Weapons/Validation/Use Active Weapon")]
    private static void UseActiveWeapon()
    {
        EquipmentManager equipmentManager = Object.FindAnyObjectByType<EquipmentManager>();
        if (equipmentManager == null)
        {
            Debug.LogError($"WeaponRuntimeValidationMenu: No {nameof(EquipmentManager)} found in play mode.");
            return;
        }

        Weapon activeWeapon = GetActiveWeapon(equipmentManager);
        if (activeWeapon == null)
        {
            Debug.LogError("WeaponRuntimeValidationMenu: No active weapon instance found.");
            return;
        }

        activeWeapon.Use();
        EditorApplication.delayCall += LogActiveWeaponDiagnostics;
    }

    [MenuItem("Tools/Weapons/Validation/Equip Wooden Bow In Play Mode", true)]
    [MenuItem("Tools/Weapons/Validation/Equip Wooden Sword In Play Mode", true)]
    [MenuItem("Tools/Weapons/Validation/Equip Wooden Staff In Play Mode", true)]
    [MenuItem("Tools/Weapons/Validation/Equip Wooden Triden In Play Mode", true)]
    [MenuItem("Tools/Weapons/Validation/Log Active Weapon Diagnostics", true)]
    [MenuItem("Tools/Weapons/Validation/Log Active Weapon Hierarchy", true)]
    [MenuItem("Tools/Weapons/Validation/Use Active Weapon", true)]
    private static bool ValidatePlayModeOnly()
    {
        return EditorApplication.isPlaying;
    }

    [MenuItem("Tools/Weapons/Validation/Log Test Weapon Scale Recommendations", true)]
    private static bool ValidateEditorOrPlayMode()
    {
        return true;
    }

    private static void EquipWeaponInPlayMode(string assetPath)
    {
        WeaponDefinitionSO definition = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(assetPath);
        if (definition == null)
        {
            Debug.LogError($"WeaponRuntimeValidationMenu: Could not load weapon definition at '{assetPath}'.");
            return;
        }

        EquipmentManager equipmentManager = Object.FindAnyObjectByType<EquipmentManager>();
        if (equipmentManager == null)
        {
            Debug.LogError($"WeaponRuntimeValidationMenu: No {nameof(EquipmentManager)} found in play mode.");
            return;
        }

        equipmentManager.ReplaceWeaponAndActivate(EquipmentManager.WeaponSlot.Main, definition);
        Debug.Log($"WeaponRuntimeValidationMenu: Equipped '{definition.name}' in play mode for validation.");
        EditorApplication.delayCall += LogActiveWeaponDiagnostics;
    }

    private static Weapon GetActiveWeapon(EquipmentManager equipmentManager)
    {
        var getter = typeof(EquipmentManager).GetMethod(
            "GetWeaponInstance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (getter == null)
        {
            return null;
        }

        return getter.Invoke(equipmentManager, new object[] { equipmentManager.GetActiveSlot() }) as Weapon;
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";
    }

    private static void LogScaleRecommendation(string assetPath)
    {
        WeaponDefinitionSO definition = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(assetPath);
        if (definition == null)
        {
            Debug.LogError($"WeaponRuntimeValidationMenu: Could not load weapon definition at '{assetPath}'.");
            return;
        }

        bool hasVisualTransform = definition.TryGetPrefabPrimaryVisualTransformSummary(
            out string rendererPath,
            out Vector3 localPosition,
            out Vector3 localEulerAngles,
            out Vector3 cumulativeScale);

        Debug.Log(
            $"[WeaponScaleRecommendation] weaponId={definition.WeaponId} " +
            $"currentVisualScale={definition.VisualScale:0.###} " +
            $"visualScaleSpace={definition.VisualScaleSpace} " +
            $"recommendedNeutralVisualScale={definition.GetNeutralPresetRigRecommendedVisualScale():0.###} " +
            $"prefabRendererPath={(hasVisualTransform ? rendererPath : "n/a")} " +
            $"prefabRendererLocalPosition={(hasVisualTransform ? FormatVector(localPosition) : "n/a")} " +
            $"prefabRendererLocalRotation={(hasVisualTransform ? FormatVector(localEulerAngles) : "n/a")} " +
            $"prefabRendererCumulativeScale={(hasVisualTransform ? FormatVector(cumulativeScale) : "n/a")}");
    }

    private static float CalculateSafeRuntimeVisualScale(WeaponDefinitionSO definition, WeaponRenderBoundsReport boundsReport)
    {
        if (definition == null || !boundsReport.IsValid || boundsReport.Ratio.y <= 0.0001f)
        {
            return float.NaN;
        }

        return definition.VisualScale * (SafeHeightRatioTarget / boundsReport.Ratio.y);
    }
}
