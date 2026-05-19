using UnityEditor;
using UnityEngine;

public class WeaponAlignmentEditorWindow : EditorWindow
{
    private const string WeaponAnchorName = "WeaponAnchor";
    private const float PixelsPerWorldUnit = 120f;
    private const float Padding = 20f;

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
    private float zoom = 1f;
    private float aimAngle;
    private GameObject previewPlayerPrefab;
    private Sprite previewPlayerSprite;
    private Vector3 previewWeaponAnchorOffset = Vector3.zero;
    private bool showPlayerPreview = true;
    private bool showWeaponAnchor = true;
    private bool showGripAimPoints = true;
    private bool showRuntimePoseDebug = true;
    private bool showWithPlayer = true;
    private bool autoTest360Aim;

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
        EditorGUI.BeginChangeCheck();
        WeaponDefinitionSO selected = (WeaponDefinitionSO)EditorGUILayout.ObjectField("Weapon Definition", weaponDefinition, typeof(WeaponDefinitionSO), false);
        if (EditorGUI.EndChangeCheck())
        {
            SetDefinition(selected);
        }

        if (weaponDefinition == null)
        {
            EditorGUILayout.HelpBox("Select a WeaponDefinitionSO asset.", MessageType.Info);
            return;
        }

        EnsureSerializedDefinition();
        serializedDefinition.Update();

        WeaponHandlingMode mode = (WeaponHandlingMode)handlingModeProperty.enumValueIndex;
        bool isAimAligned = mode == WeaponHandlingMode.AimAligned;
        bool isSlash = mode == WeaponHandlingMode.SlashArc;

        EditorGUILayout.LabelField("Weapon Alignment Editor", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(handlingModeProperty, new GUIContent("Preview Mode"));
        EditorGUILayout.PropertyField(gripPointOffsetProperty, new GUIContent("Grip Point Offset"));
        EditorGUILayout.PropertyField(aimPointOffsetProperty, new GUIContent("Aim / Muzzle / Tip Point Offset"));
        EditorGUILayout.PropertyField(localPositionOffsetProperty, new GUIContent("Local Position Offset"));
        EditorGUILayout.PropertyField(localRotationOffsetProperty, new GUIContent("Local Rotation Offset"));
        EditorGUILayout.PropertyField(visualScaleProperty, new GUIContent("Scale Multiplier"));
        EditorGUILayout.PropertyField(flipBehaviorProperty, new GUIContent("Flip Behavior"));

        if (isAimAligned)
        {
            EditorGUILayout.PropertyField(projectileSpawnPointOffsetProperty, new GUIContent("Projectile Spawn Point Offset"));
        }

        if (isSlash)
        {
            EditorGUILayout.PropertyField(slashVfxOffsetProperty, new GUIContent("Slash VFX Offset"));
        }

        zoom = EditorGUILayout.Slider("Zoom", zoom, 0.5f, 4f);

        aimAngle = EditorGUILayout.Slider("Preview Aim Angle", aimAngle, -180f, 180f);
        autoTest360Aim = EditorGUILayout.Toggle("Auto Test 360 Aim", autoTest360Aim);
        if (autoTest360Aim)
        {
            aimAngle = Mathf.Repeat((float)EditorApplication.timeSinceStartup * 90f, 360f) - 180f;
            Repaint();
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Player Preview", EditorStyles.boldLabel);
        previewPlayerPrefab = (GameObject)EditorGUILayout.ObjectField("Preview Player Prefab", previewPlayerPrefab, typeof(GameObject), false);
        previewPlayerSprite = (Sprite)EditorGUILayout.ObjectField("Preview Player Sprite", previewPlayerSprite, typeof(Sprite), false);
        previewWeaponAnchorOffset = EditorGUILayout.Vector3Field("Preview Weapon Anchor Offset", previewWeaponAnchorOffset);
        showPlayerPreview = EditorGUILayout.Toggle("Show Player Preview", showPlayerPreview);
        showWeaponAnchor = EditorGUILayout.Toggle("Show Weapon Anchor", showWeaponAnchor);
        showGripAimPoints = EditorGUILayout.Toggle("Show Grip/Aim Points", showGripAimPoints);
        showRuntimePoseDebug = EditorGUILayout.Toggle("Show Runtime Pose Debug", showRuntimePoseDebug);
        showWithPlayer = GUILayout.Toolbar(showWithPlayer ? 0 : 1, new[] { "Show With Player", "Show Weapon Only" }) == 0;

        serializedDefinition.ApplyModifiedProperties();
        EditorGUILayout.Space(8f);
        DrawPreview(mode);
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

    private void DrawPreview(WeaponHandlingMode mode)
    {
        Rect rect = GUILayoutUtility.GetRect(260f, 400f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

        Rect inner = new Rect(rect.x + Padding, rect.y + Padding, rect.width - Padding * 2f, rect.height - Padding * 2f);
        Vector2 playerCenter = inner.center;
        Vector2 weaponAnchorWorld = GetPreviewWeaponAnchorWorld(out bool foundAnchor);

        DrawGrid(inner, playerCenter);
        if (showPlayerPreview && showWithPlayer)
        {
            DrawPlayerPreview(playerCenter);
            if (previewPlayerPrefab != null && !foundAnchor)
            {
                GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 20f), "Preview player prefab has no WeaponAnchor. Using Preview Weapon Anchor Offset.", EditorStyles.miniLabel);
            }
        }

        float vectorAngle = aimAngle;
        Vector2 aimDirection = new Vector2(Mathf.Cos(vectorAngle * Mathf.Deg2Rad), Mathf.Sin(vectorAngle * Mathf.Deg2Rad));
        WeaponAlignmentPose pose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchorWorld, aimDirection, weaponDefinition);

        Vector2 weaponAnchorGui = playerCenter + WorldToGui(weaponAnchorWorld);
        Vector2 gripPoint = playerCenter + WorldToGui(ToVector2(pose.GripPoint));
        Vector2 aimPoint = playerCenter + WorldToGui(ToVector2(pose.MuzzleTipPoint));
        Vector2 projectileSpawnPoint = playerCenter + WorldToGui(ToVector2(pose.ProjectileSpawnPoint));
        Vector2 weaponPoint = playerCenter + WorldToGui(ToVector2(pose.WeaponPosition));

        Sprite sprite = weaponDefinition.ItemImage;
        if (sprite != null)
        {
            DrawSprite(sprite, weaponPoint, pose.WeaponRotation.eulerAngles.z);
        }

        if (showRuntimePoseDebug)
        {
            DrawPlayerCenter(playerCenter);
            DrawWeaponPosition(weaponPoint);
            DrawAimVector(weaponAnchorGui, pose.AimDirection);
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

    private void DrawSprite(Sprite sprite, Vector2 position, float rotation)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : 100f;
        float scale = Mathf.Max(0.01f, visualScaleProperty.floatValue);
        float width = sprite.rect.width / pixelsPerUnit * PixelsPerWorldUnit * zoom * scale;
        float height = sprite.rect.height / pixelsPerUnit * PixelsPerWorldUnit * zoom * scale;
        Rect spriteRect = new Rect(position.x - width * 0.5f, position.y - height * 0.5f, width, height);

        Texture2D texture = sprite.texture;
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height, textureRect.width / texture.width, textureRect.height / texture.height);

        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(-rotation, position);
        GUI.DrawTextureWithTexCoords(spriteRect, texture, uv, true);
        GUI.matrix = previousMatrix;
    }

    private void DrawPlayerPreview(Vector2 playerCenter)
    {
        if (previewPlayerPrefab != null)
        {
            SpriteRenderer[] renderers = previewPlayerPrefab.GetComponentsInChildren<SpriteRenderer>();
            System.Array.Sort(renderers, CompareSpriteRenderers);

            bool drewAnySprite = false;
            Transform root = previewPlayerPrefab.transform;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.sprite == null)
                {
                    continue;
                }

                Transform rendererTransform = renderer.transform;
                Vector3 localPosition = root.InverseTransformPoint(rendererTransform.position);
                Quaternion localRotation = Quaternion.Inverse(root.rotation) * rendererTransform.rotation;
                Vector3 relativeScale = GetRelativeScale(root, rendererTransform);
                DrawPreviewSprite(
                    renderer.sprite,
                    playerCenter + WorldToGui(localPosition),
                    localRotation.eulerAngles.z,
                    relativeScale,
                    1f);
                drewAnySprite = true;
            }

            if (drewAnySprite)
            {
                return;
            }
        }

        if (previewPlayerSprite != null)
        {
            DrawPreviewSprite(previewPlayerSprite, playerCenter, 0f, Vector3.one, 1f);
        }
    }

    private void DrawPreviewSprite(Sprite sprite, Vector2 position, float rotation, Vector3 localScale, float scaleMultiplier)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : 100f;
        float width = sprite.rect.width / pixelsPerUnit * PixelsPerWorldUnit * zoom * Mathf.Abs(localScale.x) * scaleMultiplier;
        float height = sprite.rect.height / pixelsPerUnit * PixelsPerWorldUnit * zoom * Mathf.Abs(localScale.y) * scaleMultiplier;
        Rect spriteRect = new Rect(position.x - width * 0.5f, position.y - height * 0.5f, width, height);

        Texture2D texture = sprite.texture;
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height, textureRect.width / texture.width, textureRect.height / texture.height);

        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(-rotation, position);
        GUI.DrawTextureWithTexCoords(spriteRect, texture, uv, true);
        GUI.matrix = previousMatrix;
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
                return ToVector2(root.InverseTransformPoint(anchor.position));
            }
        }

        return ToVector2(previewWeaponAnchorOffset);
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
        if (layerCompare != 0)
        {
            return layerCompare;
        }

        return left.sortingOrder.CompareTo(right.sortingOrder);
    }

    private static Vector3 GetRelativeScale(Transform root, Transform child)
    {
        Vector3 rootScale = root.lossyScale;
        Vector3 childScale = child.lossyScale;
        return new Vector3(
            SafeDivide(childScale.x, rootScale.x),
            SafeDivide(childScale.y, rootScale.y),
            SafeDivide(childScale.z, rootScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
    }

    private static void DrawGrid(Rect rect, Vector2 origin)
    {
        Handles.BeginGUI();
        Color previous = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, 0.08f);
        for (float x = rect.xMin; x <= rect.xMax; x += 24f) Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));
        for (float y = rect.yMin; y <= rect.yMax; y += 24f) Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
        Handles.color = new Color(1f, 1f, 1f, 0.25f);
        Handles.DrawLine(new Vector3(origin.x, rect.yMin), new Vector3(origin.x, rect.yMax));
        Handles.DrawLine(new Vector3(rect.xMin, origin.y), new Vector3(rect.xMax, origin.y));
        Handles.color = previous;
        Handles.EndGUI();
    }

    private static void DrawPlayerCenter(Vector2 position)
    {
        DrawDisc(position, 7f, new Color(1f, 1f, 1f, 0.9f));
        GUI.Label(new Rect(position.x + 8f, position.y + 4f, 80f, 18f), "PlayerCenter", EditorStyles.miniLabel);
    }

    private static void DrawGripPoint(Vector2 position)
    {
        DrawDisc(position, 6f, new Color(1f, 0.82f, 0.25f));
        GUI.Label(new Rect(position.x + 8f, position.y - 20f, 70f, 18f), "GripPoint", EditorStyles.miniLabel);
    }

    private static void DrawAimPoint(Vector2 position)
    {
        DrawDisc(position, 5f, new Color(0.25f, 1f, 0.45f));
        GUI.Label(new Rect(position.x + 8f, position.y + 2f, 100f, 18f), "Muzzle/Tip", EditorStyles.miniLabel);
    }

    private static void DrawWeaponAnchor(Vector2 position)
    {
        DrawDisc(position, 5f, new Color(0.7f, 0.7f, 0.7f));
        GUI.Label(new Rect(position.x + 8f, position.y + 2f, 100f, 18f), "WeaponAnchor", EditorStyles.miniLabel);
    }

    private static void DrawWeaponPosition(Vector2 position)
    {
        DrawDisc(position, 5f, new Color(1f, 0.25f, 1f));
        GUI.Label(new Rect(position.x + 8f, position.y - 20f, 110f, 18f), "WeaponPosition", EditorStyles.miniLabel);
    }

    private static void DrawProjectileSpawnPoint(Vector2 position)
    {
        DrawDisc(position, 4f, new Color(1f, 0.25f, 0.2f));
        GUI.Label(new Rect(position.x + 8f, position.y + 2f, 110f, 18f), "ProjectileSpawn", EditorStyles.miniLabel);
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
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-55f, 55f, t);
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
        return new Vector2(world.x * PixelsPerWorldUnit * zoom, -world.y * PixelsPerWorldUnit * zoom);
    }

    private static Vector2 ToVector2(Vector3 value)
    {
        return new Vector2(value.x, value.y);
    }

}
