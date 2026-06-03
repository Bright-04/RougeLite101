using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ArchitectureIdentityValidator
{
    [MenuItem("Tools/Validate/Architecture Identity")]
    public static void ValidateArchitectureIdentity()
    {
        ValidationReport report = new ValidationReport();

        ValidateWeaponDefinitions(report);
        ValidateArmorDefinitions(report);
        ValidateWeaponRegistry(report);
        ValidateArmorRegistry(report);

        string summary = report.BuildSummary();
        if (report.WarningCount > 0)
        {
            Debug.LogWarning(summary);
        }
        else
        {
            Debug.Log(summary);
        }
    }

    private static void ValidateWeaponDefinitions(ValidationReport report)
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponDefinitionSO", new[] { "Assets" });
        Dictionary<string, List<string>> entriesById = new Dictionary<string, List<string>>();
        report.WeaponDefinitionCount = guids.Length;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponDefinitionSO definition = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(path);
            if (definition == null)
            {
                continue;
            }

            string weaponId = definition.WeaponId;
            string label = $"Weapon '{definition.name}' ({path})";

            if (string.IsNullOrWhiteSpace(weaponId))
            {
                report.EmptyIdCount++;
                report.AddWarning($"{label}: empty weaponId / WeaponId.");
                continue;
            }

            if (!entriesById.TryGetValue(weaponId, out List<string> entries))
            {
                entries = new List<string>();
                entriesById[weaponId] = entries;
            }

            entries.Add(label);
        }

        foreach (KeyValuePair<string, List<string>> pair in entriesById)
        {
            if (pair.Value.Count > 1)
            {
                report.DuplicateIdCount++;
                report.AddWarning($"Duplicate weaponId / WeaponId '{pair.Key}' found on: {string.Join(", ", pair.Value)}.");
            }
        }
    }

    private static void ValidateArmorDefinitions(ValidationReport report)
    {
        string[] guids = AssetDatabase.FindAssets("t:ArmorDefinitionSO", new[] { "Assets" });
        Dictionary<string, List<string>> entriesById = new Dictionary<string, List<string>>();
        report.ArmorDefinitionCount = guids.Length;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ArmorDefinitionSO definition = AssetDatabase.LoadAssetAtPath<ArmorDefinitionSO>(path);
            if (definition == null)
            {
                continue;
            }

            string equipmentId = definition.EquipmentId;
            string label = $"Armor '{definition.name}' ({path})";

            if (string.IsNullOrWhiteSpace(equipmentId))
            {
                report.EmptyIdCount++;
                report.AddWarning($"{label}: empty equipmentId / EquipmentId.");
                continue;
            }

            if (!entriesById.TryGetValue(equipmentId, out List<string> entries))
            {
                entries = new List<string>();
                entriesById[equipmentId] = entries;
            }

            entries.Add(label);
        }

        foreach (KeyValuePair<string, List<string>> pair in entriesById)
        {
            if (pair.Value.Count > 1)
            {
                report.DuplicateIdCount++;
                report.AddWarning($"Duplicate equipmentId / EquipmentId '{pair.Key}' found on: {string.Join(", ", pair.Value)}.");
            }
        }
    }

    private static void ValidateWeaponRegistry(ValidationReport report)
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponRegistry", new[] { "Assets" });
        if (guids.Length == 0)
        {
            report.AddWarning("WeaponRegistry asset not found.");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponRegistry registry = AssetDatabase.LoadAssetAtPath<WeaponRegistry>(path);
            if (registry == null)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(registry);
            SerializedProperty weaponsProperty = serializedObject.FindProperty("allWeapons");
            if (weaponsProperty == null || !weaponsProperty.isArray)
            {
                report.AddWarning($"WeaponRegistry ({path}): could not inspect allWeapons.");
                continue;
            }

            for (int index = 0; index < weaponsProperty.arraySize; index++)
            {
                SerializedProperty element = weaponsProperty.GetArrayElementAtIndex(index);
                report.WeaponRegistryEntryCount++;

                WeaponDefinitionSO weapon = element.objectReferenceValue as WeaponDefinitionSO;
                if (weapon == null)
                {
                    report.AddWarning($"WeaponRegistry ({path}): allWeapons[{index}] is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(weapon.WeaponId))
                {
                    report.EmptyIdCount++;
                    report.AddWarning($"WeaponRegistry ({path}): '{weapon.name}' at allWeapons[{index}] has an empty weaponId / WeaponId.");
                }
            }
        }
    }

    private static void ValidateArmorRegistry(ValidationReport report)
    {
        string[] guids = AssetDatabase.FindAssets("t:ArmorRegistry", new[] { "Assets" });
        if (guids.Length == 0)
        {
            report.AddWarning("ArmorRegistry asset not found.");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ArmorRegistry registry = AssetDatabase.LoadAssetAtPath<ArmorRegistry>(path);
            if (registry == null)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(registry);
            SerializedProperty armorProperty = serializedObject.FindProperty("allArmor");
            if (armorProperty == null || !armorProperty.isArray)
            {
                report.AddWarning($"ArmorRegistry ({path}): could not inspect allArmor.");
                continue;
            }

            for (int index = 0; index < armorProperty.arraySize; index++)
            {
                SerializedProperty element = armorProperty.GetArrayElementAtIndex(index);
                report.ArmorRegistryEntryCount++;

                ArmorDefinitionSO armor = element.objectReferenceValue as ArmorDefinitionSO;
                if (armor == null)
                {
                    report.AddWarning($"ArmorRegistry ({path}): allArmor[{index}] is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(armor.EquipmentId))
                {
                    report.EmptyIdCount++;
                    report.AddWarning($"ArmorRegistry ({path}): '{armor.name}' at allArmor[{index}] has an empty equipmentId / EquipmentId.");
                }
            }
        }
    }

    private sealed class ValidationReport
    {
        private readonly List<string> warnings = new List<string>();

        public int WeaponDefinitionCount { get; set; }
        public int ArmorDefinitionCount { get; set; }
        public int WeaponRegistryEntryCount { get; set; }
        public int ArmorRegistryEntryCount { get; set; }
        public int EmptyIdCount { get; set; }
        public int DuplicateIdCount { get; set; }
        public int WarningCount => warnings.Count;

        public void AddWarning(string message)
        {
            warnings.Add(message);
        }

        public string BuildSummary()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Architecture Identity Validation");
            builder.AppendLine($"WeaponDefinitionSO assets checked: {WeaponDefinitionCount}");
            builder.AppendLine($"ArmorDefinitionSO assets checked: {ArmorDefinitionCount}");
            builder.AppendLine($"WeaponRegistry entries checked: {WeaponRegistryEntryCount}");
            builder.AppendLine($"ArmorRegistry entries checked: {ArmorRegistryEntryCount}");
            builder.AppendLine($"Empty ID count: {EmptyIdCount}");
            builder.AppendLine($"Duplicate ID count: {DuplicateIdCount}");
            builder.AppendLine($"Warnings: {warnings.Count}");

            if (warnings.Count == 0)
            {
                builder.AppendLine("No issues found. Stable IDs are populated and registries are consistent.");
                return builder.ToString().TrimEnd();
            }

            for (int index = 0; index < warnings.Count; index++)
            {
                builder.AppendLine($"- {warnings[index]}");
            }

            return builder.ToString().TrimEnd();
        }
    }
}