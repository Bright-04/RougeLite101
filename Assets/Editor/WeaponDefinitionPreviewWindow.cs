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
    private bool alignmentFoldout = true;
    private bool playerFoldout = true;
    private bool wysiwygFoldout = true;
    private bool debugFoldout = true;
    private bool advancedFoldout;
    private Rect lastViewportRect;

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
            EditorGUILayout.PropertyField(handlingModeProperty, GUIContent.none, GUILayout.Width(120f));
            useRuntimePlayerPrefab = GUILayout.Toggle(useRuntimePlayerPrefab, "Runtime Player", EditorStyles.toolbarButton, GUILayout.Width(105f));
            if (GUILayout.Button("Fit All", EditorStyles.toolbarButton, GUILayout.Width(58f))) FitAll();
            if (GUILayout.Button("Fit Player", EditorStyles.toolbarButton, GUILayout.Width(72f))) FitPlayer();
            if (GUILayout.Button("Fit Weapon", EditorStyles.toolbarButton, GUILayout.Width(78f))) FitWeapon();
            if (GUILayout.Button("Reset View", EditorStyles.toolbarButton, GUILayout.Width(78f))) ResetView();
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
        bool isAimAligned = mode == WeaponHandlingMode.AimAligned;
        bool isSlash = mode == WeaponHandlingMode.SlashArc;

        alignmentFoldout = EditorGUILayout.Foldout(alignmentFoldout, "Alignment", true);
        if (alignmentFoldout)
        {
            EditorGUILayout.PropertyField(gripPointOffsetProperty, new GUIContent("Grip Point Offset"));
            EditorGUILayout.PropertyField(aimPointOffsetProperty, new GUIContent("Aim / Muzzle / Tip Offset"));
            EditorGUILayout.PropertyField(localPositionOffsetProperty, new GUIContent("Local Position Offset"));
            EditorGUILayout.PropertyField(localRotationOffsetProperty, new GUIContent("Local Rotation Offset"));
            EditorGUILayout.PropertyField(visualScaleProperty, new GUIContent("Scale Multiplier"));
            EditorGUILayout.PropertyField(flipBehaviorProperty, new GUIContent("Flip Behavior"));
            aimAngle = EditorGUILayout.Slider("Preview Aim Angle", aimAngle, -180f, 180f);
            autoTest360Aim = EditorGUILayout.Toggle("Auto Test 360 Aim", autoTest360Aim);
            if (autoTest360Aim)
            {
                aimAngle = Mathf.Repeat((float)EditorApplication.timeSinceStartup * 90f, 360f) - 180f;
                Repaint();
            }
        }

        playerFoldout = EditorGUILayout.Foldout(playerFoldout, "Player Preview", true);
        if (playerFoldout)
        {
            useRuntimePlayerPrefab = EditorGUILayout.Toggle("Use Runtime Player Prefab", useRuntimePlayerPrefab);
            if (useRuntimePlayerPrefab)
            {
                previewPlayerPrefab = GetRuntimePlayerPrefab();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Runtime Preview Player", previewPlayerPrefab, typeof(GameObject), false);
                }
            }

            using (new EditorGUI.DisabledScope(useRuntimePlayerPrefab))
            {
                previewPlayerPrefab = (GameObject)EditorGUILayout.ObjectField("Preview Player Prefab", previewPlayerPrefab, typeof(GameObject), false);
            }

            previewPlayerSprite = (Sprite)EditorGUILayout.ObjectField("Preview Player Sprite", previewPlayerSprite, typeof(Sprite), false);
            previewWeaponAnchorOffset = EditorGUILayout.Vector3Field("Fallback Anchor Offset", previewWeaponAnchorOffset);
            showPlayerPreview = EditorGUILayout.Toggle("Show Player Preview", showPlayerPreview);
            showWithPlayer = GUILayout.Toolbar(showWithPlayer ? 0 : 1, new[] { "Show With Player", "Show Weapon Only" }) == 0;
        }

        wysiwygFoldout = EditorGUILayout.Foldout(wysiwygFoldout, "Runtime / WYSIWYG", true);
        if (wysiwygFoldout)
        {
            EditorGUILayout.HelpBox("Runtime Player Prefab keeps the preview in the same visual scale context as Play Mode. Use viewport Fit/Zoom/Pan to frame the large player, not asset scale edits.", MessageType.Info);
            EditorGUILayout.LabelField("Viewport Zoom", viewZoom.ToString("0.###"));
            viewZoom = EditorGUILayout.Slider(viewZoom, 0.02f, 8f);
            if (GUILayout.Button("Fit All")) FitAll();
        }

        debugFoldout = EditorGUILayout.Foldout(debugFoldout, "Debug", true);
        if (debugFoldout)
        {
            showWeaponAnchor = EditorGUILayout.Toggle("Show Weapon Anchor", showWeaponAnchor);
            showGripAimPoints = EditorGUILayout.Toggle("Show Grip/Aim Points", showGripAimPoints);
            showRuntimePoseDebug = EditorGUILayout.Toggle("Show Runtime Pose Debug", showRuntimePoseDebug);
            DrawEditorRuntimeComparisonPanel();
        }

        advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced", true);
        if (advancedFoldout)
        {
            if (isAimAligned)
            {
                EditorGUILayout.PropertyField(projectileSpawnPointOffsetProperty, new GUIContent("Projectile Spawn Offset"));
            }

            if (isSlash)
            {
                EditorGUILayout.PropertyField(slashVfxOffsetProperty, new GUIContent("Slash VFX Offset"));
            }
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static GameObject GetRuntimePlayerPrefab()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePlayerPrefabPath);
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
        Vector2 weaponAnchorWorld = GetPreviewWeaponAnchorWorld(out bool foundAnchor);
        Vector2 aimDirection = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        WeaponAlignmentPose pose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchorWorld, aimDirection, weaponDefinition);

        DrawGrid(viewportRect);
        if (showPlayerPreview && showWithPlayer)
        {
            DrawPlayerPreview();
        }

        Sprite sprite = weaponDefinition.ItemImage;
        if (sprite != null)
        {
            DrawSprite(sprite, WorldToGui(ToVector2(pose.WeaponPosition)), pose.WeaponRotation.eulerAngles.z, GetSimulatedWeaponVisualLossyScale(pose));
        }

        Vector2 weaponAnchorGui = WorldToGui(weaponAnchorWorld);
        Vector2 gripPoint = WorldToGui(ToVector2(pose.GripPoint));
        Vector2 aimPoint = WorldToGui(ToVector2(pose.MuzzleTipPoint));
        Vector2 projectileSpawnPoint = WorldToGui(ToVector2(pose.ProjectileSpawnPoint));
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
        }

        if (mode == WeaponHandlingMode.SlashArc)
        {
            DrawSlashArc(weaponAnchorGui);
        }
    }

    private void DrawViewportOverlay(Rect rect)
    {
        GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 42f, rect.width - 20f, 18f), BuildPreviewScaleDebug(weaponDefinition != null ? weaponDefinition.ItemImage : null, GetCurrentPoseScale()), EditorStyles.miniLabel);
        GUI.Label(new Rect(rect.x + 10f, rect.y + rect.height - 24f, rect.width - 20f, 18f), $"Zoom {viewZoom:0.###} | Pan with middle/right mouse, scroll to zoom | Runtime sorting: weapon above player (+1)", EditorStyles.miniLabel);
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
            WeaponAlignmentPose pose = WeaponAlignmentUtility.CalculateWeaponPose(anchor, aim, weaponDefinition);
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

    private static Bounds GetSpriteBounds(Sprite sprite, Vector3 center, Vector3 scale)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : 100f;
        Vector3 size = new Vector3(
            sprite.rect.width / pixelsPerUnit * Mathf.Abs(scale.x),
            sprite.rect.height / pixelsPerUnit * Mathf.Abs(scale.y),
            0.1f);
        return new Bounds(center, size);
    }

    private void DrawSprite(Sprite sprite, Vector2 position, float rotation, Vector3 visualScale)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : 100f;
        float width = sprite.rect.width / pixelsPerUnit * BasePixelsPerWorldUnit * viewZoom * Mathf.Abs(visualScale.x);
        float height = sprite.rect.height / pixelsPerUnit * BasePixelsPerWorldUnit * viewZoom * Mathf.Abs(visualScale.y);
        Rect spriteRect = new Rect(position.x - width * 0.5f, position.y - height * 0.5f, width, height);

        Texture2D texture = sprite.texture;
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height, textureRect.width / texture.width, textureRect.height / texture.height);

        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(-rotation, position);
        if (visualScale.x < 0f || visualScale.y < 0f)
        {
            GUIUtility.ScaleAroundPivot(new Vector2(visualScale.x < 0f ? -1f : 1f, visualScale.y < 0f ? -1f : 1f), position);
        }
        GUI.DrawTextureWithTexCoords(spriteRect, texture, uv, true);
        GUI.matrix = previousMatrix;
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

                DrawSprite(renderer.sprite, WorldToGui(ToVector2(renderer.transform.position - root.position)), renderer.transform.rotation.eulerAngles.z, renderer.transform.lossyScale);
                drewAnySprite = true;
            }

            if (drewAnySprite)
            {
                return;
            }
        }

        if (previewPlayerSprite != null)
        {
            DrawSprite(previewPlayerSprite, WorldToGui(Vector2.zero), 0f, Vector3.one);
        }
    }

    private Vector2 GetPreviewWeaponAnchorWorld(out bool foundAnchor)
    {
        foundAnchor = false;
        if (previewPlayerPrefab != null)
        {
            Transform root = previewPlayerPrefab.transform;
            Transform anchor = FindChildRecursive(root, WeaponAnchorName);
            if (anchor != null)
            {
                foundAnchor = true;
                return ToVector2(anchor.position - root.position);
            }
        }

        return previewPlayerPrefab != null ? Vector2.zero : ToVector2(previewWeaponAnchorOffset);
    }

    private Vector3 GetPreviewWeaponParentScale()
    {
        return previewPlayerPrefab != null ? previewPlayerPrefab.transform.lossyScale : Vector3.one;
    }

    private Vector3 GetSimulatedWeaponRootLossyScale()
    {
        return GetPreviewWeaponParentScale();
    }

    private Vector3 GetSimulatedWeaponVisualLocalScale(WeaponAlignmentPose pose)
    {
        return pose.VisualScale;
    }

    private Vector3 GetSimulatedWeaponVisualLossyScale(WeaponAlignmentPose pose)
    {
        return Vector3.Scale(GetSimulatedWeaponRootLossyScale(), GetSimulatedWeaponVisualLocalScale(pose));
    }

    private string GetSimulatedActiveWeaponVisualName()
    {
        return weaponDefinition != null ? "CurrentWeaponVisual_" + weaponDefinition.WeaponId : "None";
    }

    private string BuildPreviewScaleDebug(Sprite weaponSprite, Vector3 weaponLocalScale)
    {
        SpriteRenderer playerRenderer = GetPreviewPlayerRenderer();
        if (weaponSprite == null || playerRenderer == null || playerRenderer.sprite == null)
        {
            return "Runtime scale preview: missing weapon or player sprite.";
        }

        Vector3 parentScale = GetPreviewWeaponParentScale();
        Vector2 weaponSize = GetSpriteWorldSize(weaponSprite, Vector3.Scale(weaponLocalScale, parentScale));
        Vector2 playerSize = GetSpriteWorldSize(playerRenderer.sprite, playerRenderer.transform.lossyScale);
        Vector2 ratio = new Vector2(
            playerSize.x > 0.0001f ? weaponSize.x / playerSize.x : 0f,
            playerSize.y > 0.0001f ? weaponSize.y / playerSize.y : 0f);

        return $"Runtime scale preview: weapon/player ratio {ratio.x:0.###}, {ratio.y:0.###} | playerScale {FormatVector(parentScale)}";
    }

    private void DrawEditorRuntimeComparisonPanel()
    {
        if (weaponDefinition == null)
        {
            return;
        }

        Vector2 anchor = GetPreviewWeaponAnchorWorld(out _);
        Vector2 aim = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
        WeaponAlignmentPose pose = WeaponAlignmentUtility.CalculateWeaponPose(anchor, aim, weaponDefinition);
        SpriteRenderer playerRenderer = GetPreviewPlayerRenderer();
        Sprite weaponSprite = weaponDefinition.ItemImage;
        Vector3 playerLossyScale = previewPlayerPrefab != null ? previewPlayerPrefab.transform.lossyScale : Vector3.one;
        Vector3 weaponRootLossyScale = GetSimulatedWeaponRootLossyScale();
        Vector3 weaponVisualLocalScale = GetSimulatedWeaponVisualLocalScale(pose);
        Vector3 weaponVisualLossyScale = GetSimulatedWeaponVisualLossyScale(pose);
        Vector2 weaponSize = weaponSprite != null ? GetSpriteWorldSize(weaponSprite, weaponVisualLossyScale) : Vector2.zero;
        Vector2 playerSize = playerRenderer != null && playerRenderer.sprite != null ? GetSpriteWorldSize(playerRenderer.sprite, playerRenderer.transform.lossyScale) : Vector2.zero;
        Vector2 ratio = new Vector2(
            playerSize.x > 0.0001f ? weaponSize.x / playerSize.x : 0f,
            playerSize.y > 0.0001f ? weaponSize.y / playerSize.y : 0f);
        bool hasRuntimeContext = previewPlayerPrefab != null && useRuntimePlayerPrefab;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Editor vs Runtime", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Player Lossy Scale", FormatVector(playerLossyScale));
        EditorGUILayout.LabelField("WeaponRoot Lossy Scale", FormatVector(weaponRootLossyScale));
        EditorGUILayout.LabelField("Weapon Visual Local Scale", FormatVector(weaponVisualLocalScale));
        EditorGUILayout.LabelField("Weapon Visual Lossy Scale", FormatVector(weaponVisualLossyScale));
        EditorGUILayout.LabelField("Weapon Sprite Bounds", weaponSprite != null ? FormatVector2(GetSpriteWorldSize(weaponSprite, Vector3.one)) : "None");
        EditorGUILayout.LabelField("Rendered Ratio", FormatVector2(ratio));
        EditorGUILayout.LabelField("Active Visual", GetSimulatedActiveWeaponVisualName());
        EditorGUILayout.LabelField("Mismatch Status", hasRuntimeContext ? "OK: runtime prefab context" : "Risk: not using runtime prefab");
    }

    private SpriteRenderer GetPreviewPlayerRenderer()
    {
        return previewPlayerPrefab != null ? previewPlayerPrefab.GetComponentInChildren<SpriteRenderer>() : null;
    }

    private static Vector2 GetSpriteWorldSize(Sprite sprite, Vector3 scale)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : 100f;
        return new Vector2(sprite.rect.width / pixelsPerUnit * Mathf.Abs(scale.x), sprite.rect.height / pixelsPerUnit * Mathf.Abs(scale.y));
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

    private static void DrawSlashArc(Vector2 origin)
    {
        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = new Color(1f, 0.35f, 0.25f, 0.75f);
        const float radius = 95f;
        const int segments = 24;
        Vector3 previousPoint = origin + new Vector2(Mathf.Cos(-55f * Mathf.Deg2Rad), -Mathf.Sin(-55f * Mathf.Deg2Rad)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-55f, 55f, i / (float)segments);
            Vector3 point = origin + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), -Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
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

    private Vector2 WorldToGuiOffset(Vector2 world)
    {
        return new Vector2(world.x * BasePixelsPerWorldUnit * viewZoom, -world.y * BasePixelsPerWorldUnit * viewZoom);
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
}
