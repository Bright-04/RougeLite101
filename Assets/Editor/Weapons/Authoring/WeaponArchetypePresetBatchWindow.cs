using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class WeaponArchetypePresetBatchWindow : EditorWindow
{
    private const string PresetFolder = "Assets/Data/Weapons/AlignmentPresets";

    private readonly List<WeaponDefinitionSO> selectedDefinitions = new List<WeaponDefinitionSO>();
    private Vector2 scroll;
    private WeaponArchetype archetype = WeaponArchetype.Sword;
    private WeaponAlignmentPreset alignmentPreset;
    private bool useDerivedArchetypePerWeapon = true;
    private bool autoPickDefaultPreset = true;
    private bool onlyFillMissingFields;

    [MenuItem("Tools/Weapons/Batch Assign Weapon Archetype Presets")]
    public static void Open()
    {
        WeaponArchetypePresetBatchWindow window = GetWindow<WeaponArchetypePresetBatchWindow>("Weapon Preset Batch");
        window.minSize = new Vector2(520f, 420f);
        window.RefreshSelection();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshSelection();
    }

    private void OnSelectionChange()
    {
        RefreshSelection();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Weapon Preset Migration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select WeaponDefinitionSO assets in the Project window, then batch-assign explicit archetypes and alignment presets. This keeps the shared runtime prefab workflow intact while making the large spritesheet migration practical.",
            MessageType.Info);

        DrawSelectionToolbar();
        EditorGUILayout.Space(6f);

        archetype = (WeaponArchetype)EditorGUILayout.EnumPopup("Explicit Archetype", archetype);
        alignmentPreset = (WeaponAlignmentPreset)EditorGUILayout.ObjectField("Alignment Preset", alignmentPreset, typeof(WeaponAlignmentPreset), false);
        useDerivedArchetypePerWeapon = EditorGUILayout.Toggle("Derive Archetype Per Weapon", useDerivedArchetypePerWeapon);
        autoPickDefaultPreset = EditorGUILayout.Toggle("Auto Pick Default Preset", autoPickDefaultPreset);
        onlyFillMissingFields = EditorGUILayout.Toggle("Only Fill Missing Fields", onlyFillMissingFields);

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Default Preset For Chosen Archetype"))
            {
                alignmentPreset = FindDefaultPreset(archetype);
            }

            if (GUILayout.Button("Refresh Selection"))
            {
                RefreshSelection();
            }
        }

        using (new EditorGUI.DisabledScope(selectedDefinitions.Count == 0))
        {
            if (GUILayout.Button("Apply To Selected Weapon Definitions", GUILayout.Height(32f)))
            {
                ApplyBatchAssignment();
            }

            if (GUILayout.Button("Validate Selected Weapon Definitions"))
            {
                WeaponDefinitionValidator.ValidateSelectedWeaponDefinitions();
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField($"Selected Weapon Definitions: {selectedDefinitions.Count}", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < selectedDefinitions.Count; i++)
        {
            WeaponDefinitionSO definition = selectedDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            WeaponArchetype resolved = WeaponArchetypeUtility.Resolve(definition);
            string presetName = definition.AlignmentPreset != null ? definition.AlignmentPreset.name : "None";
            EditorGUILayout.LabelField(
                $"{definition.name} | explicit={definition.Archetype} | resolved={resolved} | preset={presetName}");
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectionToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Select All Weapon Definitions In Project"))
            {
                string[] guids = AssetDatabase.FindAssets("t:WeaponDefinitionSO", new[] { "Assets" });
                Object[] objects = guids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>)
                    .Where(definition => definition != null)
                    .Cast<Object>()
                    .ToArray();
                Selection.objects = objects;
                RefreshSelection();
            }

            if (GUILayout.Button("Select Preset Folder"))
            {
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(PresetFolder);
                if (folder != null)
                {
                    Selection.activeObject = folder;
                    EditorGUIUtility.PingObject(folder);
                }
            }
        }
    }

    private void RefreshSelection()
    {
        selectedDefinitions.Clear();
        selectedDefinitions.AddRange(Selection.GetFiltered<WeaponDefinitionSO>(SelectionMode.Assets));
    }

    private void ApplyBatchAssignment()
    {
        int changedCount = 0;
        for (int i = 0; i < selectedDefinitions.Count; i++)
        {
            WeaponDefinitionSO definition = selectedDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            WeaponArchetype targetArchetype = useDerivedArchetypePerWeapon
                ? WeaponArchetypeUtility.Resolve(definition)
                : archetype;
            WeaponAlignmentPreset targetPreset = autoPickDefaultPreset
                ? FindDefaultPreset(targetArchetype)
                : alignmentPreset;

            SerializedObject serializedObject = new SerializedObject(definition);
            SerializedProperty archetypeProperty = serializedObject.FindProperty("archetype");
            SerializedProperty alignmentPresetProperty = serializedObject.FindProperty("alignmentPreset");

            bool shouldSetArchetype = !onlyFillMissingFields || archetypeProperty.enumValueIndex == (int)WeaponArchetype.Generic;
            bool shouldSetPreset = !onlyFillMissingFields || alignmentPresetProperty.objectReferenceValue == null;
            bool changed = false;

            if (shouldSetArchetype && archetypeProperty.enumValueIndex != (int)targetArchetype)
            {
                archetypeProperty.enumValueIndex = (int)targetArchetype;
                changed = true;
            }

            if (shouldSetPreset && alignmentPresetProperty.objectReferenceValue != targetPreset)
            {
                alignmentPresetProperty.objectReferenceValue = targetPreset;
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            Undo.RecordObject(definition, "Batch Assign Weapon Archetype Presets");
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            changedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WeaponDefinitionValidator.ValidationSummary summary = WeaponDefinitionValidator.BuildSummary(selectedDefinitions);
        Debug.Log($"Batch assigned archetype/preset on {changedCount} weapon definitions.");
        if (summary.Errors.Count > 0 || summary.Warnings.Count > 0)
        {
            Debug.LogWarning($"Post-assignment validation for {summary.ValidatedCount} selected weapon definitions found {summary.Errors.Count} errors and {summary.Warnings.Count} warnings.");
        }
        else
        {
            Debug.Log($"Post-assignment validation for {summary.ValidatedCount} selected weapon definitions completed with no issues.");
        }
    }

    private static WeaponAlignmentPreset FindDefaultPreset(WeaponArchetype targetArchetype)
    {
        string exactPath = $"{PresetFolder}/{targetArchetype}.asset";
        WeaponAlignmentPreset preset = AssetDatabase.LoadAssetAtPath<WeaponAlignmentPreset>(exactPath);
        if (preset != null)
        {
            return preset;
        }

        string[] guids = AssetDatabase.FindAssets($"t:WeaponAlignmentPreset {targetArchetype}", new[] { PresetFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            preset = AssetDatabase.LoadAssetAtPath<WeaponAlignmentPreset>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (preset != null && preset.Archetype == targetArchetype)
            {
                return preset;
            }
        }

        return null;
    }
}
