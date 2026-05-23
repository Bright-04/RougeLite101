using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WeaponDefinitionMassApplyTool
{
    private const string MenuRoot = "Tools/Weapons/Migration/";
    private const string DefinitionsFolder = "Assets/ScriptableObjects/Weapons/Definitions";
    private const string LogPrefix = "[WeaponDefinitionMassApply]";
    private const float SuspiciousVisualScaleThreshold = 1.25f;

    private static readonly GroupProfile[] Profiles =
    {
        new GroupProfile(
            "Slash",
            "wooden_sword",
            WeaponAttackType.Slash,
            new[]
            {
                "wooden_sword", "void_sword", "sword_basic", "silver_sword", "plasma_greatsword", "plasma_blade",
                "obsidian_sword", "magma_sword", "iron_sword", "golden_whip", "golden_sword", "emerald_sword",
                "elven_sword", "darkness_sword", "crystal_sword", "bronze_sword", "axe_heavy", "void_axe",
                "magma_mace", "bronze_hammer", "dagger_fast", "elven_dagger"
            }),
        new GroupProfile(
            "Thrust",
            "wooden_triden",
            WeaponAttackType.Thrust,
            new[]
            {
                "wooden_triden", "wooden_spear", "void_spear", "silver_spear", "plasma_spear", "obsidian_spear",
                "obsidian_javelin", "magma_spear", "iron_spear", "iron_lance", "golden_spear", "emerald_spear",
                "elven_spear", "darkness_harpoon", "crystal_spear", "bronze_spear"
            }),
        new GroupProfile(
            "Projectile",
            "wooden_bow",
            WeaponAttackType.Projectile,
            new[]
            {
                "wooden_bow", "void_bow", "silver_bow", "plasma_bow", "obsidian_bow", "magma_bow", "iron_bow",
                "iron_crossbow", "golden_bow", "emerald_bow", "elven_bow", "darkness_bow", "crystal_bow",
                "bronze_bow", "bow_basic", "silver_pistol", "emerald_pistol", "darkness_pistol", "darkness_sniper"
            }),
        new GroupProfile(
            "MagicProjectile",
            "wooden_staff",
            WeaponAttackType.MagicProjectile,
            new[]
            {
                "wooden_staff", "void_staff", "staff_fire", "silver_staff", "plasma_staff", "obsidian_staff",
                "magma_staff", "iron_staff", "golden_staff", "emerald_staff", "elven_staff", "darkness_staff",
                "crystal_wand", "crystal_staff", "bronze_staff"
            })
    };

    [MenuItem(MenuRoot + "Dry Run Functional Propagation")]
    public static void DryRunFunctionalPropagation()
    {
        Execute(applyChanges: false);
    }

    [MenuItem(MenuRoot + "Apply Functional Propagation")]
    public static void ApplyFunctionalPropagation()
    {
        Execute(applyChanges: true);
    }

    [MenuItem(MenuRoot + "Validate Target Weapons")]
    public static void ValidateTargetWeapons()
    {
        MigrationReport report = BuildValidationOnlyReport();
        PrintReport(report, applyChanges: false, validationSummary: report.ValidationSummary);
    }

    private static void Execute(bool applyChanges)
    {
        MigrationReport report = new MigrationReport();
        List<WeaponDefinitionSO> touchedDefinitions = new List<WeaponDefinitionSO>();

        for (int profileIndex = 0; profileIndex < Profiles.Length; profileIndex++)
        {
            GroupProfile profile = Profiles[profileIndex];
            GroupReport groupReport = new GroupReport(profile.GroupName, profile.TemplateName);
            report.Groups.Add(groupReport);

            WeaponDefinitionSO template = FindWeaponDefinition(profile.TemplateName, out string templatePath);
            if (template == null)
            {
                groupReport.MissingAssets.Add(profile.TemplateName);
                report.MissingTemplates.Add(profile.TemplateName);
                continue;
            }

            for (int assetIndex = 0; assetIndex < profile.WeaponNames.Length; assetIndex++)
            {
                string weaponName = profile.WeaponNames[assetIndex];
                WeaponDefinitionSO definition = FindWeaponDefinition(weaponName, out string assetPath);
                if (definition == null)
                {
                    groupReport.MissingAssets.Add(weaponName);
                    continue;
                }

                AssetRawState rawState = AssetRawState.Load(assetPath);
                SerializedObject serializedObject = new SerializedObject(definition);
                serializedObject.Update();

                AssetChangeReport assetReport = BuildAssetReport(profile, template, definition, serializedObject, rawState);
                groupReport.AssetReports.Add(assetReport);

                if (!assetReport.WasChanged)
                {
                    continue;
                }

                groupReport.UpdatedWeaponNames.Add(definition.name);
                touchedDefinitions.Add(definition);
                if (!applyChanges)
                {
                    continue;
                }

                Undo.RecordObject(definition, "WeaponDefinition Functional Propagation");
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
            }
        }

        if (applyChanges && touchedDefinitions.Count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        List<WeaponDefinitionSO> targetDefinitions = LoadTargetDefinitions(report);
        report.ValidationSummary = WeaponDefinitionValidator.BuildSummary(targetDefinitions);
        PrintReport(report, applyChanges, report.ValidationSummary);
    }

    private static MigrationReport BuildValidationOnlyReport()
    {
        MigrationReport report = new MigrationReport();
        for (int profileIndex = 0; profileIndex < Profiles.Length; profileIndex++)
        {
            GroupProfile profile = Profiles[profileIndex];
            GroupReport groupReport = new GroupReport(profile.GroupName, profile.TemplateName);
            report.Groups.Add(groupReport);

            for (int assetIndex = 0; assetIndex < profile.WeaponNames.Length; assetIndex++)
            {
                string weaponName = profile.WeaponNames[assetIndex];
                WeaponDefinitionSO definition = FindWeaponDefinition(weaponName, out _);
                if (definition == null)
                {
                    groupReport.MissingAssets.Add(weaponName);
                    continue;
                }

                AssetChangeReport assetReport = new AssetChangeReport(profile.GroupName, definition.name);
                AddSuspiciousReferenceWarnings(profile, definition, assetReport);
                groupReport.AssetReports.Add(assetReport);
            }
        }

        List<WeaponDefinitionSO> targetDefinitions = LoadTargetDefinitions(report);
        report.ValidationSummary = WeaponDefinitionValidator.BuildSummary(targetDefinitions);
        return report;
    }

    private static AssetChangeReport BuildAssetReport(
        GroupProfile profile,
        WeaponDefinitionSO template,
        WeaponDefinitionSO definition,
        SerializedObject serializedObject,
        AssetRawState rawState)
    {
        AssetChangeReport report = new AssetChangeReport(profile.GroupName, definition.name);

        ApplyGeneralFields(profile, template, definition, serializedObject, rawState, report);

        switch (profile.AttackType)
        {
            case WeaponAttackType.Slash:
                ApplySlashFields(template, definition, serializedObject, rawState, report);
                break;
            case WeaponAttackType.Thrust:
                ApplyThrustFields(template, definition, serializedObject, rawState, report);
                break;
            case WeaponAttackType.Projectile:
                ApplyProjectileFields(template, definition, serializedObject, rawState, report);
                break;
            case WeaponAttackType.MagicProjectile:
                ApplyMagicProjectileFields(template, definition, serializedObject, rawState, report);
                break;
        }

        AddSuspiciousReferenceWarnings(profile, definition, report);
        return report;
    }

    private static void ApplyGeneralFields(
        GroupProfile profile,
        WeaponDefinitionSO template,
        WeaponDefinitionSO definition,
        SerializedObject serializedObject,
        AssetRawState rawState,
        AssetChangeReport report)
    {
        SetEnumIfMissingOrDefault(serializedObject, rawState, "archetype", (int)template.Archetype, report, (int)WeaponArchetype.Generic);
        SetObjectReferenceIfMissingOrNull(serializedObject, rawState, "alignmentPreset", template.AlignmentPreset, report);
        SetEnumIfMissingOrDefault(serializedObject, rawState, "attackType", (int)profile.AttackType, report, (int)WeaponAttackType.None);
        SetEnumIfMissing(serializedObject, rawState, "rigPointSource", (int)template.RigPointSource, report);
        SetEnumIfMissing(serializedObject, rawState, "handlingMode", (int)template.HandlingMode, report);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "rotationOffsetDegrees", template.RotationOffsetDegrees, report, value => !float.IsFinite(value));
        SetEnumIfMissing(serializedObject, rawState, "visualScaleSpace", (int)template.VisualScaleSpace, report);
        SetEnumIfMissing(serializedObject, rawState, "flipBehavior", (int)template.FlipBehavior, report);
        SetObjectReferenceIfMissingOrNull(serializedObject, rawState, "weaponPrefab", template.WeaponPrefab, report);

        // Legacy fallback offsets are only filled when the asset predates these fields and the template
        // carries a reusable value for the archetype.
        SetVector3IfMissing(serializedObject, rawState, "gripPointOffset", template.GripPointOffset, report);
        SetVector3IfMissing(serializedObject, rawState, "aimPointOffset", template.AimPointOffset, report);
        SetVector3IfMissing(serializedObject, rawState, "localRotationOffset", template.LocalRotationOffset, report);
        SetVector3IfMissing(serializedObject, rawState, "localPositionOffset", template.LocalPositionOffset, report);
        SetVector3IfMissing(serializedObject, rawState, "projectileSpawnPointOffset", template.ProjectileSpawnPointOffset, report);
        SetVector3IfMissing(serializedObject, rawState, "slashVfxOffset", template.SlashVfxOffset, report);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "visualScale", template.VisualScale, report, value => value <= 0f || !float.IsFinite(value));

        int resolvedDamage = definition.BaseDamage > 0 ? definition.BaseDamage : template.Damage;
        SetIntIfMissingOrInvalid(serializedObject, rawState, "damage", resolvedDamage, report, value => value <= 0);
    }

    private static void ApplySlashFields(
        WeaponDefinitionSO template,
        WeaponDefinitionSO definition,
        SerializedObject serializedObject,
        AssetRawState rawState,
        AssetChangeReport report)
    {
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "cooldown", template.Cooldown, report, value => value <= 0f);
        SetFloatIfMissing(serializedObject, rawState, "knockback", template.Knockback, report);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "anticipationDuration", template.AnticipationDuration, report, value => value < 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "activeDuration", template.ActiveDuration, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "recoveryDuration", template.RecoveryDuration, report, value => value < 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "slashArcDegrees", template.SlashArcDegrees, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "slashRange", template.SlashRange, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "hitboxDistance", template.HitboxDistance, report, value => value <= 0f);
        SetVector3IfMissingOrInvalid(serializedObject, rawState, "hitboxScale", template.HitboxScale, report, IsZeroVector);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "slashVisualExtraAnticipationDegrees", template.SlashVisualExtraAnticipationDegrees, report, value => value < 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "slashVisualExtraFollowThroughDegrees", template.SlashVisualExtraFollowThroughDegrees, report, value => value < 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "slashVfxLifetime", template.SlashVfxLifetime, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "slashVfxStartScaleMultiplier", template.SlashVfxStartScaleMultiplier, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "slashVfxEndScaleMultiplier", template.SlashVfxEndScaleMultiplier, report, value => value <= 0f);
        SetBoolIfMissing(serializedObject, rawState, "slashVfxFadeOut", template.SlashVfxFadeOut, report);
        SetFloatIfMissing(serializedObject, rawState, "meleeVisualPulseScaleAmount", template.MeleeVisualPulseScaleAmount, report);
        SetFloatIfMissing(serializedObject, rawState, "meleeVisualPulseBlend", template.MeleeVisualPulseBlend, report);
    }

    private static void ApplyThrustFields(
        WeaponDefinitionSO template,
        WeaponDefinitionSO definition,
        SerializedObject serializedObject,
        AssetRawState rawState,
        AssetChangeReport report)
    {
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "cooldown", template.Cooldown, report, value => value <= 0f);
        SetFloatIfMissing(serializedObject, rawState, "knockback", template.Knockback, report);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "anticipationDuration", template.AnticipationDuration, report, value => value < 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "activeDuration", template.ActiveDuration, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "recoveryDuration", template.RecoveryDuration, report, value => value < 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "thrustDistance", template.ThrustDistance, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "thrustWidth", template.ThrustWidth, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "hitboxDistance", template.HitboxDistance, report, value => value <= 0f);
        SetVector3IfMissingOrInvalid(serializedObject, rawState, "hitboxScale", template.HitboxScale, report, IsZeroVector);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "thrustVisualPullbackFactor", template.ThrustVisualPullbackFactor, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "thrustVisualLungeFactor", template.ThrustVisualLungeFactor, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "thrustVisualStretchFactor", template.ThrustVisualStretchFactor, report, value => value <= 0f);
        SetFloatIfMissing(serializedObject, rawState, "meleeVisualPulseScaleAmount", template.MeleeVisualPulseScaleAmount, report);
        SetFloatIfMissing(serializedObject, rawState, "meleeVisualPulseBlend", template.MeleeVisualPulseBlend, report);
    }

    private static void ApplyProjectileFields(
        WeaponDefinitionSO template,
        WeaponDefinitionSO definition,
        SerializedObject serializedObject,
        AssetRawState rawState,
        AssetChangeReport report)
    {
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "cooldown", template.Cooldown, report, value => value <= 0f);
        SetFloatIfMissing(serializedObject, rawState, "knockback", template.Knockback, report);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "range", definition.Range > 0f ? definition.Range : template.Range, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "projectileSpeed", template.ProjectileSpeed, report, value => value <= 0f);
        SetIntIfMissingOrInvalid(serializedObject, rawState, "projectileCount", Mathf.Max(1, definition.ProjectileCount), report, value => value <= 0);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "spreadAngle", template.SpreadAngle, report, value => value < 0f);
        SetObjectReferenceIfMissingOrNull(serializedObject, rawState, "projectilePrefab", template.ProjectilePrefab, report);
    }

    private static void ApplyMagicProjectileFields(
        WeaponDefinitionSO template,
        WeaponDefinitionSO definition,
        SerializedObject serializedObject,
        AssetRawState rawState,
        AssetChangeReport report)
    {
        ApplyProjectileFields(template, definition, serializedObject, rawState, report);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "anticipationDuration", template.AnticipationDuration, report, value => value < 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "activeDuration", template.ActiveDuration, report, value => value <= 0f);
        SetFloatIfMissingOrInvalid(serializedObject, rawState, "recoveryDuration", template.RecoveryDuration, report, value => value < 0f);
    }

    private static void AddSuspiciousReferenceWarnings(GroupProfile profile, WeaponDefinitionSO definition, AssetChangeReport report)
    {
        if (definition == null)
        {
            return;
        }

        if (definition.ItemImage == null)
        {
            report.Warnings.Add("Missing ItemImage sprite.");
        }

        if (definition.AlignmentPreset == null)
        {
            report.Warnings.Add("Missing alignmentPreset.");
        }

        if (definition.WeaponPrefab == null)
        {
            report.Warnings.Add("Missing weaponPrefab.");
        }

        if ((profile.AttackType == WeaponAttackType.Projectile || profile.AttackType == WeaponAttackType.MagicProjectile)
            && definition.ProjectilePrefab == null)
        {
            report.Warnings.Add("Missing projectilePrefab for ranged weapon.");
        }

        if (definition.RigPointSource == WeaponRigPointSourceMode.UsePresetRig
            && definition.VisualScale > 0f
            && definition.VisualScale <= SuspiciousVisualScaleThreshold)
        {
            report.Warnings.Add($"VisualScale={definition.VisualScale:0.###} is low for UsePresetRig and may need manual review.");
        }
    }

    private static List<WeaponDefinitionSO> LoadTargetDefinitions(MigrationReport report)
    {
        HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<WeaponDefinitionSO> definitions = new List<WeaponDefinitionSO>();

        for (int profileIndex = 0; profileIndex < Profiles.Length; profileIndex++)
        {
            GroupProfile profile = Profiles[profileIndex];
            for (int assetIndex = 0; assetIndex < profile.WeaponNames.Length; assetIndex++)
            {
                string name = profile.WeaponNames[assetIndex];
                if (!seenNames.Add(name))
                {
                    continue;
                }

                WeaponDefinitionSO definition = FindWeaponDefinition(name, out _);
                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }
        }

        return definitions;
    }

    private static WeaponDefinitionSO FindWeaponDefinition(string assetName, out string assetPath)
    {
        assetPath = Path.Combine(DefinitionsFolder, assetName + ".asset").Replace("\\", "/");
        WeaponDefinitionSO direct = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(assetPath);
        if (direct != null)
        {
            return direct;
        }

        string[] guids = AssetDatabase.FindAssets($"{assetName} t:WeaponDefinitionSO", new[] { DefinitionsFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string candidatePath = AssetDatabase.GUIDToAssetPath(guids[i]);
            WeaponDefinitionSO candidate = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(candidatePath);
            if (candidate != null && string.Equals(candidate.name, assetName, StringComparison.OrdinalIgnoreCase))
            {
                assetPath = candidatePath;
                return candidate;
            }
        }

        assetPath = null;
        return null;
    }

    private static void SetEnumIfMissingOrDefault(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        int targetValue,
        AssetChangeReport report,
        int defaultValue)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            report.Warnings.Add($"Property '{propertyName}' was not found.");
            return;
        }

        bool shouldSet = !rawState.HasField(propertyName) || property.enumValueIndex == defaultValue;
        if (!shouldSet || property.enumValueIndex == targetValue)
        {
            return;
        }

        report.RecordChange(propertyName, property.enumDisplayNames[property.enumValueIndex], property.enumDisplayNames[targetValue], !rawState.HasField(propertyName));
        property.enumValueIndex = targetValue;
    }

    private static void SetEnumIfMissing(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        int targetValue,
        AssetChangeReport report)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            report.Warnings.Add($"Property '{propertyName}' was not found.");
            return;
        }

        if (rawState.HasField(propertyName) || property.enumValueIndex == targetValue)
        {
            return;
        }

        report.RecordChange(propertyName, property.enumDisplayNames[property.enumValueIndex], property.enumDisplayNames[targetValue], true);
        property.enumValueIndex = targetValue;
    }

    private static void SetObjectReferenceIfMissingOrNull(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        UnityEngine.Object targetValue,
        AssetChangeReport report)
    {
        if (targetValue == null)
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            report.Warnings.Add($"Property '{propertyName}' was not found.");
            return;
        }

        if (property.objectReferenceValue != null && rawState.HasField(propertyName))
        {
            return;
        }

        string before = property.objectReferenceValue != null ? property.objectReferenceValue.name : "null";
        if (property.objectReferenceValue == targetValue)
        {
            return;
        }

        report.RecordChange(propertyName, before, targetValue.name, !rawState.HasField(propertyName));
        property.objectReferenceValue = targetValue;
    }

    private static void SetFloatIfMissing(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        float targetValue,
        AssetChangeReport report)
    {
        SetFloatIfMissingOrInvalid(serializedObject, rawState, propertyName, targetValue, report, _ => false);
    }

    private static void SetFloatIfMissingOrInvalid(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        float targetValue,
        AssetChangeReport report,
        Func<float, bool> invalidPredicate)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            report.Warnings.Add($"Property '{propertyName}' was not found.");
            return;
        }

        bool fieldMissing = !rawState.HasField(propertyName);
        bool invalid = invalidPredicate(property.floatValue);
        if (!fieldMissing && !invalid)
        {
            return;
        }

        if (Mathf.Approximately(property.floatValue, targetValue))
        {
            return;
        }

        report.RecordChange(propertyName, property.floatValue.ToString("0.###"), targetValue.ToString("0.###"), fieldMissing);
        property.floatValue = targetValue;
    }

    private static void SetIntIfMissingOrInvalid(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        int targetValue,
        AssetChangeReport report,
        Func<int, bool> invalidPredicate)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            report.Warnings.Add($"Property '{propertyName}' was not found.");
            return;
        }

        bool fieldMissing = !rawState.HasField(propertyName);
        bool invalid = invalidPredicate(property.intValue);
        if (!fieldMissing && !invalid)
        {
            return;
        }

        if (property.intValue == targetValue)
        {
            return;
        }

        report.RecordChange(propertyName, property.intValue.ToString(), targetValue.ToString(), fieldMissing);
        property.intValue = targetValue;
    }

    private static void SetVector3IfMissing(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        Vector3 targetValue,
        AssetChangeReport report)
    {
        SetVector3IfMissingOrInvalid(serializedObject, rawState, propertyName, targetValue, report, _ => false);
    }

    private static void SetVector3IfMissingOrInvalid(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        Vector3 targetValue,
        AssetChangeReport report,
        Func<Vector3, bool> invalidPredicate)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            report.Warnings.Add($"Property '{propertyName}' was not found.");
            return;
        }

        bool fieldMissing = !rawState.HasField(propertyName);
        Vector3 current = property.vector3Value;
        bool invalid = invalidPredicate(current);
        if (!fieldMissing && !invalid)
        {
            return;
        }

        if (current == targetValue)
        {
            return;
        }

        report.RecordChange(propertyName, FormatVector(current), FormatVector(targetValue), fieldMissing);
        property.vector3Value = targetValue;
    }

    private static void SetBoolIfMissing(
        SerializedObject serializedObject,
        AssetRawState rawState,
        string propertyName,
        bool targetValue,
        AssetChangeReport report)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            report.Warnings.Add($"Property '{propertyName}' was not found.");
            return;
        }

        if (rawState.HasField(propertyName) || property.boolValue == targetValue)
        {
            return;
        }

        report.RecordChange(propertyName, property.boolValue.ToString(), targetValue.ToString(), true);
        property.boolValue = targetValue;
    }

    private static bool IsZeroVector(Vector3 value)
    {
        return Mathf.Abs(value.x) < 0.0001f
            && Mathf.Abs(value.y) < 0.0001f
            && Mathf.Abs(value.z) < 0.0001f;
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";
    }

    private static void PrintReport(MigrationReport report, bool applyChanges, WeaponDefinitionValidator.ValidationSummary validationSummary)
    {
        string mode = applyChanges ? "APPLY" : "DRY-RUN";
        Debug.Log($"{LogPrefix} {mode} report starting.");

        for (int groupIndex = 0; groupIndex < report.Groups.Count; groupIndex++)
        {
            GroupReport group = report.Groups[groupIndex];
            Debug.Log($"{LogPrefix} Group={group.GroupName} template={group.TemplateName} updated={group.UpdatedWeaponNames.Count} missing={group.MissingAssets.Count}");

            for (int assetIndex = 0; assetIndex < group.AssetReports.Count; assetIndex++)
            {
                AssetChangeReport assetReport = group.AssetReports[assetIndex];
                if (assetReport.Changes.Count == 0 && assetReport.Warnings.Count == 0)
                {
                    continue;
                }

                string changedFields = assetReport.Changes.Count > 0
                    ? string.Join("; ", assetReport.Changes)
                    : "none";
                string warnings = assetReport.Warnings.Count > 0
                    ? string.Join(" | ", assetReport.Warnings)
                    : "none";
                Debug.Log($"{LogPrefix} {group.GroupName}/{assetReport.AssetName} changes={changedFields} warnings={warnings}");
            }

            if (group.MissingAssets.Count > 0)
            {
                Debug.LogWarning($"{LogPrefix} Group={group.GroupName} missing assets: {string.Join(", ", group.MissingAssets)}");
            }
        }

        if (report.MissingTemplates.Count > 0)
        {
            Debug.LogError($"{LogPrefix} Missing templates: {string.Join(", ", report.MissingTemplates)}");
        }

        if (validationSummary != null)
        {
            Debug.Log($"{LogPrefix} Validation summary: checked={validationSummary.ValidatedCount} errors={validationSummary.Errors.Count} warnings={validationSummary.Warnings.Count} infos={validationSummary.Infos.Count}");

            for (int i = 0; i < validationSummary.Errors.Count; i++)
            {
                Debug.LogError($"{LogPrefix} Validation error: {validationSummary.Errors[i]}");
            }

            for (int i = 0; i < validationSummary.Warnings.Count; i++)
            {
                Debug.LogWarning($"{LogPrefix} Validation warning: {validationSummary.Warnings[i]}");
            }
        }
    }

    private sealed class GroupProfile
    {
        public readonly string GroupName;
        public readonly string TemplateName;
        public readonly WeaponAttackType AttackType;
        public readonly string[] WeaponNames;

        public GroupProfile(string groupName, string templateName, WeaponAttackType attackType, string[] weaponNames)
        {
            GroupName = groupName;
            TemplateName = templateName;
            AttackType = attackType;
            WeaponNames = weaponNames;
        }
    }

    private sealed class MigrationReport
    {
        public readonly List<GroupReport> Groups = new List<GroupReport>();
        public readonly List<string> MissingTemplates = new List<string>();
        public WeaponDefinitionValidator.ValidationSummary ValidationSummary;
    }

    private sealed class GroupReport
    {
        public readonly string GroupName;
        public readonly string TemplateName;
        public readonly List<string> UpdatedWeaponNames = new List<string>();
        public readonly List<string> MissingAssets = new List<string>();
        public readonly List<AssetChangeReport> AssetReports = new List<AssetChangeReport>();

        public GroupReport(string groupName, string templateName)
        {
            GroupName = groupName;
            TemplateName = templateName;
        }
    }

    private sealed class AssetChangeReport
    {
        public readonly string GroupName;
        public readonly string AssetName;
        public readonly List<string> Changes = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public bool WasChanged => Changes.Count > 0;

        public AssetChangeReport(string groupName, string assetName)
        {
            GroupName = groupName;
            AssetName = assetName;
        }

        public void RecordChange(string propertyName, string beforeValue, string afterValue, bool fieldWasMissing)
        {
            string reason = fieldWasMissing ? "missing" : "invalid";
            Changes.Add($"{propertyName}: {beforeValue} -> {afterValue} ({reason})");
        }
    }

    private sealed class AssetRawState
    {
        private readonly string rawText;

        private AssetRawState(string text)
        {
            rawText = text ?? string.Empty;
        }

        public static AssetRawState Load(string assetPath)
        {
            return new AssetRawState(File.Exists(assetPath) ? File.ReadAllText(assetPath) : string.Empty);
        }

        public bool HasField(string propertyName)
        {
            return rawText.Contains("\n  " + propertyName + ":", StringComparison.Ordinal);
        }
    }
}
