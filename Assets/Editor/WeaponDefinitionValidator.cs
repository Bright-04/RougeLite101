using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WeaponDefinitionValidator
{
    [MenuItem("Tools/Weapons/Validate Weapon Definitions")]
    public static void ValidateAllWeaponDefinitions()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponDefinitionSO", new[] { "Assets" });
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        List<string> infos = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponDefinitionSO definition = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(path);
            if (definition == null)
            {
                continue;
            }

            ValidateDefinition(definition, path, errors, warnings, infos);
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Validated {guids.Length} WeaponDefinitionSO assets.");
        builder.AppendLine($"Errors: {errors.Count}");
        builder.AppendLine($"Warnings: {warnings.Count}");
        builder.AppendLine($"Info: {infos.Count}");

        AppendSection(builder, "Errors", errors);
        AppendSection(builder, "Warnings", warnings);
        AppendSection(builder, "Info", infos);

        string report = builder.ToString().TrimEnd();
        if (errors.Count > 0)
        {
            Debug.LogError(report);
        }
        else if (warnings.Count > 0)
        {
            Debug.LogWarning(report);
        }
        else
        {
            Debug.Log(report);
        }

        EditorUtility.DisplayDialog(
            "Weapon Validation",
            $"Validated {guids.Length} definitions.\nErrors: {errors.Count}\nWarnings: {warnings.Count}\nDetails were written to the Console.",
            "OK");
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

        if (definition.WeaponPrefab == null)
        {
            errors.Add($"{label}: Missing weapon visual prefab.");
        }

        WeaponArchetype archetype = definition.ResolvedArchetype;
        bool needsProjectile = definition.WeaponType == WeaponType.Projectile;
        bool needsMeleeRig = definition.WeaponType == WeaponType.Melee;

        if (definition.AlignmentPreset == null)
        {
            warnings.Add($"{label}: Missing alignment preset. Rig or legacy offsets must carry alignment.");
        }
        else if (definition.AlignmentPreset.Archetype != WeaponArchetype.Generic && definition.AlignmentPreset.Archetype != archetype)
        {
            warnings.Add($"{label}: Alignment preset archetype '{definition.AlignmentPreset.Archetype}' does not match resolved archetype '{archetype}'.");
        }

        if (definition.WeaponPrefab == null)
        {
            return;
        }

        WeaponRig rig = definition.WeaponPrefab.GetComponentInChildren<WeaponRig>(true);
        if (rig == null)
        {
            warnings.Add($"{label}: Missing WeaponRig on prefab '{definition.WeaponPrefab.name}'. Runtime will fall back to preset/legacy offsets.");
        }
        else
        {
            if (!rig.HasRequiredPointsFor(definition))
            {
                warnings.Add($"{label}: WeaponRig on prefab '{definition.WeaponPrefab.name}' is missing required points for archetype '{archetype}'.");
            }

            if (needsProjectile && rig.ProjectileSpawnPoint == null)
            {
                errors.Add($"{label}: Ranged weapon prefab '{definition.WeaponPrefab.name}' is missing ProjectileSpawnPoint.");
            }
        }

        if (needsProjectile && definition.UsesLegacyProjectileSpawnOffset)
        {
            warnings.Add($"{label}: Uses legacy ProjectileSpawnPointOffset fallback. Prefer WeaponRig.ProjectileSpawnPoint.");
        }

        if (needsMeleeRig)
        {
            MeleeWeapon meleeWeapon = definition.WeaponPrefab.GetComponent<MeleeWeapon>();
            if (meleeWeapon == null)
            {
                warnings.Add($"{label}: Melee definition is assigned to prefab '{definition.WeaponPrefab.name}' without MeleeWeapon component.");
            }
            else if (!PrefabHasObjectReference(definition.WeaponPrefab, "weaponCollider"))
            {
                warnings.Add($"{label}: Melee prefab '{definition.WeaponPrefab.name}' does not serialize a weaponCollider reference.");
            }
        }

        if (definition.WeaponType == WeaponType.Projectile)
        {
            ProjectileWeapon projectileWeapon = definition.WeaponPrefab.GetComponent<ProjectileWeapon>();
            if (projectileWeapon == null)
            {
                warnings.Add($"{label}: Projectile definition is assigned to prefab '{definition.WeaponPrefab.name}' without ProjectileWeapon component.");
            }
        }

        if (definition.AlignmentPreset == null && rig == null)
        {
            infos.Add($"{label}: Preview/runtime pose mismatch risk is elevated because alignment depends on legacy serialized offsets only.");
        }
    }

    private static bool PrefabHasObjectReference(GameObject prefab, string propertyName)
    {
        SerializedObject serializedObject = new SerializedObject(prefab.GetComponent<MeleeWeapon>());
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null && property.objectReferenceValue != null;
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
}
