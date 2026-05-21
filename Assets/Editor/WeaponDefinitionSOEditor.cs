using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDefinitionSO))]
public class WeaponDefinitionSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Weapon Preview"))
        {
            WeaponAlignmentEditorWindow.Open((WeaponDefinitionSO)target);
        }

        EditorGUILayout.HelpBox(
            "Use the Weapon Alignment Editor as a runtime pose visualizer. WeaponRig child points and WeaponAlignmentUtility are the source of truth.",
            MessageType.Info);

        EditorGUILayout.Space(6f);
        DrawDefaultInspector();
    }
}
