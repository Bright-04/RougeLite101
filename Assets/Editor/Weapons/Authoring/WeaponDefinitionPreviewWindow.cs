using System;
using Object = UnityEngine.Object;
using UnityEditor;
using UnityEngine;

public class WeaponAlignmentEditorWindow : EditorWindow
{
    private const string WeaponAnchorName = "WeaponAnchor";
    private const string RuntimePlayerPrefabPath = WeaponVisualScaleCalibrationUtility.RuntimePlayerPrefabPath;
    private const float BasePixelsPerWorldUnit = 120f;
    private const float ToolbarHeight = 26f;
    private const float LeftPanelWidth = 340f;
    private const float Padding = 8f;

    private WeaponDefinitionSO weaponDefinition;
    private SerializedObject serializedDefinition;
    private SerializedProperty archetypeProperty;
    private SerializedProperty alignmentPresetProperty;
    private SerializedProperty handlingModeProperty;
    private SerializedProperty gripPointOffsetProperty;
    private SerializedProperty aimPointOffsetProperty;
    private SerializedProperty localRotationOffsetProperty;
    private SerializedProperty localPositionOffsetProperty;
    private SerializedProperty visualScaleProperty;
    private SerializedProperty flipBehaviorProperty;
    private SerializedProperty projectileSpawnPointOffsetProperty;
    private SerializedProperty slashVfxOffsetProperty;

    private enum WorkflowMode
    {
        EditWeaponAlignment,
        PreviewRuntimeObject,
        ValidateGameView
    }

    private enum PreviewSurfaceMode
    {
        RuntimeView,
        AuthoringView
    }

    private enum PreviewAimMode
    {
        FreeAim,
        EightDirections
    }

    private enum MarkerDisplayMode
    {
        Hidden,
        DotsOnly,
        RelevantOnly,
        AllLabels
    }

    private enum BoundsOverlayMode
    {
        Hidden,
        SelectedBounds,
        Both
    }

    private enum AuthoringEditTarget
    {
        EditPreset,
        EditSelectedWeaponOverride
    }

    private WorkflowMode workflowMode = WorkflowMode.EditWeaponAlignment;
    private PreviewSurfaceMode previewSurfaceMode = PreviewSurfaceMode.AuthoringView;
    private AuthoringEditTarget authoringEditTarget = AuthoringEditTarget.EditPreset;
    private PreviewAimMode previewAimMode;
    private MarkerDisplayMode markerDisplayMode = MarkerDisplayMode.RelevantOnly;
    private GameObject previewPlayerPrefab;
    private Sprite previewPlayerSprite;
    private Vector3 previewWeaponAnchorOffset = Vector3.zero;
    private Vector2 settingsScroll;
    private Vector2 viewPan;
    private float viewZoom = 1f;
    private float authoringPreviewZoom = 1.6f;
    private float weaponDetailZoom = 8f;
    private float aimAngle;
    private SpriteBoundsCoordinateSpace previewBoundsMode = SpriteBoundsCoordinateSpace.OpaqueContentBounds;
    private BoundsOverlayMode boundsOverlayMode = BoundsOverlayMode.Both;
    private bool autoTest360Aim;
    private bool useRuntimePlayerReference = true;
    private bool showWeaponAnchor = true;
    private bool showGripAimPoints = true;
    private bool showRuntimePoseDebug = true;
    private bool showIrrelevantPoints;
    private bool showWeaponDetailPanel = true;
    private bool scaleDiagnosticsFoldout;
    private bool showWeaponSpriteGhost = true;
    private bool showPlayerGhost;
    private bool showFullPlayerBody;
    private bool showRuntimeScaleComparison;
    private bool showAdvancedDiagnostics;
    private bool useActualRuntimeVisualForPreview;
    private bool useGameCameraProjection;
    private bool useMainCameraRenderPreview;
    private bool useNormalizedCalibrationScale;
    private bool advancedFoldout;
    private bool legacyFoldout;
    private Rect lastViewportRect;
    private Rect lastDetailContentRect;
    private Rect lastDetailSpriteRect;
    private Rect lastWeaponGuiRect;
    private Rect lastPlayerGuiRect;
    private Bounds lastWeaponWorldBounds;
    private Bounds lastPlayerWorldBounds;
    private bool hasLastWeaponGuiRect;
    private bool hasLastPlayerGuiRect;
    private bool hasLastPlayerWorldBounds;
    private Rect lastRuntimePlayerGuiRect;
    private Bounds lastRuntimePlayerWorldBounds;
    private bool hasLastRuntimePlayerGuiRect;
    private bool hasLastRuntimePlayerWorldBounds;
    private RuntimeVisualSnapshot lastActualRuntimeSnapshot;
    private bool hasLastActualRuntimeSnapshot;
    private string lastEditorSharedBoundsDebugLine;
    private string lastPreviewBoundsWarning;
    private bool pendingFocusPoints = true;
    private EditableAuthoringPoint? activeDraggedPoint;

    private readonly struct EditableAuthoringPoint
    {
        public readonly PreviewMarkerKind MarkerKind;
        public readonly string PresetPropertyName;

        public EditableAuthoringPoint(PreviewMarkerKind markerKind, string presetPropertyName)
        {
            MarkerKind = markerKind;
            PresetPropertyName = presetPropertyName;
        }
    }

    private struct AuthoringPointSnapshot
    {
        public bool IsValid;
        public WeaponAlignmentPreset Preset;
        public Bounds SpriteBounds;
        public Bounds AuthoringBounds;
        public Sprite Sprite;
        public Vector3 GripPointLocal;
        public Vector3 TipPointLocal;
        public Vector3 ProjectileSpawnPointLocal;
        public Vector3 SlashOriginLocal;
        public Vector3 SlashArcStartLocal;
        public Vector3 SlashArcEndLocal;
        public Vector3 WeaponPosition;
        public Vector3 GripPointWorld;
        public Vector3 TipPointWorld;
        public Vector3 ProjectileSpawnPointWorld;
        public Vector3 SlashOriginWorld;
        public Vector3 SlashArcStartWorld;
        public Vector3 SlashArcEndWorld;
        public Vector2 WeaponAnchorWorld;
        public Vector2 AimDirection;
        public Quaternion Rotation;
        public Vector3 VisualScale;
    }

    private struct RuntimeVisualSnapshot
    {
        public bool IsValid;
        public GameObject Player;
        public Transform WeaponRoot;
        public Transform WeaponAnchor;
        public Transform ActiveVisual;
        public SpriteRenderer WeaponRenderer;
        public SpriteRenderer PlayerRenderer;
        public string ActiveVisualName;
        public string SpriteName;
        public string SortingLayerName;
        public int SortingOrder;
        public Vector3 WorldPosition;
        public Vector3 LocalPositionToPlayer;
        public float RotationZ;
        public Vector3 LocalScale;
        public Vector3 LossyScale;
        public Bounds SpriteBounds;
        public Bounds RendererBounds;
        public Bounds RendererBoundsLocalToPlayer;
        public Rect GuiRect;
        public Rect PlayerGuiRect;
        public Vector2 WorldRatio;
        public Vector2 GuiRatio;
        public WeaponRenderBoundsReport BoundsReport;
    }

    private struct GameCameraProjectionSnapshot
    {
        public bool IsValid;
        public Camera Camera;
        public Rect PreviewContentRect;
        public Rect PlayerGuiRect;
        public Rect WeaponGuiRect;
        public Vector2 WeaponPlayerScreenRatio;
        public Vector2 EditorGuiWeaponPlayerRatio;
        public Vector3 PlayerWorldPosition;
        public Vector3 WeaponWorldPosition;
        public string PixelPerfectStatus;
    }

    private struct BoundsRatioDebugSnapshot
    {
        public bool IsValid;
        public string WeaponRendererSource;
        public string PlayerRendererSource;
        public Bounds WeaponBounds;
        public Bounds PlayerBounds;
        public Vector2 WeaponRenderedSize;
        public Vector2 PlayerRenderedSize;
        public Vector2 Ratio;
        public string PlayerScaleSource;
        public string WeaponParentScaleSource;
        public string PlayerBoundsMode;
    }

    [MenuItem("Tools/Weapons/Weapon Alignment Editor")]
    public static void Open()
    {
        GetWindow<WeaponAlignmentEditorWindow>("Weapon Alignment");
    }

    public static void Open(WeaponDefinitionSO definition)
    {
        WeaponAlignmentEditorWindow window = GetWindow<WeaponAlignmentEditorWindow>("Weapon Alignment");
        window.SetDefinition(definition);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(920f, 520f);
        ApplyWorkflowDefaults();
        pendingFocusPoints = true;
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is WeaponDefinitionSO selectedDefinition)
        {
            SetDefinition(selectedDefinition);
            Repaint();
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (weaponDefinition == null)
        {
            Rect infoRect = new Rect(12f, ToolbarHeight + 12f, position.width - 24f, 44f);
            EditorGUI.HelpBox(infoRect, "Select a WeaponDefinitionSO asset.", MessageType.Info);
            return;
        }

        EnsureSerializedDefinition();
        serializedDefinition.Update();

        Rect contentRect = new Rect(0f, ToolbarHeight, position.width, position.height - ToolbarHeight);
        Rect settingsRect = new Rect(0f, contentRect.y, LeftPanelWidth, contentRect.height);
        Rect viewportRect = new Rect(LeftPanelWidth, contentRect.y, contentRect.width - LeftPanelWidth, contentRect.height);

        DrawSettingsPanel(settingsRect);
        serializedDefinition.ApplyModifiedProperties();

        DrawPreviewViewport(viewportRect, (WeaponHandlingMode)handlingModeProperty.enumValueIndex);
    }

    private void DrawToolbar()
    {
        Rect toolbarRect = new Rect(0f, 0f, position.width, ToolbarHeight);
        EditorGUI.DrawRect(toolbarRect, new Color(0.16f, 0.16f, 0.16f));

        GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
        GUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        WeaponDefinitionSO selected = (WeaponDefinitionSO)EditorGUILayout.ObjectField(weaponDefinition, typeof(WeaponDefinitionSO), false, GUILayout.Width(240f));
        if (EditorGUI.EndChangeCheck())
        {
            SetDefinition(selected);
        }

        if (weaponDefinition != null)
        {
            EnsureSerializedDefinition();
            serializedDefinition.Update();

            EditorGUI.BeginChangeCheck();
            PreviewSurfaceMode nextPreviewSurfaceMode = (PreviewSurfaceMode)EditorGUILayout.EnumPopup(previewSurfaceMode, EditorStyles.toolbarPopup, GUILayout.Width(150f));
            if (EditorGUI.EndChangeCheck())
            {
                SetPreviewSurfaceMode(nextPreviewSurfaceMode);
            }

            using (new EditorGUI.DisabledScope(IsMainCameraRenderView()))
            {
                if (GUILayout.Button("Focus Points", EditorStyles.toolbarButton, GUILayout.Width(88f))) FocusPoints();
                if (GUILayout.Button("Focus Weapon Detail", EditorStyles.toolbarButton, GUILayout.Width(122f))) FocusWeaponDetail();
                if (GUILayout.Button("Focus Runtime Comparison", EditorStyles.toolbarButton, GUILayout.Width(146f))) FocusRuntimeComparison();
                if (GUILayout.Button("Fit Player + Weapon", EditorStyles.toolbarButton, GUILayout.Width(118f))) FitAll();
                if (GUILayout.Button("Fit Weapon Only", EditorStyles.toolbarButton, GUILayout.Width(108f))) FitWeapon();
                if (GUILayout.Button("1:1 Runtime Scale", EditorStyles.toolbarButton, GUILayout.Width(108f))) SetOneToOneRuntimeScale();
                if (GUILayout.Button("Match Runtime Scene Scale", EditorStyles.toolbarButton, GUILayout.Width(144f))) MatchRuntimeSceneScale();
                if (GUILayout.Button("Reset View", EditorStyles.toolbarButton, GUILayout.Width(78f))) ResetView();
            }

            if (IsMainCameraRenderView())
            {
                GUILayout.Label("Source: Main Camera RenderTexture", EditorStyles.miniLabel, GUILayout.Width(190f));
            }
            serializedDefinition.ApplyModifiedProperties();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawSettingsPanel(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f));
        GUILayout.BeginArea(new Rect(rect.x + Padding, rect.y + Padding, rect.width - Padding * 2f, rect.height - Padding * 2f));
        settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);

        WeaponHandlingMode mode = (WeaponHandlingMode)handlingModeProperty.enumValueIndex;

        DrawWorkflowHeader();

        EditorGUILayout.Space(6f);
        DrawPropertyWithHelp(
            archetypeProperty,
            "Weapon Archetype",
            "Explicit archetype used for rig validation, preset assignment, and migration workflow.");

        DrawPropertyWithHelp(
            alignmentPresetProperty,
            "Alignment Preset",
            "Optional normalized sprite-space preset used when a prefab rig is missing or incomplete.");

        DrawPropertyWithHelp(
            handlingModeProperty,
            "Handling Mode",
            "Runtime handling mode for this weapon. MVP visuals still orbit around WeaponAnchor based on aim direction.");

        DrawPropertyWithHelp(
            visualScaleProperty,
            "Runtime Scale Multiplier",
            "Runtime data. This affects gameplay weapon scale and Runtime View. It is not an editor-only zoom control.");

        DrawAuthoringModeHeader();

        DrawRigStatus();
        DrawAuthoringHelp();
        DrawDisplayToggles();
        DrawAimControls();
        DrawEditingTargetSection();
        DrawPointEditingFields(mode);

        if (showRuntimeScaleComparison)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Runtime Scale Comparison", EditorStyles.boldLabel);
            DrawEditAlignmentScaleComparison();
            DrawEditorRuntimeComparisonPanel();
        }

        if (showAdvancedDiagnostics)
        {
            DrawAdvancedDiagnosticsPanel();
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawWorkflowHeader()
    {
        EditorGUILayout.LabelField(GetPreviewTitle(), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(GetWorkflowPurpose(), workflowMode == WorkflowMode.ValidateGameView ? MessageType.Info : MessageType.Warning);

        if ((workflowMode == WorkflowMode.PreviewRuntimeObject || workflowMode == WorkflowMode.ValidateGameView) && !EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use the runtime Player, WeaponRoot, WeaponAnchor, and CurrentWeaponVisual.", MessageType.Warning);
        }
    }

    private void DrawAuthoringHelp()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Point Guide", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "GripPoint = where the hand holds the weapon\n" +
            "TipPoint = weapon tip/front\n" +
            "ProjectileSpawnPoint = where projectiles spawn\n" +
            "SlashOrigin / SlashArc = melee damage and visual authoring points\n" +
            "Preview Zoom does not affect runtime",
            MessageType.None);
    }

    private void DrawAuthoringModeHeader()
    {
        if (previewSurfaceMode == PreviewSurfaceMode.AuthoringView)
        {
            authoringPreviewZoom = EditorGUILayout.Slider(new GUIContent("Preview Zoom"), authoringPreviewZoom, 0.75f, 4f);
            EditorGUILayout.HelpBox("Simple Authoring is point-first. The main canvas prioritizes H/G/T/P/O/S1/S2 placement, and the player body is optional.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Runtime View uses runtime-equivalent pose and runtime scale. Use it for inspection, not point placement.", MessageType.Info);
        }

        if (workflowMode == WorkflowMode.EditWeaponAlignment && previewSurfaceMode != PreviewSurfaceMode.AuthoringView)
        {
            if (GUILayout.Button("Validate Against Game View"))
            {
                SetWorkflowMode(WorkflowMode.ValidateGameView);
            }
        }
    }

    private void DrawDisplayToggles()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
        showWeaponSpriteGhost = EditorGUILayout.Toggle("Show Weapon Sprite Ghost", showWeaponSpriteGhost);
        showPlayerGhost = EditorGUILayout.Toggle("Show Player Ghost", showPlayerGhost);
        showFullPlayerBody = EditorGUILayout.Toggle("Show Full Player Body", showFullPlayerBody);
        showRuntimeScaleComparison = EditorGUILayout.Toggle("Show Runtime Scale Comparison", showRuntimeScaleComparison);
        showAdvancedDiagnostics = EditorGUILayout.Toggle("Show Advanced Diagnostics", showAdvancedDiagnostics);
        showWeaponDetailPanel = EditorGUILayout.Toggle("Show Weapon Detail Panel", showWeaponDetailPanel);
        if (showWeaponDetailPanel)
        {
            weaponDetailZoom = EditorGUILayout.Slider(new GUIContent("Detail Zoom"), weaponDetailZoom, 4f, 20f);
        }
    }

    private void DrawAimControls()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Preview Aim", EditorStyles.boldLabel);
        previewAimMode = (PreviewAimMode)EditorGUILayout.EnumPopup(new GUIContent("Preview Aim Mode"), previewAimMode);
        if (previewAimMode == PreviewAimMode.EightDirections)
        {
            int octant = Mathf.RoundToInt(Mathf.Repeat(aimAngle, 360f) / 45f) % 8;
            string[] octantLabels = { "Right", "Up Right", "Up", "Up Left", "Left", "Down Left", "Down", "Down Right" };
            octant = GUILayout.Toolbar(octant, octantLabels);
            aimAngle = octant * 45f;
        }
        else
        {
            aimAngle = EditorGUILayout.Slider(new GUIContent("Preview Aim Angle"), aimAngle, -180f, 180f);
        }

        autoTest360Aim = EditorGUILayout.Toggle(new GUIContent("Auto Test 360 Aim"), autoTest360Aim);
        if (autoTest360Aim)
        {
            aimAngle = Mathf.Repeat((float)EditorApplication.timeSinceStartup * 90f, 360f) - 180f;
            Repaint();
        }
    }

    private void DrawEditingTargetSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Editing Target", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            bool editPreset = GUILayout.Toggle(authoringEditTarget == AuthoringEditTarget.EditPreset, "Edit Preset", EditorStyles.miniButtonLeft);
            using (new EditorGUI.DisabledScope(true))
            {
                GUILayout.Toggle(authoringEditTarget == AuthoringEditTarget.EditSelectedWeaponOverride, "Edit Selected Weapon Override", EditorStyles.miniButtonRight);
            }

            if (editPreset)
            {
                authoringEditTarget = AuthoringEditTarget.EditPreset;
            }
        }

        if (weaponDefinition != null && weaponDefinition.AlignmentPreset != null)
        {
            EditorGUILayout.HelpBox($"Editing Alignment Preset affects all weapons using preset '{weaponDefinition.AlignmentPreset.name}'.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign an Alignment Preset to enable point editing.", MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("Selected Weapon Override Available", false);
        }
    }

    private void DrawPointEditingFields(WeaponHandlingMode mode)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Point Editing", EditorStyles.boldLabel);

        if (!TryGetAuthoringPointSnapshot(out AuthoringPointSnapshot snapshot))
        {
            EditorGUILayout.HelpBox("Point editing requires a WeaponAlignmentPreset and a weapon sprite.", MessageType.Warning);
            return;
        }

        SerializedObject presetObject = new SerializedObject(snapshot.Preset);
        presetObject.Update();
        DrawPresetPointProperty(presetObject, "normalizedGripPoint", "GripPoint");
        DrawPresetPointProperty(presetObject, "normalizedTipPoint", "TipPoint");

        if (IsRangedAuthoringArchetype())
        {
            DrawPresetPointProperty(presetObject, "normalizedProjectileSpawnPoint", "ProjectileSpawnPoint");
        }

        if (IsMeleeAuthoringArchetype())
        {
            DrawPresetPointProperty(presetObject, "normalizedSlashOrigin", "SlashOrigin");
            DrawPresetPointProperty(presetObject, "normalizedSlashArcStart", "SlashArcStart");
            DrawPresetPointProperty(presetObject, "normalizedSlashArcEnd", "SlashArcEnd");
        }

        presetObject.ApplyModifiedProperties();

        DrawPropertyWithHelp(
            localRotationOffsetProperty,
            "Local Rotation Offset",
            "Correction between sprite authored direction and runtime aim direction.");
        DrawPropertyWithHelp(
            visualScaleProperty,
            "Runtime Scale Multiplier",
            "Runtime data. This affects gameplay weapon scale and Runtime View. It is not an editor-only zoom control.");

        if (weaponDefinition != null && weaponDefinition.ResolvedArchetype == WeaponArchetype.Bow)
        {
            DrawBowAuthoringWarnings(snapshot);
        }
        else if (IsMeleeAuthoringArchetype())
        {
            EditorGUILayout.HelpBox("Melee authoring shows H, G, T, O, S1, and S2. Projectile point is hidden unless you enable advanced diagnostics.", MessageType.None);
        }
    }

    private static void DrawPresetPointProperty(SerializedObject presetObject, string propertyName, string label)
    {
        SerializedProperty property = presetObject.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
    }

    private void DrawBowAuthoringWarnings(AuthoringPointSnapshot snapshot)
    {
        float centerDistance = Vector2.Distance(ToVector2(snapshot.GripPointLocal), ToVector2(snapshot.SpriteBounds.center));
        float softThreshold = Mathf.Max(snapshot.SpriteBounds.size.x, snapshot.SpriteBounds.size.y) * 0.35f;
        if (centerDistance > softThreshold)
        {
            EditorGUILayout.HelpBox("GripPoint is far from the weapon sprite center. Confirm that the hand anchor still feels stable in the point-first view.", MessageType.Warning);
        }

        Vector2 projectileFromGrip = ToVector2(snapshot.ProjectileSpawnPointWorld - snapshot.GripPointWorld);
        if (Vector2.Dot(projectileFromGrip, snapshot.AimDirection.normalized) <= 0f)
        {
            EditorGUILayout.HelpBox("ProjectileSpawnPoint is behind GripPoint relative to the current aim direction.", MessageType.Warning);
        }
    }

    private void DrawAdvancedDiagnosticsPanel()
    {
        EditorGUILayout.Space(6f);
        advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced Diagnostics", true);
        if (!advancedFoldout)
        {
            return;
        }

        EditorGUILayout.HelpBox("Advanced diagnostics preserve the old runtime-oriented validation tools, but they are intentionally out of the default authoring flow.", MessageType.Info);
        if (workflowMode == WorkflowMode.EditWeaponAlignment && GUILayout.Button("Validate Against Game View"))
        {
            SetWorkflowMode(WorkflowMode.ValidateGameView);
        }

        useRuntimePlayerReference = EditorGUILayout.Toggle(new GUIContent("Use Runtime Player Reference"), useRuntimePlayerReference);
        previewBoundsMode = (SpriteBoundsCoordinateSpace)EditorGUILayout.EnumPopup(new GUIContent("Bounds Mode"), previewBoundsMode);
        boundsOverlayMode = (BoundsOverlayMode)EditorGUILayout.EnumPopup(new GUIContent("Bounds Overlay"), boundsOverlayMode);
        markerDisplayMode = (MarkerDisplayMode)EditorGUILayout.EnumPopup(new GUIContent("Marker Display"), markerDisplayMode);
        showIrrelevantPoints = EditorGUILayout.Toggle("Show Irrelevant Points", showIrrelevantPoints);
        previewPlayerSprite = (Sprite)EditorGUILayout.ObjectField("Fallback Player Sprite", previewPlayerSprite, typeof(Sprite), false);
        previewWeaponAnchorOffset = EditorGUILayout.Vector3Field("Fallback Anchor Offset", previewWeaponAnchorOffset);

        DrawSpriteDiagnostics();
        DrawScaleDiagnostics();
        DrawFooterStatus();

        legacyFoldout = EditorGUILayout.Foldout(legacyFoldout, "Legacy Fallback Fields", true);
        if (legacyFoldout)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(gripPointOffsetProperty, new GUIContent("Grip Point Offset (Legacy Fallback)"));
                EditorGUILayout.PropertyField(aimPointOffsetProperty, new GUIContent("Muzzle / Tip Point Offset (Legacy Fallback)"));
                EditorGUILayout.PropertyField(projectileSpawnPointOffsetProperty, new GUIContent("Projectile Spawn Point Offset (Legacy Fallback)"));
                EditorGUILayout.PropertyField(slashVfxOffsetProperty, new GUIContent("Slash VFX Offset (Legacy Fallback)"));
                EditorGUILayout.PropertyField(localPositionOffsetProperty, new GUIContent("Local Position Offset (Legacy Unused)"));
                EditorGUILayout.PropertyField(flipBehaviorProperty, new GUIContent("Flip Behavior"));
            }
        }
    }

    private void DrawSpriteDiagnostics()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Sprite Diagnostics", EditorStyles.boldLabel);

        Sprite sprite = weaponDefinition != null ? weaponDefinition.ItemImage : null;
        if (sprite == null)
        {
            EditorGUILayout.HelpBox("No weapon sprite assigned.", MessageType.Warning);
            return;
        }

        SpriteOpaqueBoundsAnalysis analysis = SpriteContentBoundsUtility.Analyze(sprite);
        Rect rect = sprite.rect;
        RectInt opaque = analysis.OpaquePixelBounds;
        EditorGUILayout.LabelField("Sprite Rect", $"{Mathf.RoundToInt(rect.width)} x {Mathf.RoundToInt(rect.height)} px");
        EditorGUILayout.LabelField("Opaque Content", $"{opaque.width} x {opaque.height} px");
        EditorGUILayout.LabelField("Content Coverage", $"{analysis.Coverage01 * 100f:0.##}%");
        EditorGUILayout.LabelField("PPU", sprite.pixelsPerUnit.ToString("0.##"));
        EditorGUILayout.LabelField("Current Bounds Mode", previewBoundsMode.ToString());

        if (analysis.Coverage01 < 0.4f)
        {
            EditorGUILayout.HelpBox("Visible content covers less than 40% of the sprite rect. This sprite may look sparse or small even when the slice bounds are correct.", MessageType.Warning);
        }

        if (!string.IsNullOrEmpty(analysis.Warning))
        {
            EditorGUILayout.HelpBox(analysis.Warning, analysis.UsedFallbackBounds ? MessageType.Warning : MessageType.Info);
        }
    }

    private void DrawScaleDiagnostics()
    {
        scaleDiagnosticsFoldout = EditorGUILayout.Foldout(scaleDiagnosticsFoldout, "Scale Diagnostics", true);
        if (!scaleDiagnosticsFoldout || weaponDefinition == null || weaponDefinition.ItemImage == null)
        {
            return;
        }

        Vector2 anchor = GetPreviewWeaponAnchorWorld(out _);
        Vector2 aim = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        WeaponAlignmentPose pose = CalculatePreviewPose(anchor, aim);
        Sprite weaponSprite = weaponDefinition.ItemImage;
        Vector2 weaponBaseWorldSize = weaponSprite.bounds.size;
        Bounds weaponRenderedBounds = GetWeaponRenderedBounds(weaponSprite, pose);
        Vector2 finalWeaponWorldSize = ToSize(weaponRenderedBounds);
        Transform playerRoot = GetConfiguredPreviewPlayerRoot();
        SpriteRenderer playerRenderer = GetPreviewPlayerRenderer();
        Vector2 playerWorldSize = playerRenderer != null && playerRenderer.sprite != null
            ? GetSpriteWorldSize(playerRenderer.sprite, GetPreviewPlayerRendererLossyScale(playerRenderer))
            : Vector2.zero;
        Vector2 ratio = CalculateRatio(finalWeaponWorldSize, playerWorldSize);
        float heightRatio = playerWorldSize.y > 0.0001f ? finalWeaponWorldSize.y / playerWorldSize.y : 0f;

        EditorGUILayout.LabelField("Preview Player Source", GetPreviewPlayerSourceLabel());
        EditorGUILayout.LabelField("Sprite Pixel Size", $"{Mathf.RoundToInt(weaponSprite.rect.width)} x {Mathf.RoundToInt(weaponSprite.rect.height)} px");
        EditorGUILayout.LabelField("Sprite PPU", weaponSprite.pixelsPerUnit.ToString("0.##"));
        EditorGUILayout.LabelField("Weapon Base World Size", FormatVector2(weaponBaseWorldSize));
        EditorGUILayout.LabelField("Runtime Scale Multiplier", weaponDefinition.VisualScale.ToString("0.###"));
        EditorGUILayout.LabelField("Final Weapon World Size", FormatVector2(finalWeaponWorldSize));
        EditorGUILayout.LabelField("Player Reference World Size", playerRenderer != null ? FormatVector2(playerWorldSize) : "None");
        EditorGUILayout.LabelField("Weapon To Player Ratio", FormatVector2(ratio));
        EditorGUILayout.LabelField("Weapon / Player Height Ratio", heightRatio.ToString("0.###"));
        EditorGUILayout.LabelField("Preview Zoom", previewSurfaceMode == PreviewSurfaceMode.AuthoringView ? authoringPreviewZoom.ToString("0.##") : "1");
        EditorGUILayout.LabelField("Preview Frame Zoom", viewZoom.ToString("0.###"));
        EditorGUILayout.LabelField("Runtime Parent Scale", FormatVector(GetPreviewWeaponParentScale()));

        if (TryGetScenePixelsPerWorldUnit(out float scenePixelsPerWorldUnit, out string sceneScaleSource))
        {
            EditorGUILayout.LabelField("Scene Pixels Per Unit", $"{scenePixelsPerWorldUnit:0.###} ({sceneScaleSource})");
        }

        if (playerRoot == null)
        {
            EditorGUILayout.HelpBox("No player reference is available. Preview falls back to weapon-only behavior.", MessageType.Warning);
            return;
        }

        DrawScaleCalibrationDiagnostics(finalWeaponWorldSize, playerWorldSize, heightRatio);

        if (EditorApplication.isPlaying && TryGetRuntimeVisualSnapshot(out RuntimeVisualSnapshot runtimeSnapshot))
        {
            bool usingRuntimeReference = runtimeSnapshot.Player != null && playerRoot == runtimeSnapshot.Player.transform;
            if (!usingRuntimeReference)
            {
                EditorGUILayout.HelpBox("Preview player reference does not match the active runtime player. Enable 'Use Runtime Player Reference' to compare against the live scene object.", MessageType.Warning);
            }
            else
            {
                Vector2 runtimePlayerSize = runtimeSnapshot.BoundsReport.PlayerRenderedSize;
                if (!IsClose(playerWorldSize, runtimePlayerSize, 0.01f))
                {
                    EditorGUILayout.HelpBox($"Preview player scale differs from runtime player bounds. Preview {FormatVector2(playerWorldSize)} vs runtime {FormatVector2(runtimePlayerSize)}.", MessageType.Warning);
                }
            }
        }
    }

    private void DrawScaleCalibrationDiagnostics(Vector2 previewWeaponWorldSize, Vector2 previewPlayerWorldSize, float previewHeightRatio)
    {
        if (!WeaponVisualScaleCalibrationUtility.TryBuildReport(weaponDefinition, out WeaponVisualScaleCalibrationUtility.CalibrationReport report))
        {
            EditorGUILayout.HelpBox(report.Warning, MessageType.Warning);
            return;
        }

        float targetRatio = report.TargetHeightRatio;
        float suggestedScale = previewHeightRatio > 0.0001f
            ? report.CurrentVisualScale * targetRatio / previewHeightRatio
            : report.CurrentVisualScale;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Scale Calibration", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Resolved Archetype", report.Archetype.ToString());
        EditorGUILayout.LabelField("Recommended Ratio Range", report.RecommendedRange.ToString());
        EditorGUILayout.LabelField("Current Ratio", previewHeightRatio.ToString("0.###"));
        EditorGUILayout.LabelField("Target Ratio", targetRatio.ToString("0.###"));
        EditorGUILayout.LabelField("Suggested Runtime Scale Multiplier", suggestedScale.ToString("0.###"));
        EditorGUILayout.LabelField("Calibration Weapon World Size", FormatVector2(previewWeaponWorldSize));
        EditorGUILayout.LabelField("Calibration Player World Size", FormatVector2(previewPlayerWorldSize));

        if (!string.IsNullOrEmpty(report.WeaponRendererPath) || !string.IsNullOrEmpty(report.PlayerRendererPath))
        {
            EditorGUILayout.LabelField("Weapon Renderer Path", string.IsNullOrEmpty(report.WeaponRendererPath) ? "None" : report.WeaponRendererPath);
            EditorGUILayout.LabelField("Player Renderer Path", string.IsNullOrEmpty(report.PlayerRendererPath) ? "None" : report.PlayerRendererPath);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Suggested Scale To Selected Weapon"))
            {
                ApplySuggestedScaleToWeapon(report.Definition, suggestedScale);
            }

            if (GUILayout.Button("Apply Suggested Scale To Selected Weapons In Project Selection"))
            {
                ApplySuggestedScaleToProjectSelection();
            }
        }
    }

    private void ApplySuggestedScaleToWeapon(WeaponDefinitionSO definition, float suggestedScale)
    {
        if (definition == null)
        {
            return;
        }

        if (WeaponVisualScaleCalibrationUtility.ApplyVisualScale(definition, suggestedScale))
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetDefinition(definition);
        }
    }

    private void ApplySuggestedScaleToProjectSelection()
    {
        WeaponDefinitionSO[] selectedDefinitions = Selection.GetFiltered<WeaponDefinitionSO>(SelectionMode.Assets);
        if (selectedDefinitions.Length == 0)
        {
            EditorUtility.DisplayDialog("Weapon Scale Calibration", "Select one or more WeaponDefinitionSO assets in the Project window first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply Suggested Scale",
                $"Apply archetype-based suggested Runtime Scale Multiplier to {selectedDefinitions.Length} selected WeaponDefinitionSO assets?\n\nThis updates only WeaponDefinitionSO.visualScale.",
                "Apply",
                "Cancel"))
        {
            return;
        }

        int changedCount = 0;
        for (int i = 0; i < selectedDefinitions.Length; i++)
        {
            WeaponDefinitionSO definition = selectedDefinitions[i];
            if (definition == null
                || !WeaponVisualScaleCalibrationUtility.TryBuildReport(definition, out WeaponVisualScaleCalibrationUtility.CalibrationReport report))
            {
                continue;
            }

            if (!Mathf.Approximately(report.CurrentVisualScale, report.SuggestedVisualScale)
                && WeaponVisualScaleCalibrationUtility.ApplyVisualScale(definition, report.SuggestedVisualScale))
            {
                changedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Applied suggested weapon visual scale to {changedCount} selected WeaponDefinitionSO assets.");
        SetDefinition(weaponDefinition);
    }

    private void DrawPropertyWithHelp(SerializedProperty property, string label, string helpText)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(property, new GUIContent(label));
        if (GUILayout.Button(new GUIContent("i", helpText), EditorStyles.miniButton, GUILayout.Width(22f)))
        {
            EditorUtility.DisplayDialog(label, helpText, "OK");
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRigStatus()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Weapon Rig", EditorStyles.boldLabel);

        if (weaponDefinition == null || weaponDefinition.WeaponPrefab == null)
        {
            EditorGUILayout.HelpBox("WeaponDefinition has no WeaponPrefab. Runtime pose cannot resolve explicit rig points.", MessageType.Error);
            return;
        }

        WeaponRig rig = GetWeaponPrefabRig();
        if (rig == null)
        {
            string fallbackLabel = weaponDefinition != null && weaponDefinition.AlignmentPreset != null
                ? $"WeaponPrefab has no WeaponRig. Preview is using alignment preset '{weaponDefinition.AlignmentPreset.name}'."
                : "WeaponPrefab has no WeaponRig. Preview is using legacy WeaponDefinition fallback offsets until the prefab is migrated.";
            EditorGUILayout.HelpBox(fallbackLabel, MessageType.Warning);
            return;
        }

        bool hasRequiredPoints = weaponDefinition != null ? rig.HasRequiredPointsFor(weaponDefinition) : rig.HasAllRequiredPoints;
        MessageType messageType = hasRequiredPoints ? MessageType.Info : MessageType.Warning;
        string message = hasRequiredPoints
            ? "Preview is using WeaponRig child points from the runtime prefab."
            : "WeaponRig is missing required child points for this weapon archetype. Runtime will warn and fallback for missing data.";
        EditorGUILayout.HelpBox(message, messageType);
        EditorGUILayout.ObjectField("Rig", rig, typeof(WeaponRig), true);
    }

    private void DrawFooterStatus()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Current Mode", GetPreviewTitle());
        EditorGUILayout.LabelField("Source of Truth", GetSourceOfTruthLabel());
        EditorGUILayout.LabelField("Runtime Availability", GetRuntimeAvailabilityLabel());
        EditorGUILayout.HelpBox(GetValidationReminder(), workflowMode == WorkflowMode.ValidateGameView ? MessageType.Info : MessageType.Warning);
    }

    private string GetSourceOfTruthLabel()
    {
        if (IsMainCameraRenderView())
        {
            return "Main Camera RenderTexture";
        }

        if (workflowMode == WorkflowMode.PreviewRuntimeObject)
        {
            return "Actual runtime object transforms, diagnostic only";
        }

        return previewSurfaceMode == PreviewSurfaceMode.RuntimeView
            ? "Runtime pose via WeaponAlignmentUtility + WeaponRig at runtime-equivalent scale"
            : "Runtime pose via WeaponAlignmentUtility + WeaponRig with editor-only authoring zoom";
    }

    private string GetRuntimeAvailabilityLabel()
    {
        if (!EditorApplication.isPlaying)
        {
            return workflowMode == WorkflowMode.EditWeaponAlignment ? "Edit mode available; runtime object views require Play Mode" : "Play Mode required";
        }

        return TryGetRuntimeVisualSnapshot(out _) ? "Runtime player and active weapon visual found" : "Runtime player or active weapon visual not found";
    }

    private string GetValidationReminder()
    {
        return workflowMode switch
        {
            WorkflowMode.EditWeaponAlignment => previewSurfaceMode == PreviewSurfaceMode.AuthoringView
                ? "Authoring View: Preview Zoom and framing are editor-only."
                : "Runtime View: pose and scale are runtime-equivalent. Framing changes only the viewport.",
            WorkflowMode.PreviewRuntimeObject => "This mode confirms runtime objects and transforms. It is not final Game View validation.",
            WorkflowMode.ValidateGameView => "Official WYSIWYG validation. This mode renders the actual Main Camera into this viewport.",
            _ => string.Empty
        };
    }

    private static GameObject GetRuntimePlayerPrefab()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePlayerPrefabPath);
    }

    private void SetWorkflowMode(WorkflowMode nextMode)
    {
        workflowMode = nextMode;
        ApplyWorkflowDefaults();
        Repaint();
    }

    private void SetPreviewSurfaceMode(PreviewSurfaceMode nextMode)
    {
        previewSurfaceMode = nextMode;
        if (workflowMode == WorkflowMode.PreviewRuntimeObject || workflowMode == WorkflowMode.ValidateGameView)
        {
            workflowMode = WorkflowMode.EditWeaponAlignment;
        }

        ApplyPreviewSurfaceDefaults();
        Repaint();
    }

    private void ApplyWorkflowDefaults()
    {
        previewPlayerPrefab = GetRuntimePlayerPrefab();

        switch (workflowMode)
        {
            case WorkflowMode.EditWeaponAlignment:
                useActualRuntimeVisualForPreview = false;
                useGameCameraProjection = false;
                useMainCameraRenderPreview = false;
                useNormalizedCalibrationScale = false;
                showWeaponAnchor = true;
                showGripAimPoints = true;
                showRuntimePoseDebug = true;
                break;
            case WorkflowMode.PreviewRuntimeObject:
                useActualRuntimeVisualForPreview = true;
                useGameCameraProjection = false;
                useMainCameraRenderPreview = false;
                useNormalizedCalibrationScale = false;
                showWeaponAnchor = true;
                showGripAimPoints = true;
                showRuntimePoseDebug = true;
                break;
            case WorkflowMode.ValidateGameView:
                useActualRuntimeVisualForPreview = true;
                useGameCameraProjection = true;
                useMainCameraRenderPreview = true;
                useNormalizedCalibrationScale = false;
                showWeaponAnchor = false;
                showGripAimPoints = false;
                showRuntimePoseDebug = false;
                break;
        }

        ApplyPreviewSurfaceDefaults();
    }

    private void ApplyPreviewSurfaceDefaults()
    {
        if (workflowMode != WorkflowMode.EditWeaponAlignment)
        {
            return;
        }

        if (previewSurfaceMode == PreviewSurfaceMode.AuthoringView)
        {
            previewBoundsMode = SpriteBoundsCoordinateSpace.OpaqueContentBounds;
            boundsOverlayMode = BoundsOverlayMode.Both;
            markerDisplayMode = MarkerDisplayMode.RelevantOnly;
            showIrrelevantPoints = false;
            showWeaponSpriteGhost = true;
            showPlayerGhost = false;
            showFullPlayerBody = false;
            showRuntimeScaleComparison = false;
            showAdvancedDiagnostics = false;
        }
        else
        {
            previewBoundsMode = SpriteBoundsCoordinateSpace.FullSpriteBounds;
            boundsOverlayMode = BoundsOverlayMode.SelectedBounds;
            markerDisplayMode = MarkerDisplayMode.DotsOnly;
        }
    }

    private bool IsMainCameraRenderView()
    {
        return workflowMode == WorkflowMode.ValidateGameView && useActualRuntimeVisualForPreview && useMainCameraRenderPreview;
    }

    private bool IsManualProjectionDiagnostic()
    {
        return workflowMode == WorkflowMode.PreviewRuntimeObject && useActualRuntimeVisualForPreview && useGameCameraProjection && !useMainCameraRenderPreview;
    }

    private string GetPreviewTitle()
    {
        if (workflowMode == WorkflowMode.ValidateGameView)
        {
            return "Validate Game View";
        }

        if (workflowMode == WorkflowMode.PreviewRuntimeObject)
        {
            return "Runtime Diagnostic View";
        }

        return previewSurfaceMode == PreviewSurfaceMode.AuthoringView ? "Authoring View" : "Runtime View";
    }

    private string GetWorkflowTitle()
    {
        return workflowMode switch
        {
            WorkflowMode.EditWeaponAlignment => "Edit Weapon Alignment",
            WorkflowMode.PreviewRuntimeObject => "Preview Runtime Object",
            WorkflowMode.ValidateGameView => "Validate Game View",
            _ => workflowMode.ToString()
        };
    }

    private string GetWorkflowPurpose()
    {
        if (workflowMode == WorkflowMode.EditWeaponAlignment)
        {
            return previewSurfaceMode == PreviewSurfaceMode.AuthoringView
                ? "Editor-focused inspection view. Uses WeaponAlignmentUtility.CalculateWeaponPose() but allows editor-only Preview Zoom and clearer marker defaults."
                : "Runtime-equivalent inspection view. Uses WeaponAlignmentUtility.CalculateWeaponPose() with runtime scale and only viewport framing adjustments.";
        }

        return workflowMode switch
        {
            WorkflowMode.PreviewRuntimeObject => "Diagnostic Play Mode view of the actual runtime Player, WeaponRoot, WeaponAnchor, and CurrentWeaponVisual.",
            WorkflowMode.ValidateGameView => "Official WYSIWYG validation rendered from the actual Main Camera RenderTexture. This should match the Game view.",
            _ => string.Empty
        };
    }

    private void SetDefinition(WeaponDefinitionSO definition)
    {
        if (weaponDefinition == definition)
        {
            return;
        }

        weaponDefinition = definition;
        serializedDefinition = null;
        EnsureSerializedDefinition();
        activeDraggedPoint = null;

        if (weaponDefinition != null && workflowMode == WorkflowMode.EditWeaponAlignment && previewSurfaceMode == PreviewSurfaceMode.AuthoringView)
        {
            pendingFocusPoints = true;
        }
    }

    private void EnsureSerializedDefinition()
    {
        if (weaponDefinition == null || serializedDefinition != null)
        {
            return;
        }

        serializedDefinition = new SerializedObject(weaponDefinition);
        archetypeProperty = serializedDefinition.FindProperty("archetype");
        alignmentPresetProperty = serializedDefinition.FindProperty("alignmentPreset");
        handlingModeProperty = serializedDefinition.FindProperty("handlingMode");
        gripPointOffsetProperty = serializedDefinition.FindProperty("gripPointOffset");
        aimPointOffsetProperty = serializedDefinition.FindProperty("aimPointOffset");
        localRotationOffsetProperty = serializedDefinition.FindProperty("localRotationOffset");
        localPositionOffsetProperty = serializedDefinition.FindProperty("localPositionOffset");
        visualScaleProperty = serializedDefinition.FindProperty("visualScale");
        flipBehaviorProperty = serializedDefinition.FindProperty("flipBehavior");
        projectileSpawnPointOffsetProperty = serializedDefinition.FindProperty("projectileSpawnPointOffset");
        slashVfxOffsetProperty = serializedDefinition.FindProperty("slashVfxOffset");
    }

    private WeaponAlignmentPose CalculatePreviewPose(Vector2 weaponAnchorWorld, Vector2 aimDirection)
    {
        return WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchorWorld, aimDirection, weaponDefinition, GetWeaponPrefabRig());
    }

    private WeaponRig GetWeaponPrefabRig()
    {
        return weaponDefinition != null && weaponDefinition.WeaponPrefab != null
            ? weaponDefinition.WeaponPrefab.GetComponentInChildren<WeaponRig>(true)
            : null;
    }

    private void DrawPreviewViewport(Rect rect, WeaponHandlingMode mode)
    {
        lastPreviewBoundsWarning = null;
        lastViewportRect = rect;
        lastDetailContentRect = Rect.zero;
        lastDetailSpriteRect = Rect.zero;
        if (pendingFocusPoints && weaponDefinition != null)
        {
            pendingFocusPoints = false;
            FocusPoints();
        }
        HandleViewportInput(rect);
        EditorGUI.DrawRect(rect, new Color(0.09f, 0.09f, 0.09f));

        if (Event.current.type == EventType.Repaint)
        {
            GUI.BeginClip(rect);
            Rect localRect = new Rect(0f, 0f, rect.width, rect.height);
            DrawPreviewContents(localRect, mode);
            GUI.EndClip();
        }

        DrawViewportOverlay(rect);
        DrawWeaponDetailPanel(rect);
    }

    private void DrawPreviewContents(Rect viewportRect, WeaponHandlingMode mode)
    {
        hasLastWeaponGuiRect = false;
        hasLastPlayerGuiRect = false;
        hasLastPlayerWorldBounds = false;
        hasLastRuntimePlayerGuiRect = false;
        hasLastRuntimePlayerWorldBounds = false;
        hasLastActualRuntimeSnapshot = false;
        Vector2 weaponAnchorWorld = GetPreviewWeaponAnchorWorld(out bool foundAnchor);
        Vector2 aimDirection = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        WeaponAlignmentPose pose = CalculatePreviewPose(weaponAnchorWorld, aimDirection);
        bool hasAuthoringSnapshot = TryGetAuthoringPointSnapshot(out AuthoringPointSnapshot authoringSnapshot);
        bool usePointFirstAuthoring = previewSurfaceMode == PreviewSurfaceMode.AuthoringView && hasAuthoringSnapshot;

        bool usingCameraView = (IsMainCameraRenderView() || IsManualProjectionDiagnostic()) && EditorApplication.isPlaying;
        if (!usingCameraView)
        {
            DrawGrid(viewportRect);
        }
        else
        {
            DrawGameCameraLetterbox();
        }

        bool drewMainCameraRender = usingCameraView && useMainCameraRenderPreview && DrawMainCameraRenderPreview();
        if (drewMainCameraRender)
        {
            if (TryGetRuntimeVisualSnapshot(out RuntimeVisualSnapshot snapshot))
            {
                lastActualRuntimeSnapshot = snapshot;
                hasLastActualRuntimeSnapshot = true;
                DrawActualRuntimeMarkers();
            }

            return;
        }

        bool drewRuntime = workflowMode == WorkflowMode.PreviewRuntimeObject
            && useActualRuntimeVisualForPreview
            && EditorApplication.isPlaying
            && DrawActualRuntimePreview();
        if (drewRuntime)
        {
            DrawActualRuntimeMarkers();
            return;
        }

        if (!drewRuntime && showFullPlayerBody)
        {
            DrawPlayerPreview();
        }
        else if (!drewRuntime && showPlayerGhost)
        {
            DrawPlayerGhost();
        }

        Sprite sprite = weaponDefinition.ItemImage;
        if (!drewRuntime && sprite != null)
        {
            if (usePointFirstAuthoring)
            {
                DrawAuthoringPreview(sprite, authoringSnapshot);
                if (showAdvancedDiagnostics)
                {
                    DrawWeaponBoundsOverlays(sprite, pose);
                }
            }
            else
            {
                DrawWeaponSprite(sprite, pose);
                if (showAdvancedDiagnostics)
                {
                    DrawWeaponBoundsOverlays(sprite, pose);
                }
            }
        }

        Vector2 weaponAnchorGui = WorldToGui(weaponAnchorWorld);
        Vector2 gripPoint = WorldToGui(ToVector2(usePointFirstAuthoring ? authoringSnapshot.GripPointWorld : pose.GripPoint));
        Vector2 aimPoint = WorldToGui(ToVector2(usePointFirstAuthoring ? authoringSnapshot.TipPointWorld : pose.MuzzleTipPoint));
        Vector2 projectileSpawnPoint = WorldToGui(ToVector2(usePointFirstAuthoring ? authoringSnapshot.ProjectileSpawnPointWorld : pose.ProjectileSpawnPoint));
        Vector2 slashOrigin = WorldToGui(ToVector2(usePointFirstAuthoring ? authoringSnapshot.SlashOriginWorld : pose.SlashOrigin));
        Vector2 slashArcStart = WorldToGui(ToVector2(usePointFirstAuthoring ? authoringSnapshot.SlashArcStartWorld : pose.SlashArcStart));
        Vector2 slashArcEnd = WorldToGui(ToVector2(usePointFirstAuthoring ? authoringSnapshot.SlashArcEndWorld : pose.SlashArcEnd));
        Vector2 weaponPoint = WorldToGui(ToVector2(usePointFirstAuthoring ? authoringSnapshot.WeaponPosition : pose.WeaponPosition));
        Vector2 displayedAimDirection = usePointFirstAuthoring ? authoringSnapshot.AimDirection : pose.AimDirection;

        if (showRuntimePoseDebug)
        {
            DrawPlayerCenter(WorldToGui(Vector2.zero));
            DrawWeaponPosition(weaponPoint);
            DrawAimVector(weaponAnchorGui, displayedAimDirection);
            if (ShouldDrawProjectileLine())
            {
                DrawProjectileDirectionLine(projectileSpawnPoint, displayedAimDirection);
            }
            if (previewPlayerPrefab != null && !foundAnchor)
            {
                GUI.Label(new Rect(10f, 10f, viewportRect.width - 20f, 18f), "Runtime-created WeaponRoot/WeaponAnchor at Player origin.", EditorStyles.miniLabel);
            }
        }

        if (showWeaponAnchor)
        {
            DrawWeaponAnchor(weaponAnchorGui, "H");
        }

        if (showGripAimPoints)
        {
            DrawFilteredMarker(PreviewMarkerKind.GripPoint, gripPoint);
            DrawFilteredMarker(PreviewMarkerKind.TipPoint, aimPoint);
            DrawFilteredMarker(PreviewMarkerKind.ProjectileSpawnPoint, projectileSpawnPoint);
            DrawFilteredMarker(PreviewMarkerKind.SlashOrigin, slashOrigin);
            DrawFilteredMarker(PreviewMarkerKind.SlashArcStart, slashArcStart);
            DrawFilteredMarker(PreviewMarkerKind.SlashArcEnd, slashArcEnd);
        }

        if (mode == WeaponHandlingMode.SlashArc && ShouldDrawSlashArc())
        {
            DrawSlashArc(slashOrigin, slashArcStart, slashArcEnd);
        }

        if (usePointFirstAuthoring)
        {
            HandleMainCanvasPointDragging(authoringSnapshot);
        }
    }

    private void DrawGameCameraLetterbox()
    {
        Camera camera = GetGamePreviewCamera();
        if (camera == null)
        {
            return;
        }

        Rect contentRect = GetGameCameraPreviewContentRect(camera);
        EditorGUI.DrawRect(contentRect, new Color(0.11f, 0.11f, 0.11f));
        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, 0.18f);
        Handles.DrawAAPolyLine(
            1f,
            new Vector3(contentRect.xMin, contentRect.yMin),
            new Vector3(contentRect.xMax, contentRect.yMin),
            new Vector3(contentRect.xMax, contentRect.yMax),
            new Vector3(contentRect.xMin, contentRect.yMax),
            new Vector3(contentRect.xMin, contentRect.yMin));
        Handles.color = previous;
        Handles.EndGUI();
    }

    private bool DrawMainCameraRenderPreview()
    {
        Camera camera = GetGamePreviewCamera();
        if (camera == null || lastViewportRect.width <= 1f || lastViewportRect.height <= 1f)
        {
            return false;
        }

        Rect contentRect = GetGameCameraPreviewContentRect(camera);
        int width = Mathf.Max(1, camera.pixelWidth > 0 ? camera.pixelWidth : Mathf.RoundToInt(contentRect.width));
        int height = Mathf.Max(1, camera.pixelHeight > 0 ? camera.pixelHeight : Mathf.RoundToInt(contentRect.height));
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = previousActive;
            GUI.DrawTexture(contentRect, renderTexture, ScaleMode.StretchToFill, false);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
        }

        return true;
    }

    private void DrawActualRuntimeMarkers()
    {
        if (!hasLastActualRuntimeSnapshot)
        {
            return;
        }

        Camera gameCamera = IsMainCameraRenderView() || IsManualProjectionDiagnostic() ? GetGamePreviewCamera() : null;
        if (showRuntimePoseDebug)
        {
            DrawPlayerCenter(RuntimeWorldToPreviewGui(lastActualRuntimeSnapshot.Player.transform.position, gameCamera));
            DrawWeaponPosition(RuntimeWorldToPreviewGui(lastActualRuntimeSnapshot.WorldPosition, gameCamera));
        }

        if (showWeaponAnchor && lastActualRuntimeSnapshot.WeaponAnchor != null)
        {
            DrawWeaponAnchor(RuntimeWorldToPreviewGui(lastActualRuntimeSnapshot.WeaponAnchor.position, gameCamera));
        }
    }

    private void DrawViewportOverlay(Rect rect)
    {
        float warningHeight = string.IsNullOrEmpty(lastPreviewBoundsWarning) ? 0f : 18f;
        GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 60f - warningHeight, rect.width - 20f, 18f), BuildPreviewScaleDebug(), EditorStyles.miniLabel);
        string mode = GetPreviewTitle();
        string description = GetValidationReminder();
        string navigation = IsMainCameraRenderView()
            ? "Fit/zoom disabled"
            : previewSurfaceMode == PreviewSurfaceMode.AuthoringView
                ? $"Runtime Frame {viewZoom:0.###} x Preview Zoom {authoringPreviewZoom:0.##} | Pan with middle/right mouse, scroll to zoom"
                : $"Runtime Frame {viewZoom:0.###} | Pan with middle/right mouse, scroll to zoom";
        GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 42f - warningHeight, rect.width - 20f, 18f), $"{mode} | {navigation}", EditorStyles.miniLabel);
        GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 24f - warningHeight, rect.width - 20f, 18f), description, EditorStyles.miniLabel);
        if (!string.IsNullOrEmpty(lastPreviewBoundsWarning))
        {
            GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 24f, rect.width - 20f, 18f), lastPreviewBoundsWarning, EditorStyles.miniLabel);
        }
    }

    private void DrawWeaponDetailPanel(Rect rect)
    {
        if (!showWeaponDetailPanel || weaponDefinition == null || weaponDefinition.ItemImage == null)
        {
            return;
        }

        Sprite sprite = weaponDefinition.ItemImage;
        const float panelWidth = 220f;
        const float panelHeight = 220f;
        Rect panelRect = new Rect(rect.xMax - panelWidth - 12f, rect.y + 12f, panelWidth, panelHeight);
        EditorGUI.DrawRect(panelRect, new Color(0.07f, 0.07f, 0.07f, 0.94f));
        GUI.Box(panelRect, GUIContent.none);
        GUI.Label(new Rect(panelRect.x + 8f, panelRect.y + 6f, panelRect.width - 16f, 18f), "Weapon Detail", EditorStyles.boldLabel);
        GUI.Label(new Rect(panelRect.x + 8f, panelRect.y + 24f, panelRect.width - 16f, 18f), $"{Mathf.RoundToInt(sprite.rect.width)}x{Mathf.RoundToInt(sprite.rect.height)} px @ {sprite.pixelsPerUnit:0.##} PPU", EditorStyles.miniLabel);

        Rect contentRect = new Rect(panelRect.x + 8f, panelRect.y + 44f, panelRect.width - 16f, panelRect.height - 52f);
        lastDetailContentRect = contentRect;
        DrawSpriteDetail(sprite, contentRect);
    }

    private void DrawSpriteDetail(Sprite sprite, Rect rect)
    {
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        EditorGUI.DrawRect(rect, new Color(0.11f, 0.11f, 0.11f));
        Rect textureRect = sprite.textureRect;
        float drawWidth = Mathf.Min(rect.width - 8f, textureRect.width * weaponDetailZoom);
        float drawHeight = Mathf.Min(rect.height - 8f, textureRect.height * weaponDetailZoom);
        float scale = Mathf.Min(drawWidth / textureRect.width, drawHeight / textureRect.height);
        float width = textureRect.width * scale;
        float height = textureRect.height * scale;
        Rect drawRect = new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
        lastDetailSpriteRect = drawRect;
        Rect uv = new Rect(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height, textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv, true);

        if (previewSurfaceMode == PreviewSurfaceMode.AuthoringView && TryGetAuthoringPointSnapshot(out AuthoringPointSnapshot snapshot))
        {
            DrawDetailPointOverlays(snapshot, drawRect);
            HandleDetailPointDragging(snapshot, drawRect);
        }
    }

    private bool TryGetAuthoringPointSnapshot(out AuthoringPointSnapshot snapshot)
    {
        snapshot = default;
        if (weaponDefinition == null || weaponDefinition.ItemImage == null || weaponDefinition.AlignmentPreset == null)
        {
            return false;
        }

        Sprite sprite = weaponDefinition.ItemImage;
        WeaponAlignmentPreset preset = weaponDefinition.AlignmentPreset;
        if (!preset.TryBuildPoints(sprite, out WeaponAlignmentPresetPoints presetPoints))
        {
            return false;
        }

        if (!SpriteContentBoundsUtility.TryGetLocalBounds(sprite, preset.CoordinateSpace, out Bounds authoringBounds, out _))
        {
            return false;
        }

        Vector2 anchorWorld = GetPreviewWeaponAnchorWorld(out _);
        Vector2 aimDirection = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            aimDirection = Vector2.right;
        }

        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg + weaponDefinition.LocalRotationOffset.z);
        Vector3 visualScale = WeaponAlignmentUtility.CalculateVisualScale(aimDirection, weaponDefinition);
        Vector3 weaponPosition = (Vector3)anchorWorld - rotation * Vector3.Scale(presetPoints.GripPoint, visualScale);

        snapshot = new AuthoringPointSnapshot
        {
            IsValid = true,
            Preset = preset,
            Sprite = sprite,
            SpriteBounds = sprite.bounds,
            AuthoringBounds = authoringBounds,
            GripPointLocal = presetPoints.GripPoint,
            TipPointLocal = presetPoints.TipPoint,
            ProjectileSpawnPointLocal = presetPoints.ProjectileSpawnPoint,
            SlashOriginLocal = presetPoints.SlashOrigin,
            SlashArcStartLocal = presetPoints.SlashArcStart,
            SlashArcEndLocal = presetPoints.SlashArcEnd,
            WeaponPosition = weaponPosition,
            GripPointWorld = TransformAuthoringLocalPoint(weaponPosition, rotation, presetPoints.GripPoint, visualScale),
            TipPointWorld = TransformAuthoringLocalPoint(weaponPosition, rotation, presetPoints.TipPoint, visualScale),
            ProjectileSpawnPointWorld = TransformAuthoringLocalPoint(weaponPosition, rotation, presetPoints.ProjectileSpawnPoint, visualScale),
            SlashOriginWorld = TransformAuthoringLocalPoint(weaponPosition, rotation, presetPoints.SlashOrigin, visualScale),
            SlashArcStartWorld = TransformAuthoringLocalPoint(weaponPosition, rotation, presetPoints.SlashArcStart, visualScale),
            SlashArcEndWorld = TransformAuthoringLocalPoint(weaponPosition, rotation, presetPoints.SlashArcEnd, visualScale),
            WeaponAnchorWorld = anchorWorld,
            AimDirection = aimDirection.normalized,
            Rotation = rotation,
            VisualScale = visualScale
        };
        return true;
    }

    private static Vector3 TransformAuthoringLocalPoint(Vector3 weaponPosition, Quaternion rotation, Vector3 localPoint, Vector3 visualScale)
    {
        return weaponPosition + rotation * Vector3.Scale(localPoint, visualScale);
    }

    private void DrawAuthoringPreview(Sprite sprite, AuthoringPointSnapshot snapshot)
    {
        if (showWeaponSpriteGhost)
        {
            Rect guiRect = DrawSpriteRendererLikeRuntime(
                sprite,
                ToVector2(snapshot.WeaponPosition),
                snapshot.Rotation.eulerAngles.z,
                snapshot.VisualScale);
            lastWeaponGuiRect = guiRect;
            hasLastWeaponGuiRect = true;
            lastWeaponWorldBounds = GetSpriteRendererLikeWorldBounds(sprite, snapshot.WeaponPosition, snapshot.Rotation, snapshot.VisualScale);
        }
    }

    private void DrawPlayerGhost()
    {
        SpriteRenderer playerRenderer = GetPreviewPlayerRenderer();
        if (playerRenderer == null || playerRenderer.sprite == null)
        {
            return;
        }

        Vector2 playerWorldSize = GetSpriteWorldSize(playerRenderer.sprite, GetPreviewPlayerRendererLossyScale(playerRenderer));
        Vector2 center = WorldToGui(Vector2.zero);
        Vector2 size = new Vector2(
            playerWorldSize.x * BasePixelsPerWorldUnit * GetEffectiveViewZoom(),
            playerWorldSize.y * BasePixelsPerWorldUnit * GetEffectiveViewZoom());
        Rect ghostRect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);

        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = new Color(0.7f, 0.85f, 1f, 0.5f);
        Handles.DrawAAPolyLine(
            2f,
            new Vector3(ghostRect.xMin, ghostRect.yMin),
            new Vector3(ghostRect.xMax, ghostRect.yMin),
            new Vector3(ghostRect.xMax, ghostRect.yMax),
            new Vector3(ghostRect.xMin, ghostRect.yMax),
            new Vector3(ghostRect.xMin, ghostRect.yMin));
        Handles.color = previous;
        Handles.EndGUI();

        GUI.Label(new Rect(ghostRect.xMin + 6f, ghostRect.yMin + 4f, 120f, 18f), "Player Ghost", EditorStyles.miniLabel);
    }

    private void DrawDetailPointOverlays(AuthoringPointSnapshot snapshot, Rect drawRect)
    {
        DrawDetailMarker(snapshot, drawRect, new EditableAuthoringPoint(PreviewMarkerKind.GripPoint, "normalizedGripPoint"));
        DrawDetailMarker(snapshot, drawRect, new EditableAuthoringPoint(PreviewMarkerKind.TipPoint, "normalizedTipPoint"));

        if (IsRangedAuthoringArchetype())
        {
            DrawDetailMarker(snapshot, drawRect, new EditableAuthoringPoint(PreviewMarkerKind.ProjectileSpawnPoint, "normalizedProjectileSpawnPoint"));
        }

        if (IsMeleeAuthoringArchetype())
        {
            DrawDetailMarker(snapshot, drawRect, new EditableAuthoringPoint(PreviewMarkerKind.SlashOrigin, "normalizedSlashOrigin"));
            DrawDetailMarker(snapshot, drawRect, new EditableAuthoringPoint(PreviewMarkerKind.SlashArcStart, "normalizedSlashArcStart"));
            DrawDetailMarker(snapshot, drawRect, new EditableAuthoringPoint(PreviewMarkerKind.SlashArcEnd, "normalizedSlashArcEnd"));
        }
    }

    private void DrawDetailMarker(AuthoringPointSnapshot snapshot, Rect drawRect, EditableAuthoringPoint editablePoint)
    {
        Vector2 position = GetDetailPointGui(snapshot, drawRect, editablePoint.MarkerKind);
        DrawMarker(position, GetMarkerColor(editablePoint.MarkerKind, true), GetMarkerLabel(editablePoint.MarkerKind), GetMarkerLabelOffset(editablePoint.MarkerKind), true);
    }

    private void HandleDetailPointDragging(AuthoringPointSnapshot snapshot, Rect drawRect)
    {
        if (!drawRect.Contains(Event.current.mousePosition) || authoringEditTarget != AuthoringEditTarget.EditPreset)
        {
            if (Event.current.type == EventType.MouseUp && activeDraggedPoint.HasValue)
            {
                activeDraggedPoint = null;
                AssetDatabase.SaveAssets();
                Repaint();
            }
            return;
        }

        HandlePointDragging(
            snapshot,
            FindNearestEditablePoint(snapshot, Event.current.mousePosition, point => GetDetailPointGui(snapshot, drawRect, point.MarkerKind)),
            point => UpdatePresetPointFromDetail(snapshot, drawRect, point, Event.current.mousePosition));
    }

    private void HandleMainCanvasPointDragging(AuthoringPointSnapshot snapshot)
    {
        if (lastDetailContentRect.Contains(Event.current.mousePosition))
        {
            return;
        }

        HandlePointDragging(
            snapshot,
            FindNearestEditablePoint(snapshot, Event.current.mousePosition, point => WorldToGui(GetAuthoringWorldPoint(snapshot, point.MarkerKind))),
            point => UpdatePresetPointFromWorld(snapshot, point, GuiToWorld(Event.current.mousePosition)));
    }

    private void HandlePointDragging(AuthoringPointSnapshot snapshot, EditableAuthoringPoint? hoveredPoint, Action<EditableAuthoringPoint> applyDrag)
    {
        Event evt = Event.current;
        if (authoringEditTarget != AuthoringEditTarget.EditPreset || !snapshot.IsValid)
        {
            return;
        }

        if (evt.type == EventType.MouseDown && evt.button == 0 && hoveredPoint.HasValue)
        {
            activeDraggedPoint = hoveredPoint;
            evt.Use();
        }
        else if (evt.type == EventType.MouseDrag && evt.button == 0 && activeDraggedPoint.HasValue)
        {
            applyDrag(activeDraggedPoint.Value);
            evt.Use();
            Repaint();
        }
        else if (evt.type == EventType.MouseUp && activeDraggedPoint.HasValue)
        {
            activeDraggedPoint = null;
            AssetDatabase.SaveAssets();
            evt.Use();
        }
    }

    private EditableAuthoringPoint? FindNearestEditablePoint(AuthoringPointSnapshot snapshot, Vector2 mousePosition, Func<EditableAuthoringPoint, Vector2> getGuiPosition)
    {
        EditableAuthoringPoint[] editablePoints = GetRelevantEditablePoints();
        for (int i = 0; i < editablePoints.Length; i++)
        {
            EditableAuthoringPoint point = editablePoints[i];
            if (Vector2.Distance(mousePosition, getGuiPosition(point)) <= 12f)
            {
                return point;
            }
        }

        return null;
    }

    private EditableAuthoringPoint[] GetRelevantEditablePoints()
    {
        if (IsRangedAuthoringArchetype())
        {
            return new[]
            {
                new EditableAuthoringPoint(PreviewMarkerKind.GripPoint, "normalizedGripPoint"),
                new EditableAuthoringPoint(PreviewMarkerKind.TipPoint, "normalizedTipPoint"),
                new EditableAuthoringPoint(PreviewMarkerKind.ProjectileSpawnPoint, "normalizedProjectileSpawnPoint")
            };
        }

        return new[]
        {
            new EditableAuthoringPoint(PreviewMarkerKind.GripPoint, "normalizedGripPoint"),
            new EditableAuthoringPoint(PreviewMarkerKind.TipPoint, "normalizedTipPoint"),
            new EditableAuthoringPoint(PreviewMarkerKind.SlashOrigin, "normalizedSlashOrigin"),
            new EditableAuthoringPoint(PreviewMarkerKind.SlashArcStart, "normalizedSlashArcStart"),
            new EditableAuthoringPoint(PreviewMarkerKind.SlashArcEnd, "normalizedSlashArcEnd")
        };
    }

    private Vector3 GetAuthoringWorldPoint(AuthoringPointSnapshot snapshot, PreviewMarkerKind kind)
    {
        return kind switch
        {
            PreviewMarkerKind.GripPoint => snapshot.GripPointWorld,
            PreviewMarkerKind.TipPoint => snapshot.TipPointWorld,
            PreviewMarkerKind.ProjectileSpawnPoint => snapshot.ProjectileSpawnPointWorld,
            PreviewMarkerKind.SlashOrigin => snapshot.SlashOriginWorld,
            PreviewMarkerKind.SlashArcStart => snapshot.SlashArcStartWorld,
            PreviewMarkerKind.SlashArcEnd => snapshot.SlashArcEndWorld,
            _ => snapshot.GripPointWorld
        };
    }

    private Vector3 GetAuthoringLocalPoint(AuthoringPointSnapshot snapshot, PreviewMarkerKind kind)
    {
        return kind switch
        {
            PreviewMarkerKind.GripPoint => snapshot.GripPointLocal,
            PreviewMarkerKind.TipPoint => snapshot.TipPointLocal,
            PreviewMarkerKind.ProjectileSpawnPoint => snapshot.ProjectileSpawnPointLocal,
            PreviewMarkerKind.SlashOrigin => snapshot.SlashOriginLocal,
            PreviewMarkerKind.SlashArcStart => snapshot.SlashArcStartLocal,
            PreviewMarkerKind.SlashArcEnd => snapshot.SlashArcEndLocal,
            _ => snapshot.GripPointLocal
        };
    }

    private Vector2 GetDetailPointGui(AuthoringPointSnapshot snapshot, Rect drawRect, PreviewMarkerKind kind)
    {
        Vector3 localPoint = GetAuthoringLocalPoint(snapshot, kind);
        Bounds spriteBounds = snapshot.SpriteBounds;
        float normalizedX = Mathf.InverseLerp(spriteBounds.min.x, spriteBounds.max.x, localPoint.x);
        float normalizedY = Mathf.InverseLerp(spriteBounds.min.y, spriteBounds.max.y, localPoint.y);
        return new Vector2(
            Mathf.Lerp(drawRect.xMin, drawRect.xMax, normalizedX),
            Mathf.Lerp(drawRect.yMax, drawRect.yMin, normalizedY));
    }

    private void UpdatePresetPointFromDetail(AuthoringPointSnapshot snapshot, Rect drawRect, EditableAuthoringPoint point, Vector2 mousePosition)
    {
        Bounds spriteBounds = snapshot.SpriteBounds;
        Vector2 spriteNormalized = new Vector2(
            Mathf.InverseLerp(drawRect.xMin, drawRect.xMax, mousePosition.x),
            Mathf.InverseLerp(drawRect.yMax, drawRect.yMin, mousePosition.y));
        Vector3 localPoint = new Vector3(
            Mathf.Lerp(spriteBounds.min.x, spriteBounds.max.x, spriteNormalized.x),
            Mathf.Lerp(spriteBounds.min.y, spriteBounds.max.y, spriteNormalized.y),
            0f);
        UpdatePresetPointFromLocal(snapshot, point.PresetPropertyName, localPoint);
    }

    private void UpdatePresetPointFromWorld(AuthoringPointSnapshot snapshot, EditableAuthoringPoint point, Vector2 worldPosition)
    {
        Vector3 localScaled = Quaternion.Inverse(snapshot.Rotation) * ((Vector3)worldPosition - snapshot.WeaponPosition);
        Vector3 localPoint = new Vector3(
            DivideScaleAxis(localScaled.x, snapshot.VisualScale.x),
            DivideScaleAxis(localScaled.y, snapshot.VisualScale.y),
            0f);
        UpdatePresetPointFromLocal(snapshot, point.PresetPropertyName, localPoint);
    }

    private void UpdatePresetPointFromLocal(AuthoringPointSnapshot snapshot, string propertyName, Vector3 localPoint)
    {
        if (snapshot.Preset == null)
        {
            return;
        }

        Bounds bounds = snapshot.AuthoringBounds;
        float normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, localPoint.x);
        float normalizedY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, localPoint.y);

        SerializedObject presetObject = new SerializedObject(snapshot.Preset);
        SerializedProperty property = presetObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        presetObject.Update();
        property.vector2Value = new Vector2(Mathf.Clamp01(normalizedX), Mathf.Clamp01(normalizedY));
        presetObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(snapshot.Preset);
    }

    private Vector3 GetCurrentPoseScale()
    {
        if (weaponDefinition == null)
        {
            return Vector3.one;
        }

        Vector2 aimDirection = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        return WeaponAlignmentUtility.CalculateVisualScale(aimDirection, weaponDefinition);
    }

    private void HandleViewportInput(Rect rect)
    {
        if ((IsMainCameraRenderView() || IsManualProjectionDiagnostic()) && EditorApplication.isPlaying)
        {
            return;
        }

        Event evt = Event.current;
        if (!rect.Contains(evt.mousePosition))
        {
            return;
        }

        if (evt.type == EventType.ScrollWheel)
        {
            float oldZoom = viewZoom;
            float zoomFactor = Mathf.Exp(-evt.delta.y * 0.08f);
            viewZoom = Mathf.Clamp(viewZoom * zoomFactor, 0.02f, 8f);
            Vector2 localMouse = evt.mousePosition - rect.position;
            Vector2 pivot = localMouse - rect.size * 0.5f - viewPan;
            viewPan -= pivot * (viewZoom / oldZoom - 1f);
            evt.Use();
            Repaint();
        }
        else if ((evt.type == EventType.MouseDrag) && (evt.button == 2 || evt.button == 1))
        {
            viewPan += evt.delta;
            evt.Use();
            Repaint();
        }
    }

    private void FitAll()
    {
        FitBounds(CalculatePreviewBounds(true, true));
    }

    private void FocusPoints()
    {
        showWeaponDetailPanel = true;
        FitBounds(CalculatePointFocusedBounds());
    }

    private void FocusWeaponDetail()
    {
        showWeaponDetailPanel = true;
        FitWeapon();
    }

    private void FocusRuntimeComparison()
    {
        showRuntimeScaleComparison = true;
        showAdvancedDiagnostics = true;
        advancedFoldout = true;
    }

    private void FitPlayer()
    {
        FitBounds(CalculatePreviewBounds(true, false));
    }

    private void FitWeapon()
    {
        FitBounds(CalculatePreviewBounds(false, true));
    }

    private void ResetView()
    {
        viewZoom = 1f;
        viewPan = Vector2.zero;
        Repaint();
    }

    private void SetOneToOneRuntimeScale()
    {
        MatchRuntimeSceneScale();
    }

    private void MatchRuntimeSceneScale()
    {
        if (!TryGetScenePixelsPerWorldUnit(out float scenePixelsPerWorldUnit, out _))
        {
            viewZoom = 1f;
            if (previewSurfaceMode == PreviewSurfaceMode.AuthoringView)
            {
                authoringPreviewZoom = 1f;
            }

            viewPan = Vector2.zero;
            Repaint();
            return;
        }

        authoringPreviewZoom = 1f;
        viewZoom = Mathf.Clamp(scenePixelsPerWorldUnit / BasePixelsPerWorldUnit, 0.02f, 8f);
        viewPan = Vector2.zero;
        Repaint();
    }

    private void FitBounds(Bounds bounds)
    {
        if (lastViewportRect.width <= 1f || lastViewportRect.height <= 1f || bounds.size.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float availableWidth = Mathf.Max(1f, lastViewportRect.width - 80f);
        float availableHeight = Mathf.Max(1f, lastViewportRect.height - 80f);
        float zoomX = availableWidth / (bounds.size.x * BasePixelsPerWorldUnit);
        float zoomY = availableHeight / (bounds.size.y * BasePixelsPerWorldUnit);
        viewZoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY) / GetAuthoringZoomFactor(), 0.02f, 8f);
        viewPan = -WorldToGuiOffset(bounds.center);
        Repaint();
    }

    private Bounds CalculatePreviewBounds(bool includePlayer, bool includeWeapon)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 0.1f);

        Transform previewRoot = GetConfiguredPreviewPlayerRoot();
        if (includePlayer && previewRoot != null)
        {
            foreach (SpriteRenderer renderer in previewRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || renderer.sprite == null || !renderer.enabled || IsUnderCurrentWeaponVisual(renderer.transform))
                {
                    continue;
                }

                Encapsulate(ref bounds, ref hasBounds, GetSpriteBounds(renderer.sprite, renderer.transform.position - previewRoot.position, GetPreviewPlayerRendererLossyScale(renderer)));
            }
        }
        else if (includePlayer && previewPlayerSprite != null)
        {
            Encapsulate(ref bounds, ref hasBounds, GetSpriteBounds(previewPlayerSprite, Vector3.zero, Vector3.one));
        }

        if (includeWeapon && weaponDefinition != null && weaponDefinition.ItemImage != null)
        {
            Vector2 anchor = GetPreviewWeaponAnchorWorld(out _);
            Vector2 aim = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
            WeaponAlignmentPose pose = CalculatePreviewPose(anchor, aim);
            Encapsulate(ref bounds, ref hasBounds, GetWeaponPreviewBounds(weaponDefinition.ItemImage, pose));
            Encapsulate(ref bounds, ref hasBounds, new Bounds(pose.GripPoint, Vector3.one * 0.2f));
            Encapsulate(ref bounds, ref hasBounds, new Bounds(pose.MuzzleTipPoint, Vector3.one * 0.2f));
            Encapsulate(ref bounds, ref hasBounds, new Bounds(pose.ProjectileSpawnPoint, Vector3.one * 0.2f));
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
    }

    private Bounds CalculatePointFocusedBounds()
    {
        if (!TryGetAuthoringPointSnapshot(out AuthoringPointSnapshot snapshot))
        {
            return CalculatePreviewBounds(false, true);
        }

        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
        Encapsulate(ref bounds, ref hasBounds, new Bounds(new Vector3(snapshot.WeaponAnchorWorld.x, snapshot.WeaponAnchorWorld.y, 0f), Vector3.one * 0.18f));
        Encapsulate(ref bounds, ref hasBounds, new Bounds(snapshot.GripPointWorld, Vector3.one * 0.18f));
        Encapsulate(ref bounds, ref hasBounds, new Bounds(snapshot.TipPointWorld, Vector3.one * 0.18f));

        if (IsRangedAuthoringArchetype())
        {
            Encapsulate(ref bounds, ref hasBounds, new Bounds(snapshot.ProjectileSpawnPointWorld, Vector3.one * 0.18f));
        }

        if (IsMeleeAuthoringArchetype())
        {
            Encapsulate(ref bounds, ref hasBounds, new Bounds(snapshot.SlashOriginWorld, Vector3.one * 0.18f));
            Encapsulate(ref bounds, ref hasBounds, new Bounds(snapshot.SlashArcStartWorld, Vector3.one * 0.18f));
            Encapsulate(ref bounds, ref hasBounds, new Bounds(snapshot.SlashArcEndWorld, Vector3.one * 0.18f));
        }

        return hasBounds ? bounds : CalculatePreviewBounds(false, true);
    }

    private static void Encapsulate(ref Bounds bounds, ref bool hasBounds, Bounds next)
    {
        if (!hasBounds)
        {
            bounds = next;
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(next);
    }

    private static void EncapsulateGuiRect(ref Rect rect, ref bool hasRect, Rect next)
    {
        if (!hasRect)
        {
            rect = next;
            hasRect = true;
            return;
        }

        float xMin = Mathf.Min(rect.xMin, next.xMin);
        float yMin = Mathf.Min(rect.yMin, next.yMin);
        float xMax = Mathf.Max(rect.xMax, next.xMax);
        float yMax = Mathf.Max(rect.yMax, next.yMax);
        rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Bounds GetSpriteBounds(Sprite sprite, Vector3 center, Vector3 scale)
    {
        Vector3 boundsCenter = center + Vector3.Scale(sprite.bounds.center, scale);
        Vector3 size = new Vector3(
            sprite.bounds.size.x * Mathf.Abs(scale.x),
            sprite.bounds.size.y * Mathf.Abs(scale.y),
            0.1f);
        return new Bounds(boundsCenter, size);
    }

    private Bounds GetSpriteBounds(Sprite sprite, Vector3 center, Vector3 scale, SpriteBoundsCoordinateSpace coordinateSpace)
    {
        if (sprite == null)
        {
            return new Bounds(center, Vector3.zero);
        }

        if (!SpriteContentBoundsUtility.TryGetLocalBounds(sprite, coordinateSpace, out Bounds localBounds, out string warning))
        {
            return GetSpriteBounds(sprite, center, scale);
        }

        if (!string.IsNullOrEmpty(warning))
        {
            lastPreviewBoundsWarning = warning;
        }

        Vector3 boundsCenter = center + Vector3.Scale(localBounds.center, scale);
        Vector3 size = new Vector3(
            localBounds.size.x * Mathf.Abs(scale.x),
            localBounds.size.y * Mathf.Abs(scale.y),
            0.1f);
        return new Bounds(boundsCenter, size);
    }

    private Bounds GetWeaponPreviewBounds(Sprite sprite, WeaponAlignmentPose pose)
    {
        ResolveWeaponRendererPreviewTransform(sprite, pose, out Vector3 rendererPosition, out Quaternion rendererRotation, out Vector3 rendererScale);
        if (SpriteContentBoundsUtility.TryGetLocalBounds(sprite, previewBoundsMode, out Bounds localBounds, out string warning))
        {
            if (!string.IsNullOrEmpty(warning))
            {
                lastPreviewBoundsWarning = warning;
            }

            return GetLocalSpriteRendererWorldBounds(localBounds, rendererPosition, rendererRotation, rendererScale);
        }

        return GetSpriteRendererLikeWorldBounds(sprite, rendererPosition, rendererRotation, rendererScale);
    }

    private static Bounds GetLocalSpriteRendererWorldBounds(Bounds localBounds, Vector3 transformWorldPosition, Quaternion rotation, Vector3 lossyScale)
    {
        Vector3 min = localBounds.min;
        Vector3 max = localBounds.max;
        Bounds bounds = default;
        bool hasBounds = false;
        EncapsulatePoint(ref bounds, ref hasBounds, TransformSpriteLocalCorner(min.x, min.y, transformWorldPosition, rotation, lossyScale));
        EncapsulatePoint(ref bounds, ref hasBounds, TransformSpriteLocalCorner(min.x, max.y, transformWorldPosition, rotation, lossyScale));
        EncapsulatePoint(ref bounds, ref hasBounds, TransformSpriteLocalCorner(max.x, min.y, transformWorldPosition, rotation, lossyScale));
        EncapsulatePoint(ref bounds, ref hasBounds, TransformSpriteLocalCorner(max.x, max.y, transformWorldPosition, rotation, lossyScale));
        if (!hasBounds)
        {
            return new Bounds(transformWorldPosition, Vector3.zero);
        }

        return bounds;
    }

    private static Bounds GetSpriteRendererLikeWorldBounds(Sprite sprite, Vector3 transformWorldPosition, Quaternion rotation, Vector3 lossyScale)
    {
        Vector3 min = sprite.bounds.min;
        Vector3 max = sprite.bounds.max;
        Bounds bounds = default;
        bool hasBounds = false;
        EncapsulatePoint(ref bounds, ref hasBounds, TransformSpriteLocalCorner(min.x, min.y, transformWorldPosition, rotation, lossyScale));
        EncapsulatePoint(ref bounds, ref hasBounds, TransformSpriteLocalCorner(min.x, max.y, transformWorldPosition, rotation, lossyScale));
        EncapsulatePoint(ref bounds, ref hasBounds, TransformSpriteLocalCorner(max.x, min.y, transformWorldPosition, rotation, lossyScale));
        EncapsulatePoint(ref bounds, ref hasBounds, TransformSpriteLocalCorner(max.x, max.y, transformWorldPosition, rotation, lossyScale));
        return bounds;
    }

    private static void EncapsulatePoint(ref Bounds bounds, ref bool hasBounds, Vector3 point)
    {
        if (!hasBounds)
        {
            bounds = new Bounds(point, Vector3.zero);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(point);
    }

    private static Vector3 TransformSpriteLocalCorner(float x, float y, Vector3 transformWorldPosition, Quaternion rotation, Vector3 lossyScale)
    {
        Vector3 local = new Vector3(x * lossyScale.x, y * lossyScale.y, 0f);
        return transformWorldPosition + rotation * local;
    }

    private Rect DrawSpriteRendererLikeRuntime(Sprite sprite, Vector2 transformWorldPosition, float rotation, Vector3 lossyScale)
    {
        Vector2 transformGuiPosition = WorldToGui(transformWorldPosition);
        Rect spriteRect = CalculateSpriteGuiRect(sprite, transformWorldPosition, lossyScale);

        Texture2D texture = sprite.texture;
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height, textureRect.width / texture.width, textureRect.height / texture.height);

        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(-rotation, transformGuiPosition);
        if (lossyScale.x < 0f || lossyScale.y < 0f)
        {
            GUIUtility.ScaleAroundPivot(new Vector2(lossyScale.x < 0f ? -1f : 1f, lossyScale.y < 0f ? -1f : 1f), transformGuiPosition);
        }
        GUI.DrawTextureWithTexCoords(spriteRect, texture, uv, true);
        GUI.matrix = previousMatrix;
        return spriteRect;
    }

    private Rect CalculateSpriteGuiRect(Sprite sprite, Vector2 transformWorldPosition, Vector3 lossyScale)
    {
        Vector2 transformGuiPosition = WorldToGui(transformWorldPosition);
        Vector2 boundsCenterGuiOffset = WorldToGuiOffset(ToVector2(Vector3.Scale(sprite.bounds.center, lossyScale)));
        Vector2 boundsCenterGui = transformGuiPosition + boundsCenterGuiOffset;
        float effectiveZoom = GetEffectiveViewZoom();
        float width = sprite.bounds.size.x * BasePixelsPerWorldUnit * effectiveZoom * Mathf.Abs(lossyScale.x);
        float height = sprite.bounds.size.y * BasePixelsPerWorldUnit * effectiveZoom * Mathf.Abs(lossyScale.y);
        return new Rect(boundsCenterGui.x - width * 0.5f, boundsCenterGui.y - height * 0.5f, width, height);
    }

    private Rect DrawSpriteRendererWithGameCamera(SpriteRenderer renderer, Camera camera)
    {
        Sprite sprite = renderer.sprite;
        Vector2 transformGuiPosition = CameraWorldToGui(camera, renderer.transform.position);
        Rect spriteRect = CalculateCameraSpriteGuiRect(renderer, camera);

        Texture2D texture = sprite.texture;
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height, textureRect.width / texture.width, textureRect.height / texture.height);
        float screenRotation = renderer.transform.rotation.eulerAngles.z - camera.transform.rotation.eulerAngles.z;

        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(-screenRotation, transformGuiPosition);
        if (renderer.transform.lossyScale.x < 0f || renderer.transform.lossyScale.y < 0f)
        {
            GUIUtility.ScaleAroundPivot(new Vector2(renderer.transform.lossyScale.x < 0f ? -1f : 1f, renderer.transform.lossyScale.y < 0f ? -1f : 1f), transformGuiPosition);
        }
        GUI.DrawTextureWithTexCoords(spriteRect, texture, uv, true);
        GUI.matrix = previousMatrix;
        return ProjectRendererBoundsToGuiRect(renderer.bounds, camera);
    }

    private Rect CalculateCameraSpriteGuiRect(SpriteRenderer renderer, Camera camera)
    {
        Sprite sprite = renderer.sprite;
        Vector3 spriteCenterWorld = renderer.transform.TransformPoint(sprite.bounds.center);
        Vector2 centerGui = CameraWorldToGui(camera, spriteCenterWorld);
        Vector2 projectedRight = CameraWorldToGui(camera, spriteCenterWorld + renderer.transform.right * sprite.bounds.size.x * Mathf.Abs(renderer.transform.lossyScale.x));
        Vector2 projectedUp = CameraWorldToGui(camera, spriteCenterWorld + renderer.transform.up * sprite.bounds.size.y * Mathf.Abs(renderer.transform.lossyScale.y));
        float width = Vector2.Distance(centerGui, projectedRight);
        float height = Vector2.Distance(centerGui, projectedUp);
        return new Rect(centerGui.x - width * 0.5f, centerGui.y - height * 0.5f, width, height);
    }

    private Rect ProjectRendererBoundsToGuiRect(Bounds bounds, Camera camera)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Rect rect = default;
        bool hasRect = false;
        EncapsulateGuiPoint(ref rect, ref hasRect, CameraWorldToGui(camera, new Vector3(min.x, min.y, min.z)));
        EncapsulateGuiPoint(ref rect, ref hasRect, CameraWorldToGui(camera, new Vector3(min.x, max.y, min.z)));
        EncapsulateGuiPoint(ref rect, ref hasRect, CameraWorldToGui(camera, new Vector3(max.x, min.y, min.z)));
        EncapsulateGuiPoint(ref rect, ref hasRect, CameraWorldToGui(camera, new Vector3(max.x, max.y, min.z)));
        return rect;
    }

    private static void EncapsulateGuiPoint(ref Rect rect, ref bool hasRect, Vector2 point)
    {
        if (!hasRect)
        {
            rect = new Rect(point.x, point.y, 0f, 0f);
            hasRect = true;
            return;
        }

        rect = Rect.MinMaxRect(
            Mathf.Min(rect.xMin, point.x),
            Mathf.Min(rect.yMin, point.y),
            Mathf.Max(rect.xMax, point.x),
            Mathf.Max(rect.yMax, point.y));
    }

    private void DrawPlayerPreview()
    {
        Transform previewRoot = GetConfiguredPreviewPlayerRoot();
        if (previewRoot != null)
        {
            SpriteRenderer[] renderers = previewRoot.GetComponentsInChildren<SpriteRenderer>(true);
            System.Array.Sort(renderers, CompareSpriteRenderers);
            bool drewAnySprite = false;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.sprite == null || IsUnderCurrentWeaponVisual(renderer.transform))
                {
                    continue;
                }

                Vector3 simulatedLossyScale = GetPreviewPlayerRendererLossyScale(renderer);
                Vector2 localPosition = previewRoot == renderer.transform.root
                    ? ToVector2(renderer.transform.position - previewRoot.position)
                    : ToVector2(renderer.transform.position - previewRoot.position);
                Rect guiRect = DrawSpriteRendererLikeRuntime(renderer.sprite, localPosition, renderer.transform.rotation.eulerAngles.z, simulatedLossyScale);
                EncapsulateGuiRect(ref lastPlayerGuiRect, ref hasLastPlayerGuiRect, guiRect);
                Encapsulate(ref lastPlayerWorldBounds, ref hasLastPlayerWorldBounds, GetSpriteBounds(renderer.sprite, renderer.transform.position - previewRoot.position, simulatedLossyScale));
                drewAnySprite = true;
            }

            if (drewAnySprite)
            {
                return;
            }
        }

        if (previewPlayerSprite != null)
        {
            Rect guiRect = DrawSpriteRendererLikeRuntime(previewPlayerSprite, Vector2.zero, 0f, Vector3.one);
            EncapsulateGuiRect(ref lastPlayerGuiRect, ref hasLastPlayerGuiRect, guiRect);
            lastPlayerWorldBounds = GetSpriteBounds(previewPlayerSprite, Vector3.zero, Vector3.one);
            hasLastPlayerWorldBounds = true;
        }
    }

    private void DrawWeaponSprite(Sprite sprite, WeaponAlignmentPose pose)
    {
        ResolveWeaponRendererPreviewTransform(sprite, pose, out Vector3 rendererPosition, out Quaternion rendererRotation, out Vector3 rendererScale);

        if (GetWeaponPrefabSpriteRenderer() != null)
        {
            Rect guiRect = DrawSpriteRendererLikeRuntime(sprite, ToVector2(rendererPosition), rendererRotation.eulerAngles.z, rendererScale);
            lastWeaponGuiRect = guiRect;
            hasLastWeaponGuiRect = true;
            lastWeaponWorldBounds = GetWeaponRenderedBounds(sprite, pose);
            return;
        }

        Rect fallbackRect = DrawSpriteRendererLikeRuntime(sprite, ToVector2(rendererPosition), rendererRotation.eulerAngles.z, rendererScale);
        lastWeaponGuiRect = fallbackRect;
        hasLastWeaponGuiRect = true;
        lastWeaponWorldBounds = GetWeaponRenderedBounds(sprite, pose);
    }

    private bool DrawActualRuntimePreview()
    {
        if (!TryGetRuntimeVisualSnapshot(out RuntimeVisualSnapshot snapshot))
        {
            return false;
        }

        lastActualRuntimeSnapshot = snapshot;
        hasLastActualRuntimeSnapshot = true;

        Camera gameCamera = IsManualProjectionDiagnostic() ? GetGamePreviewCamera() : null;
        SpriteRenderer[] playerRenderers = snapshot.Player.GetComponentsInChildren<SpriteRenderer>(true);
        System.Array.Sort(playerRenderers, CompareSpriteRenderers);
        foreach (SpriteRenderer renderer in playerRenderers)
        {
            if (renderer == null || !renderer.enabled || renderer.sprite == null || IsUnderCurrentWeaponVisual(renderer.transform))
            {
                continue;
            }

            Rect guiRect = gameCamera != null
                ? DrawSpriteRendererWithGameCamera(renderer, gameCamera)
                : DrawSpriteRendererLikeRuntime(renderer.sprite, ToVector2(renderer.transform.position - snapshot.Player.transform.position), renderer.transform.rotation.eulerAngles.z, renderer.transform.lossyScale);
            EncapsulateGuiRect(ref lastRuntimePlayerGuiRect, ref hasLastRuntimePlayerGuiRect, guiRect);
            Encapsulate(ref lastRuntimePlayerWorldBounds, ref hasLastRuntimePlayerWorldBounds, ToLocalBounds(renderer.bounds, snapshot.Player.transform.position));
        }

        if (gameCamera != null)
        {
            DrawSpriteRendererWithGameCamera(snapshot.WeaponRenderer, gameCamera);
        }
        else
        {
            DrawSpriteRendererLikeRuntime(snapshot.WeaponRenderer.sprite, ToVector2(snapshot.LocalPositionToPlayer), snapshot.RotationZ, snapshot.LossyScale);
        }

        return true;
    }

    private GameObject GetRuntimePlayerObject()
    {
        PlayerMovement playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            return playerMovement.gameObject;
        }

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        return tagged;
    }

    private bool TryGetRuntimeVisualSnapshot(out RuntimeVisualSnapshot snapshot)
    {
        snapshot = default;
        GameObject runtimePlayer = GetRuntimePlayerObject();
        if (runtimePlayer == null)
        {
            return false;
        }

        Transform weaponRoot = FindChildRecursive(runtimePlayer.transform, "WeaponRoot");
        Transform weaponAnchor = weaponRoot != null ? FindChildRecursive(weaponRoot, WeaponAnchorName) : null;
        Transform activeVisual = GetRuntimeActiveWeaponVisual(runtimePlayer);
        SpriteRenderer weaponRenderer = activeVisual != null ? activeVisual.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (weaponRoot == null || activeVisual == null || weaponRenderer == null || weaponRenderer.sprite == null)
        {
            return false;
        }

        WeaponRenderBoundsReport boundsReport = WeaponRenderBoundsUtility.CalculateRenderedBoundsRatio(weaponRenderer, runtimePlayer.transform, WeaponRenderBoundsMode.BodyRendererOnly);
        SpriteRenderer playerRenderer = boundsReport.PlayerRenderer;
        Rect playerGuiRect = GetRuntimePlayerGuiRect(boundsReport, runtimePlayer.transform, out bool hasPlayerGuiRect);
        Rect weaponGuiRect = CalculateSpriteGuiRect(weaponRenderer.sprite, ToVector2(weaponRenderer.transform.position - runtimePlayer.transform.position), weaponRenderer.transform.lossyScale);
        Vector2 guiRatio = new Vector2(
            hasPlayerGuiRect && playerGuiRect.width > 0.0001f ? weaponGuiRect.width / playerGuiRect.width : 0f,
            hasPlayerGuiRect && playerGuiRect.height > 0.0001f ? weaponGuiRect.height / playerGuiRect.height : 0f);

        snapshot = new RuntimeVisualSnapshot
        {
            IsValid = true,
            Player = runtimePlayer,
            WeaponRoot = weaponRoot,
            WeaponAnchor = weaponAnchor,
            ActiveVisual = activeVisual,
            WeaponRenderer = weaponRenderer,
            PlayerRenderer = playerRenderer,
            ActiveVisualName = activeVisual.name,
            SpriteName = weaponRenderer.sprite.name,
            SortingLayerName = weaponRenderer.sortingLayerName,
            SortingOrder = weaponRenderer.sortingOrder,
            WorldPosition = weaponRenderer.transform.position,
            LocalPositionToPlayer = weaponRenderer.transform.position - runtimePlayer.transform.position,
            RotationZ = weaponRenderer.transform.rotation.eulerAngles.z,
            LocalScale = weaponRenderer.transform.localScale,
            LossyScale = weaponRenderer.transform.lossyScale,
            SpriteBounds = weaponRenderer.sprite.bounds,
            RendererBounds = weaponRenderer.bounds,
            RendererBoundsLocalToPlayer = ToLocalBounds(weaponRenderer.bounds, runtimePlayer.transform.position),
            GuiRect = weaponGuiRect,
            PlayerGuiRect = playerGuiRect,
            WorldRatio = boundsReport.Ratio,
            GuiRatio = guiRatio,
            BoundsReport = boundsReport
        };

        return true;
    }

    private SpriteRenderer GetRuntimePlayerRenderer(GameObject runtimePlayer)
    {
        return runtimePlayer != null ? WeaponRenderBoundsUtility.GetBodyRenderer(runtimePlayer.transform) : null;
    }

    private Rect GetRuntimePlayerGuiRect(WeaponRenderBoundsReport boundsReport, Transform playerRoot, out bool hasRect)
    {
        hasRect = false;
        if (!boundsReport.IsValid || boundsReport.PlayerRenderer == null)
        {
            return default;
        }

        hasRect = true;
        return CalculateSpriteGuiRect(
            boundsReport.PlayerRenderer.sprite,
            ToVector2(boundsReport.PlayerRenderer.transform.position - playerRoot.position),
            boundsReport.PlayerRenderer.transform.lossyScale);
    }

    private SpriteRenderer GetRuntimeWeaponRenderer(GameObject runtimePlayer)
    {
        Transform currentVisual = GetRuntimeActiveWeaponVisual(runtimePlayer);
        return currentVisual != null ? currentVisual.GetComponentInChildren<SpriteRenderer>(true) : null;
    }

    private Transform GetRuntimeActiveWeaponVisual(GameObject runtimePlayer)
    {
        if (runtimePlayer == null || weaponDefinition == null)
        {
            return null;
        }

        Transform weaponRoot = FindChildRecursive(runtimePlayer.transform, "WeaponRoot");
        if (weaponRoot == null)
        {
            return null;
        }

        Transform expected = FindChildRecursive(weaponRoot, "CurrentWeaponVisual_" + weaponDefinition.WeaponId);
        if (expected != null && expected.gameObject.activeInHierarchy)
        {
            return expected;
        }

        return FindActiveWeaponVisualRecursive(weaponRoot);
    }

    private static Transform FindActiveWeaponVisualRecursive(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("CurrentWeaponVisual_") && child.gameObject.activeInHierarchy)
            {
                return child;
            }

            Transform found = FindActiveWeaponVisualRecursive(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool IsUnderCurrentWeaponVisual(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.StartsWith("CurrentWeaponVisual_"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Bounds ToLocalBounds(Bounds worldBounds, Vector3 origin)
    {
        return new Bounds(worldBounds.center - origin, worldBounds.size);
    }

    private Vector2 GetPreviewWeaponAnchorWorld(out bool foundAnchor)
    {
        foundAnchor = false;

        Transform previewRoot = GetPreviewPlayerRoot();
        Transform anchor = GetRuntimeEquivalentWeaponAnchor(previewRoot);
        if (previewRoot != null && anchor != null)
        {
            foundAnchor = true;
            return ToVector2(anchor.position - previewRoot.position);
        }

        if (previewRoot != null)
        {
            anchor = FindChildRecursive(previewRoot, WeaponAnchorName);
            if (anchor != null)
            {
                foundAnchor = true;
                return ToVector2(anchor.position - previewRoot.position);
            }
        }

        return previewRoot != null ? Vector2.zero : ToVector2(previewWeaponAnchorOffset);
    }

    private Vector3 GetPreviewWeaponParentScale()
    {
        if (useNormalizedCalibrationScale)
        {
            return Vector3.one;
        }

        Transform previewRoot = GetPreviewPlayerRoot();
        Transform anchor = GetRuntimeEquivalentWeaponAnchor(previewRoot);
        if (anchor != null)
        {
            return anchor.lossyScale;
        }

        return previewRoot != null ? previewRoot.lossyScale : Vector3.one;
    }

    private Transform GetPreviewPlayerRoot()
    {
        return GetConfiguredPreviewPlayerRoot();
    }

    private Transform GetConfiguredPreviewPlayerRoot()
    {
        if (useRuntimePlayerReference)
        {
            GameObject runtimeOrScenePlayer = GetRuntimePlayerObject();
            if (runtimeOrScenePlayer != null)
            {
                return runtimeOrScenePlayer.transform;
            }
        }

        return previewPlayerPrefab != null ? previewPlayerPrefab.transform : null;
    }

    private string GetPreviewPlayerSourceLabel()
    {
        Transform root = GetConfiguredPreviewPlayerRoot();
        if (root == null)
        {
            return "None";
        }

        GameObject runtimeOrScenePlayer = useRuntimePlayerReference ? GetRuntimePlayerObject() : null;
        if (runtimeOrScenePlayer != null && root == runtimeOrScenePlayer.transform)
        {
            return "Runtime Player";
        }

        return previewPlayerPrefab != null && root == previewPlayerPrefab.transform ? "Player Prefab" : root.name;
    }

    private Transform GetRuntimeEquivalentWeaponAnchor(Transform previewRoot)
    {
        if (previewRoot == null)
        {
            return null;
        }

        WeaponController controller = previewRoot.GetComponent<WeaponController>();
        if (controller != null && controller.WeaponAnchor != null)
        {
            return controller.WeaponAnchor;
        }

        Transform weaponRoot = FindChildRecursive(previewRoot, "WeaponRoot");
        if (weaponRoot != null)
        {
            Transform anchor = FindChildRecursive(weaponRoot, WeaponAnchorName);
            if (anchor != null)
            {
                return anchor;
            }
        }

        return null;
    }

    private Vector3 GetSimulatedWeaponRootLossyScale()
    {
        return GetPreviewWeaponParentScale();
    }

    private Vector3 GetSimulatedWeaponVisualLocalScale(WeaponAlignmentPose pose)
    {
        return DivideScale(pose.VisualScale, GetSimulatedWeaponRootLossyScale());
    }

    private Vector3 GetSimulatedWeaponVisualLossyScale(WeaponAlignmentPose pose)
    {
        return pose.VisualScale;
    }

    private static Vector3 DivideScale(Vector3 targetScale, Vector3 parentScale)
    {
        return new Vector3(
            DivideScaleAxis(targetScale.x, parentScale.x),
            DivideScaleAxis(targetScale.y, parentScale.y),
            DivideScaleAxis(targetScale.z, parentScale.z));
    }

    private static float DivideScaleAxis(float targetScale, float parentScale)
    {
        return Mathf.Abs(parentScale) > 0.0001f ? targetScale / parentScale : targetScale;
    }

    private string GetSimulatedActiveWeaponVisualName()
    {
        return weaponDefinition != null ? "CurrentWeaponVisual_" + weaponDefinition.WeaponId : "None";
    }

    private string BuildPreviewScaleDebug()
    {
        if (IsMainCameraRenderView())
        {
            return "Scale source: official Game View validation from Main Camera RenderTexture.";
        }

        if (workflowMode == WorkflowMode.PreviewRuntimeObject)
        {
            return EditorApplication.isPlaying
                ? "Scale source: runtime object diagnostic only, not Game View WYSIWYG."
                : "Scale source: runtime object diagnostic requires Play Mode.";
        }

        return previewSurfaceMode == PreviewSurfaceMode.AuthoringView
            ? "Scale source: runtime pose and runtime scale, plus editor-only Preview Zoom for readability."
            : "Scale source: runtime-equivalent preview. Only viewport framing changes.";
    }

    private void LogEditorSharedBoundsDebug(WeaponRenderBoundsReport boundsReport)
    {
        if (Event.current == null || Event.current.type != EventType.Repaint)
        {
            return;
        }

        string line = WeaponRenderBoundsUtility.FormatSharedBoundsDebug("WeaponEditor", boundsReport);
        if (line == lastEditorSharedBoundsDebugLine)
        {
            return;
        }

        lastEditorSharedBoundsDebugLine = line;
        Debug.Log(line);
    }

    private void DrawEditAlignmentScaleComparison()
    {
        Vector2 anchor = GetPreviewWeaponAnchorWorld(out _);
        Vector2 aim = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        WeaponAlignmentPose pose = CalculatePreviewPose(anchor, aim);

        BoundsRatioDebugSnapshot simulatedEdit = CreateSimulatedEditAlignmentBoundsSnapshot(pose);

        RuntimeVisualSnapshot runtime = default;
        bool hasRuntime = EditorApplication.isPlaying && TryGetRuntimeVisualSnapshot(out runtime);
        BoundsRatioDebugSnapshot runtimeShared = hasRuntime ? CreateRuntimeSharedBoundsSnapshot(runtime) : default;
        BoundsRatioDebugSnapshot displayedEdit = hasRuntime && !useNormalizedCalibrationScale ? runtimeShared : simulatedEdit;
        GameCameraProjectionSnapshot projection = default;
        Camera camera = hasRuntime ? GetGamePreviewCamera() : null;
        bool hasValidateRatio = hasRuntime && camera != null && TryGetGameCameraProjectionSnapshot(runtime, camera, out projection);
        bool ratioMatch = displayedEdit.IsValid && runtimeShared.IsValid && IsClose(displayedEdit.Ratio, runtimeShared.Ratio, 0.03f);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Edit Alignment Scale Comparison", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Scale Model", GetEditAlignmentScaleModelLabel(hasRuntime));
        EditorGUILayout.LabelField("Edit Alignment Weapon/Player Ratio", displayedEdit.IsValid ? FormatVector2(displayedEdit.Ratio) : "No edit/runtime bounds");
        EditorGUILayout.LabelField("Validate Game View Weapon/Player Ratio", hasValidateRatio ? FormatVector2(projection.WeaponPlayerScreenRatio) : "Play Mode + active runtime weapon required");
        EditorGUILayout.LabelField("Runtime Shared Bounds Ratio", runtimeShared.IsValid ? FormatVector2(runtimeShared.Ratio) : "Play Mode + active runtime weapon required");
        EditorGUILayout.LabelField("RATIO_MATCH", ratioMatch.ToString());
        if (!ratioMatch && !useNormalizedCalibrationScale)
        {
            EditorGUILayout.HelpBox("Edit Alignment ratio is not runtime-matched. Final scale is validated in Validate Game View.", MessageType.Warning);
        }

        DrawBoundsRatioDebugTable("Edit Alignment", displayedEdit);
        DrawBoundsRatioDebugTable("Edit Alignment Simulated", simulatedEdit);
        DrawBoundsRatioDebugTable("Runtime Shared", runtimeShared);
    }

    private string GetEditAlignmentScaleModelLabel(bool hasRuntime)
    {
        if (useNormalizedCalibrationScale)
        {
            return "Normalized calibration";
        }

        return hasRuntime ? "Runtime Shared Bounds" : "Runtime-relative simulation";
    }

    private void DrawBoundsRatioDebugTable(string label, BoundsRatioDebugSnapshot snapshot)
    {
        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        if (!snapshot.IsValid)
        {
            EditorGUILayout.LabelField("Status", "No bounds");
            return;
        }

        EditorGUILayout.LabelField("Weapon Renderer/Source", snapshot.WeaponRendererSource);
        EditorGUILayout.LabelField("Player Renderer/Source", snapshot.PlayerRendererSource);
        EditorGUILayout.LabelField("Weapon Bounds", FormatBounds(snapshot.WeaponBounds));
        EditorGUILayout.LabelField("Player Bounds", FormatBounds(snapshot.PlayerBounds));
        EditorGUILayout.LabelField("Weapon Rendered Size", FormatVector2(snapshot.WeaponRenderedSize));
        EditorGUILayout.LabelField("Player Rendered Size", FormatVector2(snapshot.PlayerRenderedSize));
        EditorGUILayout.LabelField("Ratio", FormatVector2(snapshot.Ratio));
        EditorGUILayout.LabelField("Source Of Player Scale", snapshot.PlayerScaleSource);
        EditorGUILayout.LabelField("Source Of Weapon Parent Scale", snapshot.WeaponParentScaleSource);
        EditorGUILayout.LabelField("Source Of Player Bounds Mode", snapshot.PlayerBoundsMode);
    }

    private BoundsRatioDebugSnapshot CreateSimulatedEditAlignmentBoundsSnapshot(WeaponAlignmentPose pose)
    {
        BoundsRatioDebugSnapshot snapshot = new BoundsRatioDebugSnapshot
        {
            WeaponRendererSource = "None",
            PlayerRendererSource = "None",
            PlayerScaleSource = useNormalizedCalibrationScale ? "Normalized Vector3.one" : "Runtime Player Prefab root lossyScale",
            WeaponParentScaleSource = useNormalizedCalibrationScale ? "Normalized Vector3.one" : "Runtime Player Prefab root lossyScale",
            PlayerBoundsMode = WeaponRenderBoundsMode.BodyRendererOnly.ToString()
        };

        Transform previewRoot = GetConfiguredPreviewPlayerRoot();
        if (weaponDefinition == null || weaponDefinition.ItemImage == null || previewRoot == null)
        {
            return snapshot;
        }

        SpriteRenderer playerRenderer = WeaponRenderBoundsUtility.GetBodyRenderer(previewRoot);
        if (playerRenderer == null || playerRenderer.sprite == null)
        {
            return snapshot;
        }

        snapshot.WeaponBounds = GetWeaponRenderedBounds(weaponDefinition.ItemImage, pose);
        snapshot.PlayerBounds = GetSpriteRendererLikeWorldBounds(
            playerRenderer.sprite,
            playerRenderer.transform.position - previewRoot.position,
            playerRenderer.transform.rotation,
            GetPreviewPlayerRendererLossyScale(playerRenderer));
        snapshot.WeaponRenderedSize = ToSize(snapshot.WeaponBounds);
        snapshot.PlayerRenderedSize = ToSize(snapshot.PlayerBounds);
        snapshot.Ratio = CalculateRatio(snapshot.WeaponRenderedSize, snapshot.PlayerRenderedSize);
        snapshot.WeaponRendererSource = GetWeaponPrefabSpriteRenderer() != null
            ? WeaponRenderBoundsUtility.GetTransformPath(GetWeaponPrefabSpriteRenderer().transform)
            : "WeaponDefinition.ItemImage";
        snapshot.PlayerRendererSource = WeaponRenderBoundsUtility.GetTransformPath(playerRenderer.transform) + $" ({GetPreviewPlayerSourceLabel()})";
        snapshot.IsValid = true;
        return snapshot;
    }

    private BoundsRatioDebugSnapshot CreateRuntimeSharedBoundsSnapshot(RuntimeVisualSnapshot runtime)
    {
        if (!runtime.IsValid || !runtime.BoundsReport.IsValid)
        {
            return default;
        }

        WeaponRenderBoundsReport report = runtime.BoundsReport;
        return new BoundsRatioDebugSnapshot
        {
            IsValid = true,
            WeaponRendererSource = report.WeaponRendererPath,
            PlayerRendererSource = report.PlayerBoundsSourcePath,
            WeaponBounds = report.WeaponBounds,
            PlayerBounds = report.PlayerBounds,
            WeaponRenderedSize = report.WeaponRenderedSize,
            PlayerRenderedSize = report.PlayerRenderedSize,
            Ratio = report.Ratio,
            PlayerScaleSource = runtime.Player != null ? FormatVector(runtime.Player.transform.lossyScale) : "None",
            WeaponParentScaleSource = runtime.ActiveVisual != null && runtime.ActiveVisual.parent != null ? FormatVector(runtime.ActiveVisual.parent.lossyScale) : "None",
            PlayerBoundsMode = report.Mode.ToString()
        };
    }

    private void DrawEditorRuntimeComparisonPanel()
    {
        if (weaponDefinition == null)
        {
            return;
        }

        Vector2 anchor = GetPreviewWeaponAnchorWorld(out _);
        Vector2 aim = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        WeaponAlignmentPose pose = CalculatePreviewPose(anchor, aim);
        RuntimeVisualSnapshot runtimeSnapshot = default;
        bool hasRuntimeSnapshot = EditorApplication.isPlaying && TryGetRuntimeVisualSnapshot(out runtimeSnapshot);
        SpriteRenderer playerRenderer = GetPreviewPlayerRenderer();
        Sprite weaponSprite = weaponDefinition.ItemImage;
        Vector3 playerLossyScale = GetPreviewWeaponParentScale();
        Vector3 weaponRootLossyScale = GetSimulatedWeaponRootLossyScale();
        Vector3 weaponVisualLocalScale = GetSimulatedWeaponVisualLocalScale(pose);
        Vector3 weaponVisualLossyScale = GetSimulatedWeaponVisualLossyScale(pose);
        Bounds weaponRenderedBounds = weaponSprite != null ? GetWeaponRenderedBounds(weaponSprite, pose) : new Bounds(Vector3.zero, Vector3.zero);
        Vector2 weaponSize = new Vector2(weaponRenderedBounds.size.x, weaponRenderedBounds.size.y);
        Vector2 playerSize = playerRenderer != null && playerRenderer.sprite != null ? GetSpriteWorldSize(playerRenderer.sprite, GetPreviewPlayerRendererLossyScale(playerRenderer)) : Vector2.zero;
        Vector2 simulatedRatio = new Vector2(
            playerSize.x > 0.0001f ? weaponSize.x / playerSize.x : 0f,
            playerSize.y > 0.0001f ? weaponSize.y / playerSize.y : 0f);
        Vector2 displayedWorldRatio = hasRuntimeSnapshot ? runtimeSnapshot.BoundsReport.Ratio : simulatedRatio;
        Vector2 guiRatio = new Vector2(
            hasLastPlayerGuiRect && lastPlayerGuiRect.width > 0.0001f ? lastWeaponGuiRect.width / lastPlayerGuiRect.width : 0f,
            hasLastPlayerGuiRect && lastPlayerGuiRect.height > 0.0001f ? lastWeaponGuiRect.height / lastPlayerGuiRect.height : 0f);
        Vector2 displayedGuiRatio = hasRuntimeSnapshot ? runtimeSnapshot.GuiRatio : guiRatio;
        bool hasRuntimeContext = GetConfiguredPreviewPlayerRoot() != null;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Editor vs Runtime", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Player Lossy Scale", FormatVector(playerLossyScale));
        EditorGUILayout.LabelField("Player Source", GetPreviewPlayerSourceLabel());
        EditorGUILayout.LabelField("WeaponRoot Lossy Scale", FormatVector(weaponRootLossyScale));
        EditorGUILayout.LabelField("Weapon Visual Local Scale", FormatVector(weaponVisualLocalScale));
        EditorGUILayout.LabelField("Weapon Visual Lossy Scale", FormatVector(weaponVisualLossyScale));
        EditorGUILayout.LabelField("SpriteRenderer.Bounds", FormatBounds(weaponRenderedBounds));
        EditorGUILayout.LabelField("World Weapon/Player Ratio", FormatVector2(displayedWorldRatio));
        EditorGUILayout.LabelField("Ratio Source", hasRuntimeSnapshot ? "WeaponRenderBoundsUtility runtime actual" : "Editor simulated, Play Mode required for runtime ratio");
        EditorGUILayout.LabelField("Shared Runtime Ratio", hasRuntimeSnapshot ? FormatVector2(runtimeSnapshot.BoundsReport.Ratio) : "None");
        EditorGUILayout.LabelField("Shared Bounds Debug", hasRuntimeSnapshot ? WeaponRenderBoundsUtility.FormatSharedBoundsDebug("WeaponEditor", runtimeSnapshot.BoundsReport) : "None");
        EditorGUILayout.LabelField("GUI Weapon/Player Ratio", hasRuntimeSnapshot || (hasLastWeaponGuiRect && hasLastPlayerGuiRect) ? FormatVector2(displayedGuiRatio) : "No viewport sample yet");
        EditorGUILayout.LabelField("Weapon GUI Rect", hasLastWeaponGuiRect ? FormatRect(lastWeaponGuiRect) : "No viewport sample yet");
        EditorGUILayout.LabelField("Player GUI Rect", hasLastPlayerGuiRect ? FormatRect(lastPlayerGuiRect) : "No viewport sample yet");
        EditorGUILayout.LabelField("Weapon World Position", FormatVector(pose.WeaponPosition));
        EditorGUILayout.LabelField("Anchor Position", FormatVector(pose.WeaponAnchorPosition));
        EditorGUILayout.LabelField("Grip Alignment Delta", FormatVector(pose.GripPoint - pose.WeaponAnchorPosition));
        EditorGUILayout.LabelField("Active Visual", GetSimulatedActiveWeaponVisualName());
        EditorGUILayout.LabelField("Mismatch Status", hasRuntimeContext ? "OK: runtime prefab context" : "Risk: not using runtime prefab");
        DrawActualRuntimeComparison(pose, displayedWorldRatio);
    }

    private void DrawActualRuntimeComparison(WeaponAlignmentPose simulatedPose, Vector2 simulatedWorldRatio)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Runtime Actual vs Editor Simulated", EditorStyles.boldLabel);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.LabelField("Actual Runtime", "Play Mode required");
            return;
        }

        if (!TryGetRuntimeVisualSnapshot(out RuntimeVisualSnapshot runtime))
        {
            EditorGUILayout.LabelField("Actual Runtime", "No active runtime CurrentWeaponVisual with SpriteRenderer found");
            return;
        }

        bool hasSimulatedRenderer = TryGetSimulatedWeaponRendererSample(
            simulatedPose,
            out Vector2 simulatedRendererPosition,
            out float simulatedRendererRotation,
            out Vector3 simulatedRendererScale,
            out Bounds simulatedRendererBounds,
            out Rect simulatedRendererGuiRect);
        bool hasSimulatedPlayerGuiRect = TryGetSimulatedPlayerGuiRect(out Rect simulatedPlayerGuiRect);
        Vector2 simulatedGuiRatio = new Vector2(
            hasSimulatedPlayerGuiRect && simulatedPlayerGuiRect.width > 0.0001f ? simulatedRendererGuiRect.width / simulatedPlayerGuiRect.width : 0f,
            hasSimulatedPlayerGuiRect && simulatedPlayerGuiRect.height > 0.0001f ? simulatedRendererGuiRect.height / simulatedPlayerGuiRect.height : 0f);

        float rotationDelta = Mathf.Abs(Mathf.DeltaAngle(runtime.RotationZ, simulatedRendererRotation));
        float scaleDelta = Vector3.Distance(runtime.LossyScale, simulatedRendererScale);
        float positionDelta = Vector2.Distance(ToVector2(runtime.LocalPositionToPlayer), simulatedRendererPosition);
        float boundsDelta = Mathf.Abs(runtime.RendererBoundsLocalToPlayer.size.x - simulatedRendererBounds.size.x)
            + Mathf.Abs(runtime.RendererBoundsLocalToPlayer.size.y - simulatedRendererBounds.size.y);

        EditorGUILayout.LabelField("Active Weapon Visual", runtime.ActiveVisualName);
        EditorGUILayout.LabelField("Sprite Name", runtime.SpriteName);
        EditorGUILayout.LabelField("World Position", FormatVector(runtime.WorldPosition));
        EditorGUILayout.LabelField("Local Position To Player", FormatVector(runtime.LocalPositionToPlayer));
        EditorGUILayout.LabelField("Rotation Z", runtime.RotationZ.ToString("0.###"));
        EditorGUILayout.LabelField("LocalScale", FormatVector(runtime.LocalScale));
        EditorGUILayout.LabelField("LossyScale", FormatVector(runtime.LossyScale));
        EditorGUILayout.LabelField("Sprite.bounds", FormatBounds(runtime.SpriteBounds));
        EditorGUILayout.LabelField("SpriteRenderer.bounds", FormatBounds(runtime.RendererBounds));
        EditorGUILayout.LabelField("SpriteRenderer.bounds Local", FormatBounds(runtime.RendererBoundsLocalToPlayer));
        EditorGUILayout.LabelField("Weapon Renderer Path", runtime.BoundsReport.WeaponRendererPath);
        EditorGUILayout.LabelField("Player Bounds Source", runtime.BoundsReport.PlayerBoundsSourcePath);
        EditorGUILayout.LabelField("Player Bounds Mode", runtime.BoundsReport.Mode.ToString());
        EditorGUILayout.LabelField("Player Bounds Description", runtime.BoundsReport.PlayerBoundsSourceDescription);
        EditorGUILayout.LabelField("Player Renderer Count", runtime.BoundsReport.PlayerRendererCount.ToString());
        EditorGUILayout.LabelField("Shared Weapon Bounds", FormatBounds(runtime.BoundsReport.WeaponBounds));
        EditorGUILayout.LabelField("Shared Player Bounds", FormatBounds(runtime.BoundsReport.PlayerBounds));
        EditorGUILayout.LabelField("Shared Weapon Size", FormatVector2(runtime.BoundsReport.WeaponRenderedSize));
        EditorGUILayout.LabelField("Shared Player Size", FormatVector2(runtime.BoundsReport.PlayerRenderedSize));
        EditorGUILayout.LabelField("Sorting", $"{runtime.SortingLayerName} / {runtime.SortingOrder}");
        EditorGUILayout.LabelField("GUI Rect", FormatRect(runtime.GuiRect));
        EditorGUILayout.LabelField("Weapon/Player World Ratio", FormatVector2(runtime.WorldRatio));
        EditorGUILayout.LabelField("Weapon/Player GUI Ratio", FormatVector2(runtime.GuiRatio));

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Editor Sim Active Visual", GetSimulatedActiveWeaponVisualName());
        EditorGUILayout.LabelField("Editor Sim Renderer Position", hasSimulatedRenderer ? FormatVector(simulatedRendererPosition) : "None");
        EditorGUILayout.LabelField("Editor Sim Rotation Z", hasSimulatedRenderer ? simulatedRendererRotation.ToString("0.###") : "None");
        EditorGUILayout.LabelField("Editor Sim LossyScale", hasSimulatedRenderer ? FormatVector(simulatedRendererScale) : "None");
        EditorGUILayout.LabelField("Editor Sim SpriteRenderer.Bounds", hasSimulatedRenderer ? FormatBounds(simulatedRendererBounds) : "None");
        EditorGUILayout.LabelField("Editor Sim GUI Rect", hasSimulatedRenderer ? FormatRect(simulatedRendererGuiRect) : "None");
        EditorGUILayout.LabelField("Editor Sim Player GUI Rect", hasSimulatedPlayerGuiRect ? FormatRect(simulatedPlayerGuiRect) : "None");
        EditorGUILayout.LabelField("Editor Sim World Ratio", FormatVector2(simulatedWorldRatio));
        EditorGUILayout.LabelField("Editor GUI Ratio", hasSimulatedPlayerGuiRect ? FormatVector2(simulatedGuiRatio) : "No simulated player rect");

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("POSITION_MATCH", (hasSimulatedRenderer && positionDelta < 0.05f).ToString());
        EditorGUILayout.LabelField("ROTATION_MATCH", (hasSimulatedRenderer && rotationDelta < 1f).ToString());
        EditorGUILayout.LabelField("SCALE_MATCH", (hasSimulatedRenderer && scaleDelta < 0.05f).ToString());
        EditorGUILayout.LabelField("BOUNDS_MATCH", (hasSimulatedRenderer && boundsDelta < 0.05f).ToString());
        EditorGUILayout.LabelField("WORLD_RATIO_MATCH", IsClose(runtime.WorldRatio, simulatedWorldRatio, 0.03f).ToString());
        EditorGUILayout.LabelField("GUI_RATIO_MATCH", IsClose(runtime.GuiRatio, simulatedGuiRatio, 0.03f).ToString());

        DrawGameCameraProjectionDebug(runtime);
    }

    private void DrawGameCameraProjectionDebug(RuntimeVisualSnapshot runtime)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Game Camera View", EditorStyles.boldLabel);

        Camera camera = EditorApplication.isPlaying ? GetGamePreviewCamera() : null;
        GameCameraProjectionSnapshot projection = default;
        bool hasProjection = camera != null && TryGetGameCameraProjectionSnapshot(runtime, camera, out projection);

        string status = !EditorApplication.isPlaying ? "Play Mode required"
            : camera == null ? "No runtime camera found"
            : hasProjection ? "OK"
            : "Could not project runtime player/weapon renderers";

        EditorGUILayout.LabelField("Game Camera Status", status);
        EditorGUILayout.LabelField("Main Camera Name", camera != null ? camera.name : "None");
        EditorGUILayout.LabelField("Camera Position", camera != null ? FormatVector(camera.transform.position) : "None");
        EditorGUILayout.LabelField("Orthographic Size", camera != null ? (camera.orthographic ? camera.orthographicSize.ToString("0.###") : "Perspective") : "None");
        EditorGUILayout.LabelField("Aspect Ratio", camera != null ? camera.aspect.ToString("0.###") : "None");
        EditorGUILayout.LabelField("Pixel Rect", camera != null ? FormatRect(camera.pixelRect) : "None");
        EditorGUILayout.LabelField("Target Display", camera != null ? camera.targetDisplay.ToString() : "None");
        EditorGUILayout.LabelField("Game View Resolution", camera != null ? GetGameViewResolutionDebug(camera) : "None");
        EditorGUILayout.LabelField("Game View Aspect", camera != null && camera.pixelHeight > 0 ? (camera.pixelWidth / (float)camera.pixelHeight).ToString("0.###") : "None");
        EditorGUILayout.LabelField("Preview Rect", hasProjection ? FormatRect(projection.PreviewContentRect) : "None");
        EditorGUILayout.LabelField("Main Camera Render Preview", (useMainCameraRenderPreview && useActualRuntimeVisualForPreview && useGameCameraProjection && EditorApplication.isPlaying).ToString());
        EditorGUILayout.LabelField("Pixel Perfect Camera", hasProjection ? projection.PixelPerfectStatus : "None");
        EditorGUILayout.LabelField("Player World Position", hasProjection ? FormatVector(projection.PlayerWorldPosition) : "None");
        EditorGUILayout.LabelField("Weapon World Position", hasProjection ? FormatVector(projection.WeaponWorldPosition) : "None");
        EditorGUILayout.LabelField("Projected Player GUI Rect", hasProjection ? FormatRect(projection.PlayerGuiRect) : "None");
        EditorGUILayout.LabelField("Projected Weapon GUI Rect", hasProjection ? FormatRect(projection.WeaponGuiRect) : "None");
        EditorGUILayout.LabelField("GameCamera Weapon/Player Screen Ratio", hasProjection ? FormatVector2(projection.WeaponPlayerScreenRatio) : "None");
        EditorGUILayout.LabelField("Editor GUI Weapon/Player Ratio", hasProjection ? FormatVector2(projection.EditorGuiWeaponPlayerRatio) : "None");
        EditorGUILayout.LabelField("Footer Ratio", FormatVector2(runtime.BoundsReport.Ratio));
        EditorGUILayout.LabelField("Shared Utility Ratio", FormatVector2(runtime.BoundsReport.Ratio));
        EditorGUILayout.LabelField("RATIO_SOURCE_MATCH", IsClose(runtime.BoundsReport.Ratio, runtime.WorldRatio, 0.0001f).ToString());
        EditorGUILayout.LabelField("CAMERA_PROJECTION_MODE_ACTIVE", (useActualRuntimeVisualForPreview && useGameCameraProjection && EditorApplication.isPlaying).ToString());
        EditorGUILayout.LabelField("USING_FIT_OR_PAN_IN_GAME_CAMERA_VIEW", "False");
        EditorGUILayout.LabelField("WYSIWYG_SOURCE_OF_TRUTH", useMainCameraRenderPreview ? "Main Camera RenderTexture" : "Manual Projection Diagnostic");
        EditorGUILayout.LabelField("CAMERA_PROJECTION_MATCH", (hasProjection && useGameCameraProjection && IsClose(projection.WeaponPlayerScreenRatio, projection.EditorGuiWeaponPlayerRatio, 0.01f)).ToString());
    }

    private bool TryGetGameCameraProjectionSnapshot(RuntimeVisualSnapshot runtime, Camera camera, out GameCameraProjectionSnapshot snapshot)
    {
        snapshot = default;
        if (!runtime.IsValid || runtime.Player == null || runtime.WeaponRenderer == null)
        {
            return false;
        }

        Rect playerRect = runtime.BoundsReport.IsValid
            ? ProjectRendererBoundsToGuiRect(runtime.BoundsReport.PlayerBounds, camera)
            : GetProjectedRuntimePlayerGuiRect(runtime.Player, camera, out _);
        bool hasPlayerRect = playerRect.width > 0.0001f && playerRect.height > 0.0001f;
        Rect weaponRect = ProjectRendererBoundsToGuiRect(runtime.WeaponRenderer.bounds, camera);
        if (!hasPlayerRect || weaponRect.width <= 0.0001f || weaponRect.height <= 0.0001f)
        {
            return false;
        }

        Vector2 ratio = new Vector2(
            playerRect.width > 0.0001f ? weaponRect.width / playerRect.width : 0f,
            playerRect.height > 0.0001f ? weaponRect.height / playerRect.height : 0f);

        snapshot = new GameCameraProjectionSnapshot
        {
            IsValid = true,
            Camera = camera,
            PreviewContentRect = GetGameCameraPreviewContentRect(camera),
            PlayerGuiRect = playerRect,
            WeaponGuiRect = weaponRect,
            WeaponPlayerScreenRatio = ratio,
            EditorGuiWeaponPlayerRatio = ratio,
            PlayerWorldPosition = runtime.Player.transform.position,
            WeaponWorldPosition = runtime.WorldPosition,
            PixelPerfectStatus = GetPixelPerfectStatus(camera)
        };

        return true;
    }

    private Rect GetProjectedRuntimePlayerGuiRect(GameObject runtimePlayer, Camera camera, out bool hasRect)
    {
        hasRect = false;
        Rect rect = default;
        foreach (SpriteRenderer renderer in runtimePlayer.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null || !renderer.enabled || renderer.sprite == null || IsUnderCurrentWeaponVisual(renderer.transform))
            {
                continue;
            }

            Rect rendererRect = ProjectRendererBoundsToGuiRect(renderer.bounds, camera);
            EncapsulateGuiRect(ref rect, ref hasRect, rendererRect);
        }

        return rect;
    }

    private static string GetPixelPerfectStatus(Camera camera)
    {
        Component[] components = camera.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null || !component.GetType().Name.Contains("PixelPerfectCamera"))
            {
                continue;
            }

            string enabledText = component is Behaviour behaviour ? (behaviour.enabled ? "Enabled" : "Disabled") : "Present";
            return $"{enabledText} ({component.GetType().FullName})";
        }

        return "Not found";
    }

    private static string GetGameViewResolutionDebug(Camera camera)
    {
        string screenResolution = $"{Screen.width}x{Screen.height}";
        try
        {
            System.Type unityStatsType = typeof(EditorWindow).Assembly.GetType("UnityEditor.UnityStats");
            System.Reflection.PropertyInfo screenResProperty = unityStatsType?.GetProperty("screenRes", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            object screenRes = screenResProperty?.GetValue(null);
            if (screenRes != null)
            {
                screenResolution = screenRes.ToString();
            }
        }
        catch
        {
            // UnityStats is editor-internal and may not be available in all Unity versions.
        }

        return $"{screenResolution} | camera pixel {camera.pixelWidth}x{camera.pixelHeight}";
    }

    private bool TryGetSimulatedWeaponRendererSample(
        WeaponAlignmentPose pose,
        out Vector2 rendererPosition,
        out float rendererRotation,
        out Vector3 rendererScale,
        out Bounds rendererBounds,
        out Rect guiRect)
    {
        rendererPosition = Vector2.zero;
        rendererRotation = 0f;
        rendererScale = Vector3.one;
        rendererBounds = new Bounds(Vector3.zero, Vector3.zero);
        guiRect = default;

        Sprite sprite = weaponDefinition != null ? weaponDefinition.ItemImage : null;
        if (sprite == null)
        {
            return false;
        }

        Vector3 visualLossyScale = GetSimulatedWeaponVisualLossyScale(pose);
        rendererPosition = ToVector2(pose.WeaponPosition);
        rendererRotation = pose.WeaponRotation.eulerAngles.z;
        rendererScale = visualLossyScale;

        SpriteRenderer prefabRenderer = GetWeaponPrefabSpriteRenderer();
        if (prefabRenderer != null)
        {
            Transform prefabRoot = weaponDefinition.WeaponPrefab.transform;
            Transform rendererTransform = prefabRenderer.transform;
            Vector2 rendererLocalPosition = ToVector2(prefabRoot.InverseTransformPoint(rendererTransform.position));
            float rendererLocalRotation = (Quaternion.Inverse(prefabRoot.rotation) * rendererTransform.rotation).eulerAngles.z;
            Vector3 rendererLocalScale = GetRelativeScaleIncludingRoot(prefabRoot, rendererTransform);
            Quaternion visualRotationQuat = Quaternion.Euler(0f, 0f, rendererRotation);
            rendererPosition += ToVector2(visualRotationQuat * Vector3.Scale(rendererLocalPosition, Vector3.Scale(visualLossyScale, prefabRoot.localScale)));
            rendererRotation += rendererLocalRotation;
            rendererScale = Vector3.Scale(visualLossyScale, rendererLocalScale);
        }

        rendererBounds = GetWeaponRenderedBounds(sprite, pose);
        guiRect = CalculateSpriteGuiRect(sprite, rendererPosition, rendererScale);
        return true;
    }

    private bool TryGetSimulatedPlayerGuiRect(out Rect guiRect)
    {
        guiRect = default;
        bool hasRect = false;

        Transform previewRoot = GetConfiguredPreviewPlayerRoot();
        if (previewRoot != null)
        {
            foreach (SpriteRenderer renderer in previewRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || !renderer.enabled || renderer.sprite == null || IsUnderCurrentWeaponVisual(renderer.transform))
                {
                    continue;
                }

                Vector3 simulatedLossyScale = GetPreviewPlayerRendererLossyScale(renderer);
                Rect rendererRect = CalculateSpriteGuiRect(renderer.sprite, ToVector2(renderer.transform.position - previewRoot.position), simulatedLossyScale);
                EncapsulateGuiRect(ref guiRect, ref hasRect, rendererRect);
            }
        }
        else if (previewPlayerSprite != null)
        {
            guiRect = CalculateSpriteGuiRect(previewPlayerSprite, Vector2.zero, Vector3.one);
            hasRect = true;
        }

        return hasRect;
    }

    private Bounds GetWeaponRenderedBounds(Sprite sprite, WeaponAlignmentPose pose)
    {
        ResolveWeaponRendererPreviewTransform(sprite, pose, out Vector3 rendererPosition, out Quaternion rendererRotation, out Vector3 rendererScale);
        return GetSpriteRendererLikeWorldBounds(sprite, rendererPosition, rendererRotation, rendererScale);
    }

    private void ResolveWeaponRendererPreviewTransform(Sprite sprite, WeaponAlignmentPose pose, out Vector3 rendererPosition, out Quaternion rendererRotation, out Vector3 rendererScale)
    {
        Vector3 visualLossyScale = GetSimulatedWeaponVisualLossyScale(pose);
        rendererPosition = pose.WeaponPosition;
        rendererRotation = pose.WeaponRotation;
        rendererScale = visualLossyScale;

        SpriteRenderer prefabRenderer = GetWeaponPrefabSpriteRenderer();
        if (prefabRenderer == null || weaponDefinition == null || weaponDefinition.WeaponPrefab == null)
        {
            return;
        }

        Transform prefabRoot = weaponDefinition.WeaponPrefab.transform;
        Transform rendererTransform = prefabRenderer.transform;
        Vector3 rendererLocalPosition = prefabRoot.InverseTransformPoint(rendererTransform.position);
        float rendererLocalRotation = (Quaternion.Inverse(prefabRoot.rotation) * rendererTransform.rotation).eulerAngles.z;
        Vector3 rendererLocalScale = GetRelativeScaleIncludingRoot(prefabRoot, rendererTransform);
        rendererScale = Vector3.Scale(visualLossyScale, rendererLocalScale);
        rendererPosition = pose.WeaponPosition + pose.WeaponRotation * Vector3.Scale(rendererLocalPosition, Vector3.Scale(visualLossyScale, prefabRoot.localScale));
        rendererRotation = pose.WeaponRotation * Quaternion.Euler(0f, 0f, rendererLocalRotation);
    }

    private Vector3 GetPreviewPlayerRendererLossyScale(SpriteRenderer renderer)
    {
        Transform previewRoot = GetConfiguredPreviewPlayerRoot();
        if (renderer == null || previewRoot == null)
        {
            return Vector3.one;
        }

        if (useRuntimePlayerReference && GetRuntimePlayerObject() != null && previewRoot == GetRuntimePlayerObject().transform)
        {
            return renderer.transform.lossyScale;
        }

        return GetSimulatedLossyScale(previewRoot, renderer.transform, previewRoot.lossyScale);
    }

    private static Vector3 GetSimulatedLossyScale(Transform root, Transform child, Vector3 rootSimulatedLossyScale)
    {
        Vector3 localScale = Vector3.one;
        Transform current = child;
        while (current != null && current != root)
        {
            localScale = Vector3.Scale(localScale, current.localScale);
            current = current.parent;
        }
        return Vector3.Scale(rootSimulatedLossyScale, localScale);
    }

    private SpriteRenderer GetPreviewPlayerRenderer()
    {
        Transform previewRoot = GetConfiguredPreviewPlayerRoot();
        return previewRoot != null ? WeaponRenderBoundsUtility.GetBodyRenderer(previewRoot) : null;
    }

    private SpriteRenderer GetWeaponPrefabSpriteRenderer()
    {
        if (weaponDefinition == null || weaponDefinition.WeaponPrefab == null)
        {
            return null;
        }

        return weaponDefinition.WeaponPrefab.GetComponentInChildren<SpriteRenderer>(true);
    }

    private static Vector3 GetRelativeScaleIncludingRoot(Transform root, Transform child)
    {
        Vector3 scale = Vector3.one;
        Transform current = child;
        while (current != null)
        {
            scale = Vector3.Scale(scale, current.localScale);
            if (current == root)
            {
                break;
            }

            current = current.parent;
        }
        return scale;
    }

    private static Vector2 GetSpriteWorldSize(Sprite sprite, Vector3 scale)
    {
        return new Vector2(sprite.bounds.size.x * Mathf.Abs(scale.x), sprite.bounds.size.y * Mathf.Abs(scale.y));
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static int CompareSpriteRenderers(SpriteRenderer left, SpriteRenderer right)
    {
        int layerCompare = left.sortingLayerID.CompareTo(right.sortingLayerID);
        return layerCompare != 0 ? layerCompare : left.sortingOrder.CompareTo(right.sortingOrder);
    }

    private void DrawGrid(Rect rect)
    {
        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, 0.08f);
        float step = BasePixelsPerWorldUnit * GetEffectiveViewZoom();
        if (step >= 8f)
        {
            Vector2 origin = WorldToGui(Vector2.zero);
            for (float x = Mathf.Repeat(origin.x, step); x <= rect.xMax; x += step) Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));
            for (float y = Mathf.Repeat(origin.y, step); y <= rect.yMax; y += step) Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
        }
        Handles.color = new Color(1f, 1f, 1f, 0.25f);
        Vector2 axis = WorldToGui(Vector2.zero);
        Handles.DrawLine(new Vector3(axis.x, rect.yMin), new Vector3(axis.x, rect.yMax));
        Handles.DrawLine(new Vector3(rect.xMin, axis.y), new Vector3(rect.xMax, axis.y));
        Handles.color = previous;
        Handles.EndGUI();
    }

    private enum PreviewMarkerKind
    {
        GripPoint,
        TipPoint,
        ProjectileSpawnPoint,
        SlashOrigin,
        SlashArcStart,
        SlashArcEnd
    }

    private static void DrawPlayerCenter(Vector2 position) => DrawLabeledDisc(position, 7f, new Color(1f, 1f, 1f, 0.9f), "PC", new Vector2(8f, 4f));
    private static void DrawWeaponAnchor(Vector2 position, string label = "A") => DrawLabeledDisc(position, 5f, new Color(0.7f, 0.7f, 0.7f), label, new Vector2(8f, 2f));
    private static void DrawWeaponPosition(Vector2 position) => DrawLabeledDisc(position, 5f, new Color(1f, 0.25f, 1f), "W", new Vector2(8f, -20f));

    private void DrawFilteredMarker(PreviewMarkerKind kind, Vector2 position)
    {
        if (markerDisplayMode == MarkerDisplayMode.Hidden)
        {
            return;
        }

        bool relevant = IsMarkerRelevant(kind);
        if (!relevant && !showIrrelevantPoints && markerDisplayMode != MarkerDisplayMode.AllLabels)
        {
            return;
        }

        bool showLabel = markerDisplayMode == MarkerDisplayMode.AllLabels
            || (markerDisplayMode == MarkerDisplayMode.RelevantOnly && relevant && weaponDefinition != null && weaponDefinition.ResolvedArchetype != WeaponArchetype.Generic);
        if (markerDisplayMode == MarkerDisplayMode.DotsOnly)
        {
            showLabel = false;
        }

        DrawMarker(position, GetMarkerColor(kind, relevant), GetMarkerLabel(kind), GetMarkerLabelOffset(kind), showLabel);
    }

    private bool IsMarkerRelevant(PreviewMarkerKind kind)
    {
        if (weaponDefinition == null)
        {
            return true;
        }

        bool isRanged = IsRangedAuthoringArchetype();
        bool isMelee = IsMeleeAuthoringArchetype();
        bool isGeneric = weaponDefinition.ResolvedArchetype == WeaponArchetype.Generic;

        switch (kind)
        {
            case PreviewMarkerKind.GripPoint:
            case PreviewMarkerKind.TipPoint:
                return true;
            case PreviewMarkerKind.ProjectileSpawnPoint:
                return isRanged || (isGeneric && markerDisplayMode == MarkerDisplayMode.AllLabels) || showIrrelevantPoints;
            case PreviewMarkerKind.SlashOrigin:
            case PreviewMarkerKind.SlashArcStart:
            case PreviewMarkerKind.SlashArcEnd:
                return isMelee || (isGeneric && markerDisplayMode == MarkerDisplayMode.AllLabels) || showIrrelevantPoints;
            default:
                return true;
        }
    }

    private bool IsRangedAuthoringArchetype()
    {
        if (weaponDefinition == null)
        {
            return false;
        }

        WeaponArchetype archetype = weaponDefinition.ResolvedArchetype;
        return weaponDefinition.WeaponType == WeaponType.Projectile
            || archetype == WeaponArchetype.Bow
            || archetype == WeaponArchetype.Gun
            || archetype == WeaponArchetype.Staff
            || archetype == WeaponArchetype.Wand;
    }

    private bool IsMeleeAuthoringArchetype()
    {
        if (weaponDefinition == null)
        {
            return false;
        }

        WeaponArchetype archetype = weaponDefinition.ResolvedArchetype;
        return weaponDefinition.WeaponType == WeaponType.Melee
            || archetype == WeaponArchetype.Sword
            || archetype == WeaponArchetype.Dagger
            || archetype == WeaponArchetype.Axe
            || archetype == WeaponArchetype.Greatsword
            || archetype == WeaponArchetype.Spear;
    }

    private bool ShouldDrawSlashArc()
    {
        return markerDisplayMode != MarkerDisplayMode.Hidden
            && IsMarkerRelevant(PreviewMarkerKind.SlashOrigin)
            && IsMarkerRelevant(PreviewMarkerKind.SlashArcStart)
            && IsMarkerRelevant(PreviewMarkerKind.SlashArcEnd);
    }

    private bool ShouldDrawProjectileLine()
    {
        return markerDisplayMode != MarkerDisplayMode.Hidden && IsMarkerRelevant(PreviewMarkerKind.ProjectileSpawnPoint);
    }

    private static Color GetMarkerColor(PreviewMarkerKind kind, bool relevant)
    {
        Color color = kind switch
        {
            PreviewMarkerKind.GripPoint => new Color(1f, 0.82f, 0.25f),
            PreviewMarkerKind.TipPoint => new Color(0.25f, 1f, 0.45f),
            PreviewMarkerKind.ProjectileSpawnPoint => new Color(1f, 0.25f, 0.2f),
            _ => new Color(1f, 0.35f, 0.2f)
        };

        if (!relevant)
        {
            color *= 0.6f;
            color.a = 0.45f;
        }

        return color;
    }

    private static string GetMarkerLabel(PreviewMarkerKind kind)
    {
        return kind switch
        {
            PreviewMarkerKind.GripPoint => "G",
            PreviewMarkerKind.TipPoint => "T",
            PreviewMarkerKind.ProjectileSpawnPoint => "P",
            PreviewMarkerKind.SlashOrigin => "O",
            PreviewMarkerKind.SlashArcStart => "S1",
            PreviewMarkerKind.SlashArcEnd => "S2",
            _ => "?"
        };
    }

    private static Vector2 GetMarkerLabelOffset(PreviewMarkerKind kind)
    {
        return kind switch
        {
            PreviewMarkerKind.GripPoint => new Vector2(6f, -16f),
            PreviewMarkerKind.TipPoint => new Vector2(6f, 2f),
            PreviewMarkerKind.ProjectileSpawnPoint => new Vector2(6f, 12f),
            PreviewMarkerKind.SlashOrigin => new Vector2(6f, -16f),
            PreviewMarkerKind.SlashArcStart => new Vector2(6f, -16f),
            PreviewMarkerKind.SlashArcEnd => new Vector2(6f, 2f),
            _ => new Vector2(6f, 2f)
        };
    }

    private static void DrawMarker(Vector2 position, Color color, string label, Vector2 labelOffset, bool showLabel)
    {
        DrawDisc(position, 4.5f, color);
        if (showLabel)
        {
            GUI.Label(new Rect(position.x + labelOffset.x, position.y + labelOffset.y, 36f, 18f), label, EditorStyles.miniLabel);
        }
    }

    private static void DrawLabeledDisc(Vector2 position, float radius, Color color, string label, Vector2 labelOffset)
    {
        DrawDisc(position, radius, color);
        GUI.Label(new Rect(position.x + labelOffset.x, position.y + labelOffset.y, 120f, 18f), label, EditorStyles.miniLabel);
    }

    private static void DrawAimVector(Vector2 origin, Vector2 direction)
    {
        Vector2 end = origin + new Vector2(direction.x, -direction.y).normalized * 150f;
        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = new Color(0.35f, 0.82f, 1f);
        Handles.DrawAAPolyLine(3f, origin, end);
        Handles.color = previous;
        Handles.EndGUI();
    }

    private void DrawWeaponBoundsOverlays(Sprite sprite, WeaponAlignmentPose pose)
    {
        if (sprite == null || boundsOverlayMode == BoundsOverlayMode.Hidden)
        {
            return;
        }

        if (boundsOverlayMode == BoundsOverlayMode.SelectedBounds)
        {
            DrawWeaponBoundsOverlay(sprite, pose, previewBoundsMode, previewBoundsMode == SpriteBoundsCoordinateSpace.OpaqueContentBounds
                ? new Color(0.25f, 1f, 0.9f, 0.9f)
                : new Color(1f, 1f, 0.3f, 0.9f), previewBoundsMode == SpriteBoundsCoordinateSpace.OpaqueContentBounds ? "Opaque" : "Full");
        }

        if (boundsOverlayMode == BoundsOverlayMode.Both)
        {
            DrawWeaponBoundsOverlay(sprite, pose, SpriteBoundsCoordinateSpace.FullSpriteBounds, new Color(1f, 1f, 0.25f, 0.7f), "Full");
            DrawWeaponBoundsOverlay(sprite, pose, SpriteBoundsCoordinateSpace.OpaqueContentBounds, new Color(0.25f, 1f, 0.9f, 0.7f), "Opaque");
        }
    }

    private void DrawWeaponBoundsOverlay(Sprite sprite, WeaponAlignmentPose pose, SpriteBoundsCoordinateSpace coordinateSpace, Color color, string label)
    {
        if (!SpriteContentBoundsUtility.TryGetLocalBounds(sprite, coordinateSpace, out Bounds localBounds, out string warning))
        {
            return;
        }

        if (!string.IsNullOrEmpty(warning))
        {
            lastPreviewBoundsWarning = warning;
        }

        ResolveWeaponRendererPreviewTransform(sprite, pose, out Vector3 rendererPosition, out Quaternion rendererRotation, out Vector3 rendererScale);
        Vector3 min = localBounds.min;
        Vector3 max = localBounds.max;
        Vector2 a = WorldToGui(ToVector2(TransformSpriteLocalCorner(min.x, min.y, rendererPosition, rendererRotation, rendererScale)));
        Vector2 b = WorldToGui(ToVector2(TransformSpriteLocalCorner(min.x, max.y, rendererPosition, rendererRotation, rendererScale)));
        Vector2 c = WorldToGui(ToVector2(TransformSpriteLocalCorner(max.x, max.y, rendererPosition, rendererRotation, rendererScale)));
        Vector2 d = WorldToGui(ToVector2(TransformSpriteLocalCorner(max.x, min.y, rendererPosition, rendererRotation, rendererScale)));
        Vector2 labelPosition = (a + b + c + d) * 0.25f;

        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = color;
        Handles.DrawAAPolyLine(2f, a, b, c, d, a);
        Handles.color = previous;
        Handles.EndGUI();

        GUI.Label(new Rect(labelPosition.x + 6f, labelPosition.y - 16f, 52f, 18f), label, EditorStyles.miniLabel);
    }

    private static void DrawProjectileDirectionLine(Vector2 origin, Vector2 direction)
    {
        Vector2 end = origin + new Vector2(direction.x, -direction.y).normalized * 96f;
        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = new Color(1f, 0.45f, 0.2f, 0.8f);
        Handles.DrawAAPolyLine(2f, origin, end);
        Handles.color = previous;
        Handles.EndGUI();
    }

    private static void DrawSlashArc(Vector2 origin, Vector2 start, Vector2 end)
    {
        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = new Color(1f, 0.35f, 0.25f, 0.75f);
        const int segments = 24;
        Vector3 startOffset = start - origin;
        Vector3 endOffset = end - origin;
        Vector3 previousPoint = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = (Vector3)origin + Vector3.Slerp(startOffset, endOffset, t);
            Handles.DrawLine(previousPoint, point);
            previousPoint = point;
        }
        Handles.color = previous;
        Handles.EndGUI();
    }

    private static void DrawDisc(Vector2 position, float radius, Color color)
    {
        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = color;
        Handles.DrawSolidDisc(position, Vector3.forward, radius);
        Handles.color = previous;
        Handles.EndGUI();
    }

    private Vector2 WorldToGui(Vector2 world)
    {
        return lastViewportRect.size * 0.5f + viewPan + WorldToGuiOffset(world);
    }

    private Vector2 GuiToWorld(Vector2 guiPosition)
    {
        float effectiveZoom = GetEffectiveViewZoom();
        Vector2 offset = guiPosition - lastViewportRect.size * 0.5f - viewPan;
        return new Vector2(
            offset.x / (BasePixelsPerWorldUnit * effectiveZoom),
            -offset.y / (BasePixelsPerWorldUnit * effectiveZoom));
    }

    private Vector2 RuntimeWorldToPreviewGui(Vector3 world, Camera gameCamera)
    {
        if (gameCamera != null)
        {
            return CameraWorldToGui(gameCamera, world);
        }

        return hasLastActualRuntimeSnapshot
            ? WorldToGui(ToVector2(world - lastActualRuntimeSnapshot.Player.transform.position))
            : WorldToGui(ToVector2(world));
    }

    private Vector2 CameraWorldToGui(Camera camera, Vector3 world)
    {
        Vector3 viewport = camera.WorldToViewportPoint(world);
        Rect contentRect = GetGameCameraPreviewContentRect(camera);
        return new Vector2(
            contentRect.x + viewport.x * contentRect.width,
            contentRect.y + (1f - viewport.y) * contentRect.height);
    }

    private Vector2 WorldToGuiOffset(Vector2 world)
    {
        float effectiveZoom = GetEffectiveViewZoom();
        return new Vector2(world.x * BasePixelsPerWorldUnit * effectiveZoom, -world.y * BasePixelsPerWorldUnit * effectiveZoom);
    }

    private float GetEffectiveViewZoom()
    {
        return previewSurfaceMode == PreviewSurfaceMode.AuthoringView && workflowMode == WorkflowMode.EditWeaponAlignment
            ? viewZoom * authoringPreviewZoom
            : viewZoom;
    }

    private float GetAuthoringZoomFactor()
    {
        return previewSurfaceMode == PreviewSurfaceMode.AuthoringView && workflowMode == WorkflowMode.EditWeaponAlignment
            ? authoringPreviewZoom
            : 1f;
    }

    private bool TryGetScenePixelsPerWorldUnit(out float pixelsPerWorldUnit, out string source)
    {
        pixelsPerWorldUnit = 0f;
        source = "None";
        Camera camera = GetGamePreviewCamera();
        if (camera == null || !camera.orthographic || camera.orthographicSize <= 0.0001f || camera.pixelHeight <= 0)
        {
            return false;
        }

        pixelsPerWorldUnit = camera.pixelHeight / (camera.orthographicSize * 2f);
        source = $"{camera.name} {camera.pixelWidth}x{camera.pixelHeight} ortho {camera.orthographicSize:0.###}";
        return true;
    }

    private Camera GetGamePreviewCamera()
    {
        if (Camera.main != null)
        {
            return Camera.main;
        }

        return Object.FindFirstObjectByType<Camera>();
    }

    private Rect GetGameCameraPreviewContentRect(Camera camera)
    {
        if (camera == null || lastViewportRect.width <= 1f || lastViewportRect.height <= 1f)
        {
            return new Rect(0f, 0f, lastViewportRect.width, lastViewportRect.height);
        }

        float cameraAspect = camera.pixelHeight > 0 ? camera.pixelWidth / (float)camera.pixelHeight : camera.aspect;
        if (cameraAspect <= 0.0001f)
        {
            cameraAspect = 16f / 9f;
        }

        float viewportAspect = lastViewportRect.width / lastViewportRect.height;
        if (viewportAspect > cameraAspect)
        {
            float width = lastViewportRect.height * cameraAspect;
            return new Rect((lastViewportRect.width - width) * 0.5f, 0f, width, lastViewportRect.height);
        }

        float height = lastViewportRect.width / cameraAspect;
        return new Rect(0f, (lastViewportRect.height - height) * 0.5f, lastViewportRect.width, height);
    }

    private static Vector2 ToVector2(Vector3 value) => new Vector2(value.x, value.y);

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";
    }

    private static string FormatVector2(Vector2 value)
    {
        return $"({value.x:0.###}, {value.y:0.###})";
    }

    private static Vector2 ToSize(Bounds bounds)
    {
        return new Vector2(bounds.size.x, bounds.size.y);
    }

    private static Vector2 CalculateRatio(Vector2 weaponSize, Vector2 playerSize)
    {
        return new Vector2(
            playerSize.x > 0.0001f ? weaponSize.x / playerSize.x : 0f,
            playerSize.y > 0.0001f ? weaponSize.y / playerSize.y : 0f);
    }

    private static string FormatBounds(Bounds bounds)
    {
        return $"center {FormatVector(bounds.center)} size {FormatVector(bounds.size)}";
    }

    private static string FormatRect(Rect rect)
    {
        return $"pos ({rect.x:0.#}, {rect.y:0.#}) size ({rect.width:0.#}, {rect.height:0.#})";
    }

    private static bool IsClose(Vector2 left, Vector2 right, float tolerance)
    {
        return Mathf.Abs(left.x - right.x) <= tolerance && Mathf.Abs(left.y - right.y) <= tolerance;
    }

    private static bool IsZeroVector(Vector3 value)
    {
        return value.sqrMagnitude <= 0.000001f;
    }
}
