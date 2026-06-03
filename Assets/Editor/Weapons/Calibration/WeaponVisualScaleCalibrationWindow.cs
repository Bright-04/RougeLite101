using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class WeaponVisualScaleCalibrationWindow : EditorWindow
{
    private readonly List<WeaponDefinitionSO> selectedDefinitions = new List<WeaponDefinitionSO>();
    private Vector2 scroll;
    private bool dryRunReportOnly = true;
    private bool onlyScaleIfBelowTargetMinimum = true;
    private bool clampSuggestedScale = true;
    private float clampMinScale = 1f;
    private float clampMaxScale = 10f;

    [MenuItem("Tools/Weapons/Batch Calibrate Weapon Visual Scale")]
    public static void Open()
    {
        WeaponVisualScaleCalibrationWindow window = GetWindow<WeaponVisualScaleCalibrationWindow>("Weapon Scale Calibration");
        window.minSize = new Vector2(760f, 420f);
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
        EditorGUILayout.LabelField("Weapon Visual Scale Calibration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select WeaponDefinitionSO assets in the Project window to preview and optionally apply archetype-based visualScale calibration. This edits only WeaponDefinitionSO.visualScale.",
            MessageType.Info);

        DrawSelectionToolbar();
        EditorGUILayout.Space(6f);

        dryRunReportOnly = EditorGUILayout.Toggle("Dry Run / Report Only", dryRunReportOnly);
        onlyScaleIfBelowTargetMinimum = EditorGUILayout.Toggle("Only Scale If Below Target Minimum", onlyScaleIfBelowTargetMinimum);
        clampSuggestedScale = EditorGUILayout.Toggle("Clamp Suggested Scale", clampSuggestedScale);

        using (new EditorGUI.DisabledScope(!clampSuggestedScale))
        {
            clampMinScale = EditorGUILayout.FloatField("Clamp Min Scale", clampMinScale);
            clampMaxScale = EditorGUILayout.FloatField("Clamp Max Scale", clampMaxScale);
        }

        if (clampMaxScale < clampMinScale)
        {
            clampMaxScale = clampMinScale;
        }

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Preview"))
            {
                RefreshSelection();
            }

            if (GUILayout.Button("Log Preview Report"))
            {
                Debug.Log(BuildPreviewReport());
            }
        }

        using (new EditorGUI.DisabledScope(selectedDefinitions.Count == 0))
        {
            string actionLabel = dryRunReportOnly
                ? "Run Dry Run Report"
                : "Apply Preview To Selected Weapon Definitions";
            if (GUILayout.Button(actionLabel, GUILayout.Height(32f)))
            {
                ExecuteCalibration();
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField($"Selected Weapon Definitions: {selectedDefinitions.Count}", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < selectedDefinitions.Count; i++)
        {
            DrawDefinitionPreview(selectedDefinitions[i]);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectionToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Current Project Selection"))
            {
                RefreshSelection();
            }

            if (GUILayout.Button("Select All Weapon Definitions In Project"))
            {
                string[] guids = AssetDatabase.FindAssets("t:WeaponDefinitionSO", new[] { "Assets" });
                Object[] objects = new Object[guids.Length];
                for (int i = 0; i < guids.Length; i++)
                {
                    objects[i] = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                }

                Selection.objects = objects;
                RefreshSelection();
            }
        }
    }

    private void RefreshSelection()
    {
        selectedDefinitions.Clear();
        selectedDefinitions.AddRange(Selection.GetFiltered<WeaponDefinitionSO>(SelectionMode.Assets));
    }

    private void DrawDefinitionPreview(WeaponDefinitionSO definition)
    {
        if (definition == null)
        {
            return;
        }

        if (!WeaponVisualScaleCalibrationUtility.TryBuildReport(definition, out WeaponVisualScaleCalibrationUtility.CalibrationReport report))
        {
            EditorGUILayout.HelpBox($"{definition.name}: {report.Warning}", MessageType.Warning);
            return;
        }

        float suggestedScale = GetSuggestedScale(report);
        bool shouldScale = WeaponVisualScaleCalibrationUtility.ShouldScale(report, onlyScaleIfBelowTargetMinimum);
        string status = shouldScale ? "will calibrate" : "already within threshold";

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.ObjectField(definition, typeof(WeaponDefinitionSO), false);
        EditorGUILayout.LabelField("Archetype", report.Archetype.ToString());
        EditorGUILayout.LabelField("Current Height Ratio", report.CurrentHeightRatio.ToString("0.###"));
        EditorGUILayout.LabelField("Recommended Ratio Range", report.RecommendedRange.ToString());
        EditorGUILayout.LabelField("Target Ratio", report.TargetHeightRatio.ToString("0.###"));
        EditorGUILayout.LabelField("Suggested Runtime Scale Multiplier", suggestedScale.ToString("0.###"));
        EditorGUILayout.LabelField("Weapon Final World Size", FormatVector2(report.WeaponFinalWorldSize));
        EditorGUILayout.LabelField("Player Reference World Size", FormatVector2(report.PlayerReferenceWorldSize));
        EditorGUILayout.LabelField("Status", status);
        EditorGUILayout.EndVertical();
    }

    private void ExecuteCalibration()
    {
        if (dryRunReportOnly)
        {
            Debug.Log(BuildPreviewReport());
            return;
        }

        int changedCount = 0;
        List<string> changedLines = new List<string>();
        for (int i = 0; i < selectedDefinitions.Count; i++)
        {
            WeaponDefinitionSO definition = selectedDefinitions[i];
            if (definition == null
                || !WeaponVisualScaleCalibrationUtility.TryBuildReport(definition, out WeaponVisualScaleCalibrationUtility.CalibrationReport report)
                || !WeaponVisualScaleCalibrationUtility.ShouldScale(report, onlyScaleIfBelowTargetMinimum))
            {
                continue;
            }

            float suggestedScale = GetSuggestedScale(report);
            if (!Mathf.Approximately(report.CurrentVisualScale, suggestedScale)
                && WeaponVisualScaleCalibrationUtility.ApplyVisualScale(definition, suggestedScale))
            {
                changedCount++;
                changedLines.Add(WeaponVisualScaleCalibrationUtility.BuildPreviewLine(report, suggestedScale));
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Batch weapon visual scale calibration updated {changedCount} assets.");
        for (int i = 0; i < changedLines.Count; i++)
        {
            builder.AppendLine("- " + changedLines[i]);
        }
        Debug.Log(builder.ToString().TrimEnd());
    }

    private string BuildPreviewReport()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Weapon visual scale calibration preview:");
        builder.AppendLine($"dryRun={dryRunReportOnly}");
        builder.AppendLine($"onlyScaleIfBelowTargetMinimum={onlyScaleIfBelowTargetMinimum}");
        builder.AppendLine($"clampSuggestedScale={clampSuggestedScale}");
        if (clampSuggestedScale)
        {
            builder.AppendLine($"clampRange={clampMinScale:0.###}-{clampMaxScale:0.###}");
        }

        for (int i = 0; i < selectedDefinitions.Count; i++)
        {
            WeaponDefinitionSO definition = selectedDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            if (!WeaponVisualScaleCalibrationUtility.TryBuildReport(definition, out WeaponVisualScaleCalibrationUtility.CalibrationReport report))
            {
                builder.AppendLine($"- {definition.name} | invalid | {report.Warning}");
                continue;
            }

            float suggestedScale = GetSuggestedScale(report);
            string status = WeaponVisualScaleCalibrationUtility.ShouldScale(report, onlyScaleIfBelowTargetMinimum)
                ? "candidate"
                : "skipped";
            builder.AppendLine($"- {WeaponVisualScaleCalibrationUtility.BuildPreviewLine(report, suggestedScale)} | status={status}");
        }

        return builder.ToString().TrimEnd();
    }

    private float GetSuggestedScale(WeaponVisualScaleCalibrationUtility.CalibrationReport report)
    {
        return clampSuggestedScale
            ? WeaponVisualScaleCalibrationUtility.GetClampedSuggestedScale(report, clampMinScale, clampMaxScale)
            : report.SuggestedVisualScale;
    }

    private static string FormatVector2(Vector2 value)
    {
        return $"({value.x:0.###}, {value.y:0.###})";
    }
}
