using UnityEngine;

public struct WeaponAlignmentPose
{
    public Vector3 WeaponAnchorPosition;
    public Vector3 GripPoint;
    public Vector3 WeaponPosition;
    public Vector3 MuzzleTipPoint;
    public Vector3 ProjectileSpawnPoint;
    public Vector3 SlashOrigin;
    public Vector3 SlashArcStart;
    public Vector3 SlashArcEnd;
    public Vector2 AimDirection;
    public float AimAngle;
    public Quaternion WeaponRotation;
    public Vector3 VisualScale;
}

public static class WeaponAlignmentUtility
{
    private const float MinVisualScale = 0.05f;
    private const float MaxLocalVisualScale = 8f;

    public static void ApplyPoseToVisualTransform(Transform visualTransform, WeaponAlignmentPose pose)
    {
        if (visualTransform == null)
        {
            return;
        }

        visualTransform.position = pose.WeaponPosition;
        visualTransform.rotation = pose.WeaponRotation;
        visualTransform.localScale = GetLocalScaleForTargetLossyScale(visualTransform, pose.VisualScale);
    }

    public static Vector3 GetLocalScaleForTargetLossyScale(Transform visualTransform, Vector3 targetLossyScale)
    {
        if (visualTransform == null || visualTransform.parent == null)
        {
            return targetLossyScale;
        }

        Vector3 parentScale = visualTransform.parent.lossyScale;
        return new Vector3(
            DivideScaleAxis(targetLossyScale.x, parentScale.x),
            DivideScaleAxis(targetLossyScale.y, parentScale.y),
            DivideScaleAxis(targetLossyScale.z, parentScale.z));
    }

    private static float DivideScaleAxis(float targetScale, float parentScale)
    {
        return Mathf.Abs(parentScale) > 0.0001f ? targetScale / parentScale : targetScale;
    }

    public static WeaponAlignmentPose CalculateWeaponPose(Vector2 weaponAnchorPosition, Vector2 aimDirection, WeaponDefinitionSO weapon)
    {
        return CalculateWeaponPose(weaponAnchorPosition, aimDirection, weapon, null);
    }

    public static WeaponAlignmentPose CalculateWeaponPose(Vector2 weaponAnchorPosition, Vector2 aimDirection, WeaponDefinitionSO weapon, WeaponRig rig)
    {
        Vector2 safeAim = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
        float aimAngle = Mathf.Atan2(safeAim.y, safeAim.x) * Mathf.Rad2Deg;
        Vector3 anchor = new Vector3(weaponAnchorPosition.x, weaponAnchorPosition.y, 0f);

        Vector3 localRotationOffset = weapon != null ? weapon.LocalRotationOffset : Vector3.zero;
        Quaternion weaponRotation = Quaternion.Euler(0f, 0f, aimAngle + localRotationOffset.z);
        Vector3 localVisualScale = CalculateVisualScale(safeAim, weapon);
        Vector3 visualScale = CalculateRuntimeVisualLossyScale(localVisualScale, rig);
        WeaponRigPoints rigPoints = WeaponRigPoints.From(weapon, rig);

        // Weapon-local alignment points are transformed the way the rendered sprite is: local point, visual scale, then rotation.
        Vector3 scaledGripOffset = ScaleWeaponLocalPoint(rigPoints.GripPoint, visualScale);
        Vector3 weaponPosition = anchor - weaponRotation * scaledGripOffset;
        Vector3 gripPoint = TransformWeaponLocalPoint(weaponPosition, weaponRotation, rigPoints.GripPoint, visualScale);
        Vector3 muzzleTipPoint = TransformWeaponLocalPoint(weaponPosition, weaponRotation, rigPoints.TipPoint, visualScale);
        Vector3 projectileSpawnPoint = TransformWeaponLocalPoint(weaponPosition, weaponRotation, rigPoints.ProjectileSpawnPoint, visualScale);
        Vector3 slashOrigin = TransformWeaponLocalPoint(weaponPosition, weaponRotation, rigPoints.SlashOrigin, visualScale);
        Vector3 slashArcStart = TransformWeaponLocalPoint(weaponPosition, weaponRotation, rigPoints.SlashArcStart, visualScale);
        Vector3 slashArcEnd = TransformWeaponLocalPoint(weaponPosition, weaponRotation, rigPoints.SlashArcEnd, visualScale);

        return new WeaponAlignmentPose
        {
            WeaponAnchorPosition = anchor,
            GripPoint = gripPoint,
            WeaponPosition = weaponPosition,
            MuzzleTipPoint = muzzleTipPoint,
            ProjectileSpawnPoint = projectileSpawnPoint,
            SlashOrigin = slashOrigin,
            SlashArcStart = slashArcStart,
            SlashArcEnd = slashArcEnd,
            AimDirection = safeAim,
            AimAngle = aimAngle,
            WeaponRotation = weaponRotation,
            VisualScale = visualScale
        };
    }

    private static Vector3 TransformWeaponLocalPoint(Vector3 weaponPosition, Quaternion weaponRotation, Vector3 localPoint, Vector3 visualScale)
    {
        return weaponPosition + weaponRotation * ScaleWeaponLocalPoint(localPoint, visualScale);
    }

    private static Vector3 ScaleWeaponLocalPoint(Vector3 localPoint, Vector3 visualScale)
    {
        return Vector3.Scale(localPoint, visualScale);
    }

    public static WeaponAlignmentPose CalculatePose(WeaponDefinitionSO definition, Vector3 playerCenter, Vector2 aimDirection)
    {
        return CalculateWeaponPose(playerCenter, aimDirection, definition);
    }

    public static WeaponAlignmentPose CalculatePose(WeaponDefinitionSO definition, Vector3 playerCenter, Vector2 aimDirection, WeaponRig rig)
    {
        return CalculateWeaponPose(playerCenter, aimDirection, definition, rig);
    }

    public static Vector3 CalculateVisualScale(Vector2 aimDirection, WeaponDefinitionSO weapon)
    {
        float scale = weapon != null && weapon.VisualScale > 0f ? weapon.VisualScale : 1f;
        if (!float.IsFinite(scale))
        {
            scale = 1f;
        }

        scale = Mathf.Clamp(scale, MinVisualScale, MaxLocalVisualScale);
        Vector3 visualScale = Vector3.one * scale;

        if (weapon == null)
        {
            return visualScale;
        }

        bool aimLeft = aimDirection.x < -0.001f;
        if (!aimLeft)
        {
            return visualScale;
        }

        if (weapon.FlipBehavior == WeaponFlipBehavior.FlipXOnAimLeft
            || weapon.FlipBehavior == WeaponFlipBehavior.FlipBothOnAimLeft)
        {
            visualScale.x *= -1f;
        }

        if (weapon.FlipBehavior == WeaponFlipBehavior.FlipYOnAimLeft
            || weapon.FlipBehavior == WeaponFlipBehavior.FlipBothOnAimLeft)
        {
            visualScale.y *= -1f;
        }

        return visualScale;
    }

    private static Vector3 CalculateRuntimeVisualLossyScale(Vector3 localVisualScale, WeaponRig rig)
    {
        if (rig == null || rig.transform == null)
        {
            return localVisualScale;
        }

        Transform visualRoot = rig.transform.parent;
        Transform visualParent = visualRoot != null ? visualRoot.parent : null;
        if (visualParent == null)
        {
            return localVisualScale;
        }

        return Vector3.Scale(localVisualScale, visualParent.lossyScale);
    }

    private readonly struct WeaponRigPoints
    {
        public readonly Vector3 GripPoint;
        public readonly Vector3 TipPoint;
        public readonly Vector3 ProjectileSpawnPoint;
        public readonly Vector3 SlashOrigin;
        public readonly Vector3 SlashArcStart;
        public readonly Vector3 SlashArcEnd;

        private WeaponRigPoints(
            Vector3 gripPoint,
            Vector3 tipPoint,
            Vector3 projectileSpawnPoint,
            Vector3 slashOrigin,
            Vector3 slashArcStart,
            Vector3 slashArcEnd)
        {
            GripPoint = gripPoint;
            TipPoint = tipPoint;
            ProjectileSpawnPoint = projectileSpawnPoint;
            SlashOrigin = slashOrigin;
            SlashArcStart = slashArcStart;
            SlashArcEnd = slashArcEnd;
        }

        public static WeaponRigPoints From(WeaponDefinitionSO definition, WeaponRig rig)
        {
            if (rig != null && definition != null && rig.HasRequiredPointsFor(definition))
            {
                Vector3 tipPoint = rig.TipPointLocal;
                Vector3 projectileSpawnPoint = rig.ProjectileSpawnPoint != null
                    ? rig.ProjectileSpawnPointLocal
                    : tipPoint;
                Vector3 rigSlashOrigin = rig.SlashOrigin != null
                    ? rig.SlashOriginLocal
                    : (definition != null ? definition.SlashVfxOffset : Vector3.zero);
                Vector3 rigSlashArcStart = rig.SlashArcStart != null
                    ? rig.SlashArcStartLocal
                    : rigSlashOrigin + new Vector3(0.2f, -0.25f, 0f);
                Vector3 rigSlashArcEnd = rig.SlashArcEnd != null
                    ? rig.SlashArcEndLocal
                    : rigSlashOrigin + new Vector3(0.2f, 0.25f, 0f);

                return new WeaponRigPoints(
                    rig.GripPointLocal,
                    tipPoint,
                    projectileSpawnPoint,
                    rigSlashOrigin,
                    rigSlashArcStart,
                    rigSlashArcEnd);
            }

            if (definition != null
                && definition.AlignmentPreset != null
                && definition.AlignmentPreset.TryBuildPoints(definition.ItemImage, out WeaponAlignmentPresetPoints presetPoints))
            {
                return new WeaponRigPoints(
                    presetPoints.GripPoint,
                    presetPoints.TipPoint,
                    presetPoints.ProjectileSpawnPoint,
                    presetPoints.SlashOrigin,
                    presetPoints.SlashArcStart,
                    presetPoints.SlashArcEnd);
            }

            Vector3 grip = definition != null ? definition.GripPointOffset : Vector3.zero;
            Vector3 tip = definition != null ? definition.MuzzleTipPointOffset : new Vector3(0.45f, 0f, 0f);
            Vector3 projectile = definition != null ? definition.ProjectileSpawnPointOffset : tip;
            if (projectile == Vector3.zero)
            {
                projectile = tip;
            }

            Vector3 slashOrigin = definition != null ? definition.SlashVfxOffset : Vector3.zero;
            return new WeaponRigPoints(
                grip,
                tip,
                projectile,
                slashOrigin,
                slashOrigin + new Vector3(0.2f, -0.25f, 0f),
                slashOrigin + new Vector3(0.2f, 0.25f, 0f));
        }
    }
}

public enum WeaponRenderBoundsMode
{
    BodyRendererOnly,
    AllChildRenderersExcludingRuntimeObjects
}

public struct WeaponRenderBoundsReport
{
    public bool IsValid;
    public WeaponRenderBoundsMode Mode;
    public Bounds WeaponBounds;
    public Bounds PlayerBounds;
    public Vector2 WeaponRenderedSize;
    public Vector2 PlayerRenderedSize;
    public Vector2 Ratio;
    public SpriteRenderer WeaponRenderer;
    public SpriteRenderer PlayerRenderer;
    public string WeaponRendererPath;
    public string PlayerRootPath;
    public string PlayerBoundsSourcePath;
    public string PlayerBoundsSourceDescription;
    public int PlayerRendererCount;
    public float WeaponRotationZ;
    public Vector3 WeaponLossyScale;
}

public static class WeaponRenderBoundsUtility
{
    public static string FormatSharedBoundsDebug(string source, WeaponRenderBoundsReport report)
    {
        return "[SharedBoundsDebug] " +
            $"Source={source} " +
            $"WeaponRendererPath={report.WeaponRendererPath} " +
            $"PlayerRootPath={report.PlayerRootPath} " +
            $"PlayerBoundsSourcePath={report.PlayerBoundsSourcePath} " +
            $"BoundsMode={report.Mode} " +
            $"PlayerRendererCount={report.PlayerRendererCount} " +
            $"WeaponRotationZ={report.WeaponRotationZ:0.###} " +
            $"WeaponLossyScale={FormatVector(report.WeaponLossyScale)} " +
            $"WeaponBoundsCenter={FormatVector(report.WeaponBounds.center)} " +
            $"WeaponBoundsSize={FormatVector(report.WeaponBounds.size)} " +
            $"PlayerBoundsCenter={FormatVector(report.PlayerBounds.center)} " +
            $"PlayerBoundsSize={FormatVector(report.PlayerBounds.size)} " +
            $"WeaponRenderedSize={FormatVector2(report.WeaponRenderedSize)} " +
            $"PlayerRenderedSize={FormatVector2(report.PlayerRenderedSize)} " +
            $"Ratio={FormatVector2(report.Ratio)}";
    }

    public static WeaponRenderBoundsReport CalculateRenderedBoundsRatio(
        SpriteRenderer weaponRenderer,
        Transform playerRoot,
        WeaponRenderBoundsMode mode = WeaponRenderBoundsMode.BodyRendererOnly)
    {
        WeaponRenderBoundsReport report = new WeaponRenderBoundsReport
        {
            Mode = mode,
            WeaponRenderer = weaponRenderer,
            WeaponRendererPath = GetTransformPath(weaponRenderer != null ? weaponRenderer.transform : null),
            PlayerRootPath = GetTransformPath(playerRoot),
            PlayerBoundsSourceDescription = mode.ToString()
        };

        if (weaponRenderer == null || playerRoot == null)
        {
            return report;
        }

        report.WeaponBounds = weaponRenderer.bounds;
        report.WeaponRenderedSize = ToSize(report.WeaponBounds);
        report.WeaponRotationZ = weaponRenderer.transform.rotation.eulerAngles.z;
        report.WeaponLossyScale = weaponRenderer.transform.lossyScale;

        if (!TryGetPlayerVisualBounds(playerRoot, mode, out Bounds playerBounds, out SpriteRenderer playerRenderer, out string sourcePath, out int rendererCount))
        {
            return report;
        }

        report.IsValid = true;
        report.PlayerBounds = playerBounds;
        report.PlayerRenderedSize = ToSize(playerBounds);
        report.PlayerRenderer = playerRenderer;
        report.PlayerBoundsSourcePath = sourcePath;
        report.PlayerRendererCount = rendererCount;
        report.Ratio = new Vector2(
            report.PlayerRenderedSize.x > 0.0001f ? report.WeaponRenderedSize.x / report.PlayerRenderedSize.x : 0f,
            report.PlayerRenderedSize.y > 0.0001f ? report.WeaponRenderedSize.y / report.PlayerRenderedSize.y : 0f);

        return report;
    }

    public static bool TryGetPlayerVisualBounds(
        Transform playerRoot,
        WeaponRenderBoundsMode mode,
        out Bounds bounds,
        out SpriteRenderer sourceRenderer,
        out string sourcePath,
        out int rendererCount)
    {
        bounds = default;
        sourceRenderer = null;
        sourcePath = "None";
        rendererCount = 0;

        if (playerRoot == null)
        {
            return false;
        }

        if (mode == WeaponRenderBoundsMode.BodyRendererOnly)
        {
            sourceRenderer = GetBodyRenderer(playerRoot);
            if (sourceRenderer == null)
            {
                return false;
            }

            bounds = sourceRenderer.bounds;
            sourcePath = GetTransformPath(sourceRenderer.transform);
            rendererCount = 1;
            return true;
        }

        bool hasBounds = false;
        foreach (SpriteRenderer renderer in playerRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null || !renderer.enabled || renderer.sprite == null || ShouldExcludeFromPlayerBounds(renderer.transform, playerRoot))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                sourceRenderer = renderer;
                sourcePath = GetTransformPath(renderer.transform);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
                sourcePath += ", " + GetTransformPath(renderer.transform);
            }

            rendererCount++;
        }

        return hasBounds;
    }

    public static SpriteRenderer GetBodyRenderer(Transform playerRoot)
    {
        if (playerRoot == null)
        {
            return null;
        }

        SpriteRenderer ownRenderer = playerRoot.GetComponent<SpriteRenderer>();
        if (IsUsablePlayerRenderer(ownRenderer, playerRoot))
        {
            return ownRenderer;
        }

        foreach (SpriteRenderer renderer in playerRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (IsUsablePlayerRenderer(renderer, playerRoot))
            {
                return renderer;
            }
        }

        return null;
    }

    public static bool ShouldExcludeFromPlayerBounds(Transform target, Transform playerRoot)
    {
        Transform current = target;
        while (current != null && current != playerRoot)
        {
            string name = current.name;
            if (name == "WeaponRoot"
                || name == "WeaponAnchor"
                || name.StartsWith("CurrentWeaponVisual_")
                || name.Contains("Projectile")
                || name.Contains("Debug")
                || name.Contains("Gizmo")
                || name.Contains("Canvas")
                || name.Contains("UI"))
            {
                return true;
            }

            current = current.parent;
        }

        return target.gameObject.layer == LayerMask.NameToLayer("UI");
    }

    public static string GetTransformPath(Transform transform)
    {
        if (transform == null)
        {
            return "None";
        }

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static bool IsUsablePlayerRenderer(SpriteRenderer renderer, Transform playerRoot)
    {
        return renderer != null
            && renderer.enabled
            && renderer.sprite != null
            && !ShouldExcludeFromPlayerBounds(renderer.transform, playerRoot);
    }

    private static Vector2 ToSize(Bounds bounds)
    {
        return new Vector2(bounds.size.x, bounds.size.y);
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";
    }

    private static string FormatVector2(Vector2 value)
    {
        return $"({value.x:0.###}, {value.y:0.###})";
    }
}
