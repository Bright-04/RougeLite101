using UnityEditor;
using UnityEngine;

public class WeaponAlignmentEditorWindow : EditorWindow
{
    private const string WeaponAnchorName = "WeaponAnchor";
    private const string RuntimePlayerPrefabPath = "Assets/Prefabs/Scenes Management/Player.prefab";
    private const float BasePixelsPerWorldUnit = 120f;
    private const float ToolbarHeight = 26f;
    private const float LeftPanelWidth = 340f;
    private const float Padding = 8f;

    private WeaponDefinitionSO weaponDefinition;
    private SerializedObject serializedDefinition;
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

    private enum PreviewAimMode
    {
        FreeAim,
        EightDirections
    }

    private WorkflowMode workflowMode = WorkflowMode.EditWeaponAlignment;
    private PreviewAimMode previewAimMode;
    private GameObject previewPlayerPrefab;
    private Sprite previewPlayerSprite;
    private Vector3 previewWeaponAnchorOffset = Vector3.zero;
    private Vector2 settingsScroll;
    private Vector2 viewPan;
    private float viewZoom = 1f;
    private float aimAngle;
    private bool autoTest360Aim;
    private bool useRuntimePlayerPrefab = true;
    private bool showPlayerPreview = true;
    private bool showWeaponAnchor = true;
    private bool showGripAimPoints = true;
    private bool showRuntimePoseDebug = true;
    private bool showWithPlayer = true;
    private bool useActualRuntimeVisualForPreview;
    private bool useGameCameraProjection;
    private bool useMainCameraRenderPreview;
    private bool useNormalizedCalibrationScale;
    private bool advancedFoldout;
    private bool legacyFoldout;
    private Rect lastViewportRect;
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
            WorkflowMode nextWorkflowMode = (WorkflowMode)EditorGUILayout.EnumPopup(workflowMode, EditorStyles.toolbarPopup, GUILayout.Width(190f));
            if (EditorGUI.EndChangeCheck())
            {
                SetWorkflowMode(nextWorkflowMode);
            }

            using (new EditorGUI.DisabledScope(IsMainCameraRenderView()))
            {
                if (GUILayout.Button("Fit All", EditorStyles.toolbarButton, GUILayout.Width(58f))) FitAll();
                if (GUILayout.Button("Fit Player", EditorStyles.toolbarButton, GUILayout.Width(72f))) FitPlayer();
                if (GUILayout.Button("Fit Weapon", EditorStyles.toolbarButton, GUILayout.Width(78f))) FitWeapon();
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
        bool isSlash = mode == WeaponHandlingMode.SlashArc;

        DrawWorkflowHeader();

        EditorGUILayout.Space(6f);
        DrawPropertyWithHelp(
            handlingModeProperty,
            "Handling Mode",
            "Runtime handling mode for this weapon. MVP visuals still orbit around WeaponAnchor based on aim direction.");

        DrawPropertyWithHelp(
            visualScaleProperty,
            "Scale Multiplier",
            "Visual weapon size relative to the player. Final runtime appearance must be checked in Validate Game View.");

        if (workflowMode == WorkflowMode.EditWeaponAlignment)
        {
            EditorGUILayout.HelpBox("Edit Weapon Alignment is the authoring view. It uses the same pose and visual local-scale semantics as runtime; viewport zoom only changes framing.", MessageType.Info);
            if (GUILayout.Button("Validate Scale in Game View"))
            {
                SetWorkflowMode(WorkflowMode.ValidateGameView);
            }
        }

        DrawRigStatus();

        DrawPropertyWithHelp(
            localRotationOffsetProperty,
            "Local Rotation Offset",
            "Correction between sprite authored direction and runtime aim direction.");
        DrawPropertyWithHelp(
            flipBehaviorProperty,
            "Flip Behavior",
            "Optional local scale flip when aiming left.");

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

        DrawFooterStatus();

        advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Legacy / Advanced", true);
        if (advancedFoldout)
        {
            EditorGUILayout.HelpBox("Diagnostics only. Workflow modes set the trusted preview path automatically; these values are shown here so the editor does not hide what it is doing.", MessageType.Info);
            EditorGUILayout.LabelField("Internal Runtime Options", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                useNormalizedCalibrationScale = EditorGUILayout.Toggle("Use Normalized Calibration Scale", useNormalizedCalibrationScale);
                useRuntimePlayerPrefab = EditorGUILayout.Toggle("Use Runtime Player Prefab", useRuntimePlayerPrefab);
                useActualRuntimeVisualForPreview = EditorGUILayout.Toggle("Use Actual Runtime Visual", useActualRuntimeVisualForPreview);
                useGameCameraProjection = EditorGUILayout.Toggle("Use Game Camera Projection", useGameCameraProjection);
                useMainCameraRenderPreview = EditorGUILayout.Toggle("Use Main Camera Render", useMainCameraRenderPreview);
                EditorGUILayout.Toggle("Use Manual Projection Diagnostic", IsManualProjectionDiagnostic());
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Overlay Options", EditorStyles.boldLabel);
            showPlayerPreview = EditorGUILayout.Toggle("Show Player Preview", showPlayerPreview);
            showWithPlayer = GUILayout.Toolbar(showWithPlayer ? 0 : 1, new[] { "Show With Player", "Show Weapon Only" }) == 0;
            showWeaponAnchor = EditorGUILayout.Toggle("Show Weapon Anchor", showWeaponAnchor);
            showGripAimPoints = EditorGUILayout.Toggle("Show Grip/Aim Points", showGripAimPoints);
            showRuntimePoseDebug = EditorGUILayout.Toggle("Show Runtime Pose Debug", showRuntimePoseDebug);
            previewPlayerSprite = (Sprite)EditorGUILayout.ObjectField("Fallback Player Sprite", previewPlayerSprite, typeof(Sprite), false);
            previewWeaponAnchorOffset = EditorGUILayout.Vector3Field("Fallback Anchor Offset", previewWeaponAnchorOffset);

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
                }
            }

            EditorGUILayout.Space(4f);
            DrawEditAlignmentScaleComparison();
            DrawEditorRuntimeComparisonPanel();
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawWorkflowHeader()
    {
        EditorGUILayout.LabelField(GetWorkflowTitle(), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(GetWorkflowPurpose(), workflowMode == WorkflowMode.ValidateGameView ? MessageType.Info : MessageType.Warning);

        if ((workflowMode == WorkflowMode.PreviewRuntimeObject || workflowMode == WorkflowMode.ValidateGameView) && !EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use the runtime Player, WeaponRoot, WeaponAnchor, and CurrentWeaponVisual.", MessageType.Warning);
        }
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
        EditorGUILayout.LabelField("Current Mode", GetWorkflowTitle());
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

            return "Runtime pose via WeaponAlignmentUtility + WeaponRig";
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
            WorkflowMode.EditWeaponAlignment => "Author alignment here. View zoom changes only the viewport, not saved alignment scale.",
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

    private void ApplyWorkflowDefaults()
    {
        useRuntimePlayerPrefab = true;
        previewPlayerPrefab = GetRuntimePlayerPrefab();

        switch (workflowMode)
        {
            case WorkflowMode.EditWeaponAlignment:
                useActualRuntimeVisualForPreview = false;
                useGameCameraProjection = false;
                useMainCameraRenderPreview = false;
                useNormalizedCalibrationScale = false;
                showPlayerPreview = true;
                showWithPlayer = true;
                showWeaponAnchor = true;
                showGripAimPoints = true;
                showRuntimePoseDebug = true;
                break;
            case WorkflowMode.PreviewRuntimeObject:
                useActualRuntimeVisualForPreview = true;
                useGameCameraProjection = false;
                useMainCameraRenderPreview = false;
                useNormalizedCalibrationScale = false;
                showPlayerPreview = true;
                showWithPlayer = true;
                showWeaponAnchor = true;
                showGripAimPoints = true;
                showRuntimePoseDebug = true;
                break;
            case WorkflowMode.ValidateGameView:
                useActualRuntimeVisualForPreview = true;
                useGameCameraProjection = true;
                useMainCameraRenderPreview = true;
                useNormalizedCalibrationScale = false;
                showPlayerPreview = true;
                showWithPlayer = true;
                showWeaponAnchor = false;
                showGripAimPoints = false;
                showRuntimePoseDebug = false;
                break;
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
        return workflowMode switch
        {
            WorkflowMode.EditWeaponAlignment => "Author weapon data in a runtime-equivalent preview. The view camera changes framing only.",
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
    }

    private void EnsureSerializedDefinition()
    {
        if (weaponDefinition == null || serializedDefinition != null)
        {
            return;
        }

        serializedDefinition = new SerializedObject(weaponDefinition);
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
        lastViewportRect = rect;
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

        if (!drewRuntime && showPlayerPreview && showWithPlayer)
        {
            DrawPlayerPreview();
        }

        Sprite sprite = weaponDefinition.ItemImage;
        if (!drewRuntime && sprite != null)
        {
            DrawWeaponSprite(sprite, pose);
        }

        Vector2 weaponAnchorGui = WorldToGui(weaponAnchorWorld);
        Vector2 gripPoint = WorldToGui(ToVector2(pose.GripPoint));
        Vector2 aimPoint = WorldToGui(ToVector2(pose.MuzzleTipPoint));
        Vector2 projectileSpawnPoint = WorldToGui(ToVector2(pose.ProjectileSpawnPoint));
        Vector2 slashOrigin = WorldToGui(ToVector2(pose.SlashOrigin));
        Vector2 slashArcStart = WorldToGui(ToVector2(pose.SlashArcStart));
        Vector2 slashArcEnd = WorldToGui(ToVector2(pose.SlashArcEnd));
        Vector2 weaponPoint = WorldToGui(ToVector2(pose.WeaponPosition));

        if (showRuntimePoseDebug)
        {
            DrawPlayerCenter(WorldToGui(Vector2.zero));
            DrawWeaponPosition(weaponPoint);
            DrawAimVector(weaponAnchorGui, pose.AimDirection);
            if (previewPlayerPrefab != null && !foundAnchor)
            {
                GUI.Label(new Rect(10f, 10f, viewportRect.width - 20f, 18f), "Runtime-created WeaponRoot/WeaponAnchor at Player origin.", EditorStyles.miniLabel);
            }
        }

        if (showWeaponAnchor)
        {
            DrawWeaponAnchor(weaponAnchorGui);
        }

        if (showGripAimPoints)
        {
            DrawGripPoint(gripPoint);
            DrawAimPoint(aimPoint);
            DrawProjectileSpawnPoint(projectileSpawnPoint);
            DrawSlashOrigin(slashOrigin);
            DrawSlashArcStart(slashArcStart);
            DrawSlashArcEnd(slashArcEnd);
        }

        if (mode == WeaponHandlingMode.SlashArc)
        {
            DrawSlashArc(slashOrigin, slashArcStart, slashArcEnd);
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
        GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 60f, rect.width - 20f, 18f), BuildPreviewScaleDebug(), EditorStyles.miniLabel);
        string mode = GetWorkflowTitle();
        string description = GetValidationReminder();
        string navigation = IsMainCameraRenderView()
            ? "Fit/zoom disabled"
            : $"Zoom {viewZoom:0.###} | Pan with middle/right mouse, scroll to zoom";
        GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 42f, rect.width - 20f, 18f), $"{mode} | {navigation}", EditorStyles.miniLabel);
        GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 24f, rect.width - 20f, 18f), description, EditorStyles.miniLabel);
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
        viewZoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), 0.02f, 8f);
        viewPan = -WorldToGuiOffset(bounds.center);
        Repaint();
    }

    private Bounds CalculatePreviewBounds(bool includePlayer, bool includeWeapon)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 0.1f);

        if (includePlayer && previewPlayerPrefab != null)
        {
            Transform root = previewPlayerPrefab.transform;
            foreach (SpriteRenderer renderer in previewPlayerPrefab.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer == null || renderer.sprite == null || !renderer.enabled)
                {
                    continue;
                }

                Encapsulate(ref bounds, ref hasBounds, GetSpriteBounds(renderer.sprite, renderer.transform.position - root.position, renderer.transform.lossyScale));
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
            Vector3 scale = GetSimulatedWeaponVisualLossyScale(pose);
            Encapsulate(ref bounds, ref hasBounds, GetSpriteBounds(weaponDefinition.ItemImage, pose.WeaponPosition, scale));
            Encapsulate(ref bounds, ref hasBounds, new Bounds(pose.GripPoint, Vector3.one * 0.2f));
            Encapsulate(ref bounds, ref hasBounds, new Bounds(pose.MuzzleTipPoint, Vector3.one * 0.2f));
            Encapsulate(ref bounds, ref hasBounds, new Bounds(pose.ProjectileSpawnPoint, Vector3.one * 0.2f));
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
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
        float width = sprite.bounds.size.x * BasePixelsPerWorldUnit * viewZoom * Mathf.Abs(lossyScale.x);
        float height = sprite.bounds.size.y * BasePixelsPerWorldUnit * viewZoom * Mathf.Abs(lossyScale.y);
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
        if (previewPlayerPrefab != null)
        {
            SpriteRenderer[] renderers = previewPlayerPrefab.GetComponentsInChildren<SpriteRenderer>();
            System.Array.Sort(renderers, CompareSpriteRenderers);
            Transform root = previewPlayerPrefab.transform;
            bool drewAnySprite = false;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.sprite == null)
                {
                    continue;
                }

                Vector3 simulatedLossyScale = GetSimulatedPlayerRendererLossyScale(renderer);
                Rect guiRect = DrawSpriteRendererLikeRuntime(renderer.sprite, ToVector2(renderer.transform.position - root.position), renderer.transform.rotation.eulerAngles.z, simulatedLossyScale);
                EncapsulateGuiRect(ref lastPlayerGuiRect, ref hasLastPlayerGuiRect, guiRect);
                Encapsulate(ref lastPlayerWorldBounds, ref hasLastPlayerWorldBounds, GetSpriteBounds(renderer.sprite, renderer.transform.position - root.position, simulatedLossyScale));
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
        Vector3 visualLossyScale = GetSimulatedWeaponVisualLossyScale(pose);
        Vector2 visualPosition = ToVector2(pose.WeaponPosition);
        float visualRotation = pose.WeaponRotation.eulerAngles.z;

        SpriteRenderer prefabRenderer = GetWeaponPrefabSpriteRenderer();
        if (prefabRenderer != null)
        {
            Transform prefabRoot = weaponDefinition.WeaponPrefab.transform;
            Transform rendererTransform = prefabRenderer.transform;
            Vector2 rendererLocalPosition = ToVector2(prefabRoot.InverseTransformPoint(rendererTransform.position));
            float rendererLocalRotation = (Quaternion.Inverse(prefabRoot.rotation) * rendererTransform.rotation).eulerAngles.z;
            Vector3 rendererLocalScale = GetRelativeScaleIncludingRoot(prefabRoot, rendererTransform);
            Quaternion visualRotationQuat = Quaternion.Euler(0f, 0f, visualRotation);
            Vector2 rendererWorldPosition = visualPosition + ToVector2(visualRotationQuat * Vector3.Scale(rendererLocalPosition, Vector3.Scale(visualLossyScale, prefabRoot.localScale)));
            Vector3 rendererWorldScale = Vector3.Scale(visualLossyScale, rendererLocalScale);
            Rect guiRect = DrawSpriteRendererLikeRuntime(sprite, rendererWorldPosition, visualRotation + rendererLocalRotation, rendererWorldScale);
            lastWeaponGuiRect = guiRect;
            hasLastWeaponGuiRect = true;
            lastWeaponWorldBounds = GetWeaponRenderedBounds(sprite, pose);
            return;
        }

        Rect fallbackRect = DrawSpriteRendererLikeRuntime(sprite, visualPosition, visualRotation, visualLossyScale);
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
        GameObject runtimeOrScenePlayer = GetRuntimePlayerObject();
        if (runtimeOrScenePlayer != null)
        {
            return runtimeOrScenePlayer.transform;
        }

        return previewPlayerPrefab != null ? previewPlayerPrefab.transform : null;
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

        return "Scale source: runtime-equivalent authoring preview. View zoom changes only framing.";
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

        if (weaponDefinition == null || weaponDefinition.ItemImage == null || previewPlayerPrefab == null)
        {
            return snapshot;
        }

        SpriteRenderer playerRenderer = WeaponRenderBoundsUtility.GetBodyRenderer(previewPlayerPrefab.transform);
        if (playerRenderer == null || playerRenderer.sprite == null)
        {
            return snapshot;
        }

        snapshot.WeaponBounds = GetWeaponRenderedBounds(weaponDefinition.ItemImage, pose);
        snapshot.PlayerBounds = GetSpriteRendererLikeWorldBounds(
            playerRenderer.sprite,
            playerRenderer.transform.position - previewPlayerPrefab.transform.position,
            playerRenderer.transform.rotation,
            GetSimulatedPlayerRendererLossyScale(playerRenderer));
        snapshot.WeaponRenderedSize = ToSize(snapshot.WeaponBounds);
        snapshot.PlayerRenderedSize = ToSize(snapshot.PlayerBounds);
        snapshot.Ratio = CalculateRatio(snapshot.WeaponRenderedSize, snapshot.PlayerRenderedSize);
        snapshot.WeaponRendererSource = GetWeaponPrefabSpriteRenderer() != null
            ? WeaponRenderBoundsUtility.GetTransformPath(GetWeaponPrefabSpriteRenderer().transform)
            : "WeaponDefinition.ItemImage";
        snapshot.PlayerRendererSource = WeaponRenderBoundsUtility.GetTransformPath(playerRenderer.transform);
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
        Vector2 playerSize = playerRenderer != null && playerRenderer.sprite != null ? GetSpriteWorldSize(playerRenderer.sprite, GetSimulatedPlayerRendererLossyScale(playerRenderer)) : Vector2.zero;
        Vector2 simulatedRatio = new Vector2(
            playerSize.x > 0.0001f ? weaponSize.x / playerSize.x : 0f,
            playerSize.y > 0.0001f ? weaponSize.y / playerSize.y : 0f);
        Vector2 displayedWorldRatio = hasRuntimeSnapshot ? runtimeSnapshot.BoundsReport.Ratio : simulatedRatio;
        Vector2 guiRatio = new Vector2(
            hasLastPlayerGuiRect && lastPlayerGuiRect.width > 0.0001f ? lastWeaponGuiRect.width / lastPlayerGuiRect.width : 0f,
            hasLastPlayerGuiRect && lastPlayerGuiRect.height > 0.0001f ? lastWeaponGuiRect.height / lastPlayerGuiRect.height : 0f);
        Vector2 displayedGuiRatio = hasRuntimeSnapshot ? runtimeSnapshot.GuiRatio : guiRatio;
        bool hasRuntimeContext = previewPlayerPrefab != null && useRuntimePlayerPrefab;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Editor vs Runtime", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Player Lossy Scale", FormatVector(playerLossyScale));
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

        if (previewPlayerPrefab != null)
        {
            Transform root = previewPlayerPrefab.transform;
            foreach (SpriteRenderer renderer in previewPlayerPrefab.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer == null || !renderer.enabled || renderer.sprite == null || IsUnderCurrentWeaponVisual(renderer.transform))
                {
                    continue;
                }

                Vector3 simulatedLossyScale = GetSimulatedPlayerRendererLossyScale(renderer);
                Rect rendererRect = CalculateSpriteGuiRect(renderer.sprite, ToVector2(renderer.transform.position - root.position), simulatedLossyScale);
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
        Vector3 visualLossyScale = GetSimulatedWeaponVisualLossyScale(pose);
        Vector3 rendererPosition = pose.WeaponPosition;
        Quaternion rendererRotation = pose.WeaponRotation;
        Vector3 scale = visualLossyScale;

        SpriteRenderer prefabRenderer = GetWeaponPrefabSpriteRenderer();
        if (prefabRenderer != null)
        {
            Transform prefabRoot = weaponDefinition.WeaponPrefab.transform;
            Transform rendererTransform = prefabRenderer.transform;
            Vector3 rendererLocalPosition = prefabRoot.InverseTransformPoint(rendererTransform.position);
            float rendererLocalRotation = (Quaternion.Inverse(prefabRoot.rotation) * rendererTransform.rotation).eulerAngles.z;
            Vector3 rendererLocalScale = GetRelativeScaleIncludingRoot(prefabRoot, rendererTransform);
            scale = Vector3.Scale(visualLossyScale, rendererLocalScale);
            rendererPosition = pose.WeaponPosition + pose.WeaponRotation * Vector3.Scale(rendererLocalPosition, Vector3.Scale(visualLossyScale, prefabRoot.localScale));
            rendererRotation = pose.WeaponRotation * Quaternion.Euler(0f, 0f, rendererLocalRotation);
        }

        return GetSpriteRendererLikeWorldBounds(sprite, rendererPosition, rendererRotation, scale);
    }

    private Vector3 GetSimulatedPlayerRendererLossyScale(SpriteRenderer renderer)
    {
        if (previewPlayerPrefab == null)
        {
            return Vector3.one;
        }
        return GetSimulatedLossyScale(previewPlayerPrefab.transform, renderer.transform, GetPreviewWeaponParentScale());
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
        return previewPlayerPrefab != null ? previewPlayerPrefab.GetComponentInChildren<SpriteRenderer>() : null;
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
        float step = BasePixelsPerWorldUnit * viewZoom;
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

    private static void DrawPlayerCenter(Vector2 position) => DrawLabeledDisc(position, 7f, new Color(1f, 1f, 1f, 0.9f), "PlayerCenter", new Vector2(8f, 4f));
    private static void DrawGripPoint(Vector2 position) => DrawLabeledDisc(position, 6f, new Color(1f, 0.82f, 0.25f), "GripPoint", new Vector2(8f, -20f));
    private static void DrawAimPoint(Vector2 position) => DrawLabeledDisc(position, 5f, new Color(0.25f, 1f, 0.45f), "Muzzle/Tip", new Vector2(8f, 2f));
    private static void DrawWeaponAnchor(Vector2 position) => DrawLabeledDisc(position, 5f, new Color(0.7f, 0.7f, 0.7f), "WeaponAnchor", new Vector2(8f, 2f));
    private static void DrawWeaponPosition(Vector2 position) => DrawLabeledDisc(position, 5f, new Color(1f, 0.25f, 1f), "WeaponPosition", new Vector2(8f, -20f));
    private static void DrawProjectileSpawnPoint(Vector2 position) => DrawLabeledDisc(position, 4f, new Color(1f, 0.25f, 0.2f), "ProjectileSpawn", new Vector2(8f, 2f));
    private static void DrawSlashOrigin(Vector2 position) => DrawLabeledDisc(position, 4f, new Color(1f, 0.45f, 0.1f), "SlashOrigin", new Vector2(8f, -20f));
    private static void DrawSlashArcStart(Vector2 position) => DrawLabeledDisc(position, 4f, new Color(1f, 0.2f, 0.2f), "SlashArcStart", new Vector2(8f, -20f));
    private static void DrawSlashArcEnd(Vector2 position) => DrawLabeledDisc(position, 4f, new Color(1f, 0.2f, 0.2f), "SlashArcEnd", new Vector2(8f, 2f));

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
        return new Vector2(world.x * BasePixelsPerWorldUnit * viewZoom, -world.y * BasePixelsPerWorldUnit * viewZoom);
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
