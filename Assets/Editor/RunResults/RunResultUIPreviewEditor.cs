using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(RunResultUIPreview))]
public class RunResultUIPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        RunResultUIPreview preview = (RunResultUIPreview)target;

        EditorGUILayout.Space();

        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Preview actions are disabled during Play Mode.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Use the buttons below to preview the result UI in edit mode only.", MessageType.None);
        }

        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Apply Preview"))
            {
                ApplyPreview(preview);
            }

            if (GUILayout.Button("Hide Preview"))
            {
                HidePreview(preview);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void ApplyPreview(RunResultUIPreview preview)
    {
        if (preview == null)
        {
            return;
        }

        RegisterUndo(preview);

        if (preview.ApplyPreview())
        {
            MarkSceneDirty(preview);
        }
    }

    private static void HidePreview(RunResultUIPreview preview)
    {
        if (preview == null)
        {
            return;
        }

        RegisterUndo(preview);

        if (preview.HidePreview())
        {
            MarkSceneDirty(preview);
        }
    }

    private static void RegisterUndo(RunResultUIPreview preview)
    {
        Undo.RecordObject(preview, "Run Result UI Preview");

        if (preview.ResultUI != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(preview.ResultUI.gameObject, "Run Result UI Preview");
        }
        else
        {
            Undo.RegisterFullObjectHierarchyUndo(preview.gameObject, "Run Result UI Preview");
        }
    }

    private static void MarkSceneDirty(RunResultUIPreview preview)
    {
        EditorUtility.SetDirty(preview);

        if (preview.ResultUI != null)
        {
            EditorUtility.SetDirty(preview.ResultUI);
        }

        if (preview.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
        }
    }
}
