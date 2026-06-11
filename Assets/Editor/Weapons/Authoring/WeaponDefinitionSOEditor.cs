using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDefinitionSO))]
public class WeaponDefinitionSOEditor : Editor
{
    private bool showAdvancedLegacy;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawToolbar();
        EditorGUILayout.Space(6f);

        DrawNormalWorkflow();
        EditorGUILayout.Space(8f);
        DrawPointGuidance();
        EditorGUILayout.Space(8f);
        DrawAdvancedLegacy();

        if (serializedObject.ApplyModifiedProperties() && EditorApplication.isPlaying)
        {
            WeaponDefinitionSO changedDefinition = (WeaponDefinitionSO)target;
            EditorApplication.delayCall += () => RefreshEquippedWeapons(changedDefinition);
        }
    }

    private void DrawToolbar()
    {
        if (GUILayout.Button("Open Weapon Preview"))
        {
            WeaponAlignmentEditorWindow.Open((WeaponDefinitionSO)target);
        }

        if (GUILayout.Button("Open Batch Scale Calibration"))
        {
            WeaponVisualScaleCalibrationWindow.Open();
        }
    }

    private void DrawNormalWorkflow()
    {
        EditorGUILayout.LabelField("Normal Workflow", EditorStyles.boldLabel);

        DrawProperty("<Name>k__BackingField");
        DrawProperty("<ItemId>k__BackingField", "Item ID");
        DrawProperty("<Description>k__BackingField");
        DrawProperty("<ItemImage>k__BackingField", "Weapon Sprite");
        DrawProperty("equipmentClass");
        DrawProperty("archetype");
        DrawProperty("alignmentPreset");
        DrawProperty("weaponPrefab");
        DrawProperty("attackType");
        DrawProperty("visualScale");
        DrawProperty("rotationOffsetDegrees");
        DrawProperty("damage");
        DrawProperty("cooldown");
        DrawProperty("knockback");
        DrawProperty("range");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
        DrawProperty("anticipationDuration");
        DrawProperty("activeDuration");
        DrawProperty("recoveryDuration");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Projectile", EditorStyles.boldLabel);
        DrawProperty("projectilePrefab");
        DrawProperty("projectileSpeed");
        DrawProperty("projectileCount");
        DrawProperty("spreadAngle");

        WeaponAttackType attackType = (WeaponAttackType)FindProperty("attackType").enumValueIndex;
        if (attackType == WeaponAttackType.Slash)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Slash", EditorStyles.boldLabel);
            DrawProperty("slashArcDegrees");
            DrawProperty("slashRange");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Melee Visuals", EditorStyles.boldLabel);
            DrawProperty("slashVisualExtraAnticipationDegrees");
            DrawProperty("slashVisualExtraFollowThroughDegrees");
            DrawProperty("slashVfxLifetime");
            DrawProperty("slashVfxStartScaleMultiplier");
            DrawProperty("slashVfxEndScaleMultiplier");
            DrawProperty("slashVfxFadeOut");
            DrawProperty("meleeVisualPulseScaleAmount");
            DrawProperty("meleeVisualPulseBlend");
        }
        else if (attackType == WeaponAttackType.Thrust)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Thrust", EditorStyles.boldLabel);
            DrawProperty("thrustDistance");
            DrawProperty("thrustWidth");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Melee Visuals", EditorStyles.boldLabel);
            DrawProperty("thrustVisualPullbackFactor");
            DrawProperty("thrustVisualLungeFactor");
            DrawProperty("thrustVisualStretchFactor");
            DrawProperty("meleeVisualPulseScaleAmount");
            DrawProperty("meleeVisualPulseBlend");
        }
    }

    private void DrawPointGuidance()
    {
        EditorGUILayout.HelpBox(
            "Normal UsePresetRig runtime uses WeaponDefinitionSO + WeaponAlignmentPreset as the source of truth. " +
            "Preset points drive G/T/P/O/S1/S2, GripPoint aligns to the player hand anchor, projectile spawn uses preset P, " +
            "and rotation uses aim plus rotationOffsetDegrees.",
            MessageType.Info);
    }

    private void DrawAdvancedLegacy()
    {
        showAdvancedLegacy = EditorGUILayout.Foldout(showAdvancedLegacy, "Advanced / Legacy", true);
        if (!showAdvancedLegacy)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            "These fields are preserved for UsePrefabRig and LegacyFallback compatibility. Normal UsePresetRig weapons ignore legacy offsets and legacy localRotationOffset.",
            MessageType.Warning);

        DrawProperty("rigPointSource");
        DrawProperty("weaponType");
        DrawProperty("handlingMode");
        DrawProperty("visualScaleSpace");
        DrawProperty("gripPointOffset");
        DrawProperty("aimPointOffset");
        DrawProperty("localPositionOffset");
        DrawProperty("projectileSpawnPointOffset");
        DrawProperty("localRotationOffset");
        DrawProperty("flipBehavior");
        DrawProperty("slashVfxOffset");
        DrawProperty("baseDamage");
        DrawProperty("hitboxDistance");
        DrawProperty("hitboxScale");
        DrawProperty("series");
        DrawProperty("tier");
        DrawProperty("rarity");
        DrawProperty("tags");
        DrawProperty("modifiersData");
    }

    private SerializedProperty FindProperty(string name)
    {
        return serializedObject.FindProperty(name);
    }

    private void DrawProperty(string name, string label = null)
    {
        SerializedProperty property = FindProperty(name);
        if (property == null)
        {
            return;
        }

        if (label == null)
        {
            EditorGUILayout.PropertyField(property, true);
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }

    private static void RefreshEquippedWeapons(WeaponDefinitionSO changedDefinition)
    {
        if (changedDefinition == null)
        {
            return;
        }

        EquipmentManager equipmentManager = Object.FindAnyObjectByType<EquipmentManager>();
        if (equipmentManager == null)
        {
            return;
        }

        equipmentManager.RefreshEquippedWeapons(changedDefinition);
    }
}
