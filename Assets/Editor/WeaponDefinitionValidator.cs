using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WeaponDefinitionValidator
{
    [MenuItem("Tools/Weapons/Validate Weapon Definitions")]
    public static void ValidateAllWeaponDefinitions()
    {
        WeaponDefinitionSO[] definitions = LoadAllDefinitions();
        ValidationSummary summary = BuildSummary(definitions);
        LogAndDisplaySummary(summary, "Weapon Validation");
    }

    [MenuItem("Tools/Weapons/Validate Selected Weapon Definitions")]
    public static void ValidateSelectedWeaponDefinitions()
    {
        WeaponDefinitionSO[] definitions = Selection.GetFiltered<WeaponDefinitionSO>(SelectionMode.Assets);
        ValidationSummary summary = BuildSummary(definitions);
        LogAndDisplaySummary(summary, "Selected Weapon Validation");
    }

    [MenuItem("Tools/Weapons/Clear Legacy Offsets On Selected UsePresetRig Weapons")]
    public static void ClearLegacyOffsetsOnSelectedUsePresetRigWeapons()
    {
        WeaponDefinitionSO[] definitions = Selection.GetFiltered<WeaponDefinitionSO>(SelectionMode.Assets);
        int changedCount = 0;

        for (int i = 0; i < definitions.Length; i++)
        {
            WeaponDefinitionSO definition = definitions[i];
            if (definition == null || definition.RigPointSource != WeaponRigPointSourceMode.UsePresetRig)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(definition);
            bool changed = false;
            changed |= SetVector3IfNeeded(serializedObject.FindProperty("aimPointOffset"), Vector3.zero);
            changed |= SetVector3IfNeeded(serializedObject.FindProperty("localPositionOffset"), Vector3.zero);
            changed |= SetVector3IfNeeded(serializedObject.FindProperty("localRotationOffset"), Vector3.zero);
            changed |= SetVector3IfNeeded(serializedObject.FindProperty("projectileSpawnPointOffset"), Vector3.zero);

            if (!changed)
            {
                continue;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
            changedCount++;
        }

        if (changedCount > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"Clear Legacy Offsets: updated {changedCount} selected UsePresetRig weapon definition(s).");
    }

    public static ValidationSummary BuildSummary(IEnumerable<WeaponDefinitionSO> definitions)
    {
        ValidationSummary summary = new ValidationSummary();
        foreach (WeaponDefinitionSO definition in definitions)
        {
            if (definition == null)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(definition);
            summary.ValidatedCount++;
            ValidateDefinition(definition, path, summary.Errors, summary.Warnings, summary.Infos);
        }

        return summary;
    }

    private static void ValidateDefinition(
        WeaponDefinitionSO definition,
        string path,
        List<string> errors,
        List<string> warnings,
        List<string> infos)
    {
        string label = $"{definition.name} ({path})";

        if (definition.ItemImage == null)
        {
            errors.Add($"{label}: Missing weapon sprite.");
        }
        else
        {
            Bounds spriteBounds = definition.ItemImage.bounds;
            if (Mathf.Abs(spriteBounds.size.x) < 0.0001f || Mathf.Abs(spriteBounds.size.y) < 0.0001f)
            {
                errors.Add($"{label}: Weapon sprite has invalid bounds and cannot be used for normalized preset placement.");
            }
        }

        if (definition.WeaponPrefab == null)
        {
            errors.Add($"{label}: Missing weapon visual prefab.");
        }

        WeaponArchetype archetype = definition.ResolvedArchetype;
        bool needsProjectile = definition.WeaponType == WeaponType.Projectile;
        bool needsMeleeRig = definition.WeaponType == WeaponType.Melee;
        bool usesPresetRig = definition.RigPointSource == WeaponRigPointSourceMode.UsePresetRig;
        bool projectileAttackType = definition.IsProjectileAttack;
        bool hasExplicitArchetype = definition.Archetype != WeaponArchetype.Generic;
        bool archetypeWasInferred = definition.Archetype == WeaponArchetype.Generic && archetype != WeaponArchetype.Generic;

        if (!hasExplicitArchetype && archetypeWasInferred)
        {
            warnings.Add($"{label}: Missing explicit archetype. Runtime currently infers '{archetype}' from EquipmentClass.");
        }
        else if (!hasExplicitArchetype && archetype == WeaponArchetype.Generic)
        {
            infos.Add($"{label}: Uses Generic archetype. Assign a more specific archetype if this weapon should use a standard preset.");
        }

        if (definition.AlignmentPreset == null)
        {
            warnings.Add(usesPresetRig
                ? $"{label}: Missing alignment preset. Normal UsePresetRig runtime needs preset points."
                : $"{label}: Missing alignment preset. Rig or legacy offsets must carry alignment.");
        }
        else if (definition.AlignmentPreset.Archetype != WeaponArchetype.Generic && definition.AlignmentPreset.Archetype != archetype)
        {
            warnings.Add($"{label}: Alignment preset archetype '{definition.AlignmentPreset.Archetype}' does not match resolved archetype '{archetype}'.");
        }

        if (usesPresetRig && definition.AttackType == WeaponAttackType.None)
        {
            warnings.Add($"{label}: UsePresetRig weapon is missing attackType. Normal workflow should explicitly use Slash, Thrust, Projectile, or MagicProjectile.");
        }

        if (usesPresetRig && definition.UsesLegacyAimPointOffset)
        {
            warnings.Add($"{label}: UsePresetRig weapon has non-zero aimPointOffset, but normal runtime ignores it.");
        }

        if (usesPresetRig && definition.UsesLegacyLocalPositionOffset)
        {
            warnings.Add($"{label}: UsePresetRig weapon has non-zero localPositionOffset, but normal runtime ignores it.");
        }

        if (usesPresetRig && definition.UsesLegacyProjectileSpawnOffset)
        {
            warnings.Add($"{label}: UsePresetRig weapon has non-zero ProjectileSpawnPointOffset, but normal runtime ignores it and uses preset P.");
        }

        if (definition.WeaponPrefab == null)
        {
            return;
        }

        WeaponRig rig = definition.WeaponPrefab.GetComponentInChildren<WeaponRig>(true);
        bool hasPresetPointSupport = definition.AlignmentPreset != null
            && definition.ItemImage != null
            && definition.AlignmentPreset.TryBuildPoints(definition.ItemImage, out _);
        bool hasProjectileSupport = false;
        bool hasSlashSupport = false;

        if (rig == null)
        {
            if (!usesPresetRig)
            {
                warnings.Add($"{label}: Missing WeaponRig on prefab '{definition.WeaponPrefab.name}'. Runtime will fall back to preset/legacy offsets.");
            }
        }
        else
        {
            if (!usesPresetRig && !rig.HasRequiredPointsFor(definition))
            {
                warnings.Add($"{label}: WeaponRig on prefab '{definition.WeaponPrefab.name}' is missing required points for archetype '{archetype}'.");
            }

            hasProjectileSupport = rig.ProjectileSpawnPoint != null;
            hasSlashSupport = rig.SlashOrigin != null && rig.SlashArcStart != null && rig.SlashArcEnd != null;
        }

        if (needsProjectile && definition.UsesLegacyProjectileSpawnOffset)
        {
            warnings.Add($"{label}: Uses legacy ProjectileSpawnPointOffset fallback. Prefer WeaponRig.ProjectileSpawnPoint.");
        }

        if (usesPresetRig && projectileAttackType && !hasPresetPointSupport)
        {
            warnings.Add($"{label}: UsePresetRig projectile weapon is missing preset point support for ProjectileSpawnPoint and will not use prefab shoot points.");
        }

        if (needsProjectile && !hasProjectileSupport && !hasPresetPointSupport && !definition.UsesLegacyProjectileSpawnOffset)
        {
            errors.Add($"{label}: Ranged weapon has no ProjectileSpawnPoint support from rig, preset, or legacy projectile offset.");
        }

        if (needsMeleeRig)
        {
            MeleeWeapon meleeWeapon = definition.WeaponPrefab.GetComponent<MeleeWeapon>();
            if (meleeWeapon == null)
            {
                errors.Add($"{label}: Melee definition is assigned to prefab '{definition.WeaponPrefab.name}' without MeleeWeapon component.");
            }
            else if (!PrefabHasObjectReference(meleeWeapon, "weaponCollider"))
            {
                errors.Add($"{label}: Melee prefab '{definition.WeaponPrefab.name}' does not serialize a weaponCollider reference.");
            }

            if (!hasSlashSupport && !hasPresetPointSupport && definition.SlashVfxOffset == Vector3.zero)
            {
                warnings.Add($"{label}: Melee weapon has no explicit slash pose support from rig or preset and is relying on generic fallback slash arc math.");
            }
        }

        if (definition.WeaponType == WeaponType.Projectile)
        {
            ProjectileWeapon projectileWeapon = definition.WeaponPrefab.GetComponent<ProjectileWeapon>();
            if (projectileWeapon == null)
            {
                warnings.Add($"{label}: Projectile definition is assigned to prefab '{definition.WeaponPrefab.name}' without ProjectileWeapon component.");
            }
            else if (usesPresetRig
                && projectileAttackType
                && (PrefabHasObjectReference(projectileWeapon, "shootPoint") || PrefabHasObjectReference(projectileWeapon, "defaultShootPoint")))
            {
                warnings.Add($"{label}: UsePresetRig projectile weapon has prefab shoot point references, but normal runtime ignores them and uses preset P.");
            }
        }

        if (definition.AlignmentPreset == null && rig == null)
        {
            infos.Add($"{label}: Preview/runtime pose mismatch risk is elevated because alignment depends on legacy serialized offsets only.");
        }
    }

    private static bool PrefabHasObjectReference(Object target, string propertyName)
    {
        if (target == null)
        {
            return false;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null && property.objectReferenceValue != null;
    }

    private static WeaponDefinitionSO[] LoadAllDefinitions()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponDefinitionSO", new[] { "Assets" });
        List<WeaponDefinitionSO> definitions = new List<WeaponDefinitionSO>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            WeaponDefinitionSO definition = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (definition != null)
            {
                definitions.Add(definition);
            }
        }

        return definitions.ToArray();
    }

    private static void LogAndDisplaySummary(ValidationSummary summary, string dialogTitle)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Validated {summary.ValidatedCount} WeaponDefinitionSO assets.");
        builder.AppendLine($"Errors: {summary.Errors.Count}");
        builder.AppendLine($"Warnings: {summary.Warnings.Count}");
        builder.AppendLine($"Info: {summary.Infos.Count}");

        AppendSection(builder, "Errors", summary.Errors);
        AppendSection(builder, "Warnings", summary.Warnings);
        AppendSection(builder, "Info", summary.Infos);

        string report = builder.ToString().TrimEnd();
        if (summary.Errors.Count > 0)
        {
            Debug.LogError(report);
        }
        else if (summary.Warnings.Count > 0)
        {
            Debug.LogWarning(report);
        }
        else
        {
            Debug.Log(report);
        }

        Debug.Log($"{dialogTitle}: Validated {summary.ValidatedCount} definitions. Errors={summary.Errors.Count}, Warnings={summary.Warnings.Count}, Info={summary.Infos.Count}.");
    }

    private static void AppendSection(StringBuilder builder, string title, List<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine(title + ":");
        for (int i = 0; i < lines.Count; i++)
        {
            builder.AppendLine("- " + lines[i]);
        }
    }

    public sealed class ValidationSummary
    {
        public int ValidatedCount;
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Infos = new List<string>();
    }

    private static bool SetVector3IfNeeded(SerializedProperty property, Vector3 targetValue)
    {
        if (property == null || property.propertyType != SerializedPropertyType.Vector3)
        {
            return false;
        }

        if ((property.vector3Value - targetValue).sqrMagnitude < 0.000001f)
        {
            return false;
        }

        property.vector3Value = targetValue;
        return true;
    }
}
