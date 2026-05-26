using UnityEditor;
using UnityEngine;

public static class WeaponVisualScaleCalibrationUtility
{
    public const string RuntimePlayerPrefabPath = "Assets/Prefabs/Scenes Management/Player.prefab";
    private const float Epsilon = 0.0001f;

    public readonly struct RatioRange
    {
        public readonly float Min;
        public readonly float Max;
        public readonly float DefaultTarget;

        public RatioRange(float min, float max, float defaultTarget)
        {
            Min = min;
            Max = max;
            DefaultTarget = defaultTarget;
        }

        public override string ToString()
        {
            return $"{Min:0.###} - {Max:0.###}";
        }
    }

    public readonly struct CalibrationReport
    {
        public readonly bool IsValid;
        public readonly string Warning;
        public readonly WeaponDefinitionSO Definition;
        public readonly WeaponArchetype Archetype;
        public readonly RatioRange RecommendedRange;
        public readonly float CurrentVisualScale;
        public readonly float CurrentHeightRatio;
        public readonly float TargetHeightRatio;
        public readonly float SuggestedVisualScale;
        public readonly Vector2 WeaponFinalWorldSize;
        public readonly Vector2 PlayerReferenceWorldSize;
        public readonly string WeaponRendererPath;
        public readonly string PlayerRendererPath;

        public CalibrationReport(
            bool isValid,
            string warning,
            WeaponDefinitionSO definition,
            WeaponArchetype archetype,
            RatioRange recommendedRange,
            float currentVisualScale,
            float currentHeightRatio,
            float targetHeightRatio,
            float suggestedVisualScale,
            Vector2 weaponFinalWorldSize,
            Vector2 playerReferenceWorldSize,
            string weaponRendererPath,
            string playerRendererPath)
        {
            IsValid = isValid;
            Warning = warning;
            Definition = definition;
            Archetype = archetype;
            RecommendedRange = recommendedRange;
            CurrentVisualScale = currentVisualScale;
            CurrentHeightRatio = currentHeightRatio;
            TargetHeightRatio = targetHeightRatio;
            SuggestedVisualScale = suggestedVisualScale;
            WeaponFinalWorldSize = weaponFinalWorldSize;
            PlayerReferenceWorldSize = playerReferenceWorldSize;
            WeaponRendererPath = weaponRendererPath;
            PlayerRendererPath = playerRendererPath;
        }
    }

    public static RatioRange GetRecommendedRatioRange(WeaponArchetype archetype)
    {
        switch (archetype)
        {
            case WeaponArchetype.Dagger:
                return new RatioRange(0.15f, 0.22f, 0.185f);
            case WeaponArchetype.Sword:
                return new RatioRange(0.22f, 0.35f, 0.285f);
            case WeaponArchetype.Axe:
                return new RatioRange(0.25f, 0.38f, 0.315f);
            case WeaponArchetype.Greatsword:
                return new RatioRange(0.35f, 0.50f, 0.425f);
            case WeaponArchetype.Spear:
                return new RatioRange(0.35f, 0.55f, 0.45f);
            case WeaponArchetype.Bow:
                return new RatioRange(0.20f, 0.32f, 0.25f);
            case WeaponArchetype.Staff:
                return new RatioRange(0.30f, 0.48f, 0.39f);
            case WeaponArchetype.Wand:
                return new RatioRange(0.15f, 0.25f, 0.20f);
            case WeaponArchetype.Gun:
                return new RatioRange(0.18f, 0.30f, 0.24f);
            default:
                return new RatioRange(0.20f, 0.35f, 0.275f);
        }
    }

    public static bool TryBuildReport(WeaponDefinitionSO definition, out CalibrationReport report)
    {
        return TryBuildReport(definition, null, out report);
    }

    public static bool TryBuildReport(WeaponDefinitionSO definition, float? targetRatioOverride, out CalibrationReport report)
    {
        report = default;
        if (definition == null)
        {
            return false;
        }

        if (definition.ItemImage == null)
        {
            report = BuildInvalidReport(definition, "WeaponDefinition is missing ItemImage.");
            return false;
        }

        if (definition.WeaponPrefab == null)
        {
            report = BuildInvalidReport(definition, "WeaponDefinition is missing WeaponPrefab.");
            return false;
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePlayerPrefabPath);
        if (playerPrefab == null)
        {
            report = BuildInvalidReport(definition, $"Player prefab not found at '{RuntimePlayerPrefabPath}'.");
            return false;
        }

        SpriteRenderer playerRenderer = WeaponRenderBoundsUtility.GetBodyRenderer(playerPrefab.transform);
        if (playerRenderer == null || playerRenderer.sprite == null)
        {
            report = BuildInvalidReport(definition, "Player prefab is missing a body SpriteRenderer reference.");
            return false;
        }

        WeaponArchetype archetype = definition.ResolvedArchetype;
        RatioRange range = GetRecommendedRatioRange(archetype);
        float currentVisualScale = GetCurrentVisualScale(definition);
        Vector2 playerSize = CalculatePlayerReferenceWorldSize(playerPrefab.transform, playerRenderer);
        Vector2 weaponSize = CalculateWeaponFinalWorldSize(definition, playerPrefab.transform, out string weaponRendererPath);

        if (playerSize.y <= Epsilon)
        {
            report = BuildInvalidReport(definition, "Player reference world height is zero.");
            return false;
        }

        if (weaponSize.y <= Epsilon)
        {
            report = BuildInvalidReport(definition, "Weapon final world height is zero.");
            return false;
        }

        float currentRatio = weaponSize.y / playerSize.y;
        float targetRatio = Mathf.Max(Epsilon, targetRatioOverride ?? range.DefaultTarget);
        float suggestedScale = currentRatio > Epsilon
            ? currentVisualScale * targetRatio / currentRatio
            : currentVisualScale;

        report = new CalibrationReport(
            true,
            null,
            definition,
            archetype,
            range,
            currentVisualScale,
            currentRatio,
            targetRatio,
            suggestedScale,
            weaponSize,
            playerSize,
            weaponRendererPath,
            WeaponRenderBoundsUtility.GetTransformPath(playerRenderer.transform));
        return true;
    }

    public static float GetClampedSuggestedScale(CalibrationReport report, float minScale, float maxScale)
    {
        return Mathf.Clamp(report.SuggestedVisualScale, minScale, maxScale);
    }

    public static bool ShouldScale(CalibrationReport report, bool onlyScaleIfBelowTargetMinimum)
    {
        return report.IsValid
            && (!onlyScaleIfBelowTargetMinimum || report.CurrentHeightRatio < report.RecommendedRange.Min - 0.0005f);
    }

    public static bool ApplyVisualScale(WeaponDefinitionSO definition, float newScale)
    {
        if (definition == null)
        {
            return false;
        }

        SerializedObject serializedObject = new SerializedObject(definition);
        SerializedProperty visualScaleProperty = serializedObject.FindProperty("visualScale");
        if (visualScaleProperty == null)
        {
            return false;
        }

        Undo.RecordObject(definition, "Calibrate Weapon Visual Scale");
        visualScaleProperty.floatValue = newScale;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return true;
    }

    public static string BuildPreviewLine(CalibrationReport report, float suggestedScale)
    {
        if (!report.IsValid)
        {
            return $"{report.Definition?.name ?? "Unknown"} | invalid | {report.Warning}";
        }

        return $"{report.Definition.name} | archetype={report.Archetype} | ratio={report.CurrentHeightRatio:0.###} | target={report.TargetHeightRatio:0.###} | scale {report.CurrentVisualScale:0.###} -> {suggestedScale:0.###}";
    }

    private static CalibrationReport BuildInvalidReport(WeaponDefinitionSO definition, string warning)
    {
        return new CalibrationReport(
            false,
            warning,
            definition,
            definition != null ? definition.ResolvedArchetype : WeaponArchetype.Generic,
            GetRecommendedRatioRange(definition != null ? definition.ResolvedArchetype : WeaponArchetype.Generic),
            definition != null ? GetCurrentVisualScale(definition) : 1f,
            0f,
            0f,
            definition != null ? GetCurrentVisualScale(definition) : 1f,
            Vector2.zero,
            Vector2.zero,
            null,
            null);
    }

    private static float GetCurrentVisualScale(WeaponDefinitionSO definition)
    {
        return definition != null && definition.VisualScale > 0f && float.IsFinite(definition.VisualScale)
            ? definition.VisualScale
            : 1f;
    }

    private static Vector2 CalculatePlayerReferenceWorldSize(Transform playerRoot, SpriteRenderer playerRenderer)
    {
        Quaternion localRotation = Quaternion.Inverse(playerRoot.rotation) * playerRenderer.transform.rotation;
        Vector3 lossyScale = GetSimulatedLossyScale(playerRoot, playerRenderer.transform, playerRoot.localScale);
        Bounds bounds = GetSpriteRendererLikeWorldBounds(playerRenderer.sprite, Vector3.zero, localRotation, lossyScale);
        return new Vector2(bounds.size.x, bounds.size.y);
    }

    private static Vector2 CalculateWeaponFinalWorldSize(WeaponDefinitionSO definition, Transform playerRoot, out string rendererPath)
    {
        rendererPath = null;
        Sprite weaponSprite = definition.ItemImage;
        Vector3 visualLossyScale = Vector3.Scale(WeaponAlignmentUtility.CalculateVisualScale(Vector2.right, definition), playerRoot.localScale);
        Quaternion rendererRotation = Quaternion.Euler(0f, 0f, definition.LocalRotationOffset.z);
        Vector3 rendererScale = visualLossyScale;

        SpriteRenderer prefabRenderer = GetWeaponPrefabSpriteRenderer(definition);
        if (prefabRenderer != null)
        {
            Transform prefabRoot = definition.WeaponPrefab.transform;
            float rendererLocalRotation = (Quaternion.Inverse(prefabRoot.rotation) * prefabRenderer.transform.rotation).eulerAngles.z;
            Vector3 rendererLocalScale = GetRelativeScaleIncludingRoot(prefabRoot, prefabRenderer.transform);
            rendererRotation *= Quaternion.Euler(0f, 0f, rendererLocalRotation);
            rendererScale = Vector3.Scale(visualLossyScale, rendererLocalScale);
            rendererPath = WeaponRenderBoundsUtility.GetTransformPath(prefabRenderer.transform);
        }

        Bounds bounds = GetSpriteRendererLikeWorldBounds(weaponSprite, Vector3.zero, rendererRotation, rendererScale);
        return new Vector2(bounds.size.x, bounds.size.y);
    }

    private static SpriteRenderer GetWeaponPrefabSpriteRenderer(WeaponDefinitionSO definition)
    {
        if (definition == null || definition.WeaponPrefab == null)
        {
            return null;
        }

        SpriteRenderer rootRenderer = definition.WeaponPrefab.GetComponent<SpriteRenderer>();
        return rootRenderer != null ? rootRenderer : definition.WeaponPrefab.GetComponentInChildren<SpriteRenderer>(true);
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

    private static Vector3 TransformSpriteLocalCorner(float x, float y, Vector3 transformWorldPosition, Quaternion rotation, Vector3 lossyScale)
    {
        Vector3 local = new Vector3(x * lossyScale.x, y * lossyScale.y, 0f);
        return transformWorldPosition + rotation * local;
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
}
