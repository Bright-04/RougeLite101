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
            "Use the Weapon Alignment Editor before using a weapon in gameplay. Runtime reads the saved alignment data.",
            MessageType.Info);

        EditorGUILayout.Space(6f);
        DrawDefaultInspector();
    }
}
