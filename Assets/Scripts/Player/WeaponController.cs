using UnityEngine;
using System.Text;

[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
    private const float PresetGripValidationTolerance = 0.02f;

    [SerializeField] private Transform weaponRoot;
    [SerializeField] private Transform weaponAnchor;
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugReadout = true;
    [SerializeField] private bool logScaleDebugOnWeaponChange = true;
    [SerializeField] private bool logSharedBoundsDebugChanges;

    private PlayerMovement playerMovement;
    private Weapon currentWeapon;
    private WeaponDefinitionSO currentDefinition;
    private Transform currentWeaponVisual;
    private WeaponRig currentWeaponRig;
    private WeaponAlignmentPose currentPose;
    private WeaponRigRuntimeResolution currentRigResolution;
    private SpriteRenderer currentDisplayedWeaponRenderer;
    private Vector3 currentActualRenderedGripPoint;
    private float currentActualRenderedGripDistance;
    private Vector2 currentAimDirection = Vector2.right;
    private string lastSharedBoundsDebugLine;
    private string currentRigSourceSummary = "None";
    private string currentProjectileSourceSummary = "None";
    private string lastPresetGripValidationFailure;
    private bool loggedEquipVisibility;
    private bool loggedFirstPoseVisibility;

    public WeaponAlignmentPose CurrentPose => currentPose;
    public WeaponRig CurrentWeaponRig => currentWeaponRig;
    public Transform WeaponRoot => weaponRoot;
    public Transform WeaponAnchor => weaponAnchor;
    public Transform CurrentWeaponVisualTransform => currentWeaponVisual;
    public WeaponRigRuntimeResolution CurrentRigResolution => currentRigResolution;
    public string CurrentProjectileSource => currentProjectileSourceSummary;
    public SpriteRenderer CurrentDisplayedWeaponRenderer => currentDisplayedWeaponRenderer;
    public Vector3 CurrentActualRenderedGripPoint => currentActualRenderedGripPoint;
    public float CurrentActualRenderedGripDistance => currentActualRenderedGripDistance;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        EnsureHierarchy();
    }

    private void LateUpdate()
    {
        ApplyCurrentPose();
    }

    public Transform CreateWeaponVisualRoot(string weaponId)
    {
        EnsureHierarchy();

        GameObject visual = new GameObject("CurrentWeaponVisual_" + weaponId);
        visual.transform.SetParent(weaponAnchor, false);
        ResetTransform(visual.transform);
        return visual.transform;
    }

    public void SetCurrentWeapon(Weapon weapon, WeaponDefinitionSO definition)
    {
        currentWeapon = weapon;
        currentDefinition = definition;
        currentWeaponVisual = weapon != null ? weapon.transform.parent : null;
        currentWeaponRig = EnsureRuntimeRig(weapon, definition);
        currentRigResolution = default;
        currentRigSourceSummary = "None";
        currentProjectileSourceSummary = "None";
        currentDisplayedWeaponRenderer = null;
        currentActualRenderedGripPoint = Vector3.zero;
        currentActualRenderedGripDistance = 0f;
        lastPresetGripValidationFailure = null;
        loggedEquipVisibility = false;
        loggedFirstPoseVisibility = false;
        if (currentWeaponRig != null)
        {
            currentWeaponRig.ApplyDefinitionRig(definition, out currentRigSourceSummary);
            currentWeaponRig.ValidateRequiredPoints(definition);
        }
        else if (definition != null)
        {
            currentRigSourceSummary = definition.AlignmentPreset != null
                ? $"No runtime WeaponRig component; resolving '{definition.AlignmentPreset.name}' from definition data"
                : "No runtime WeaponRig component available";
        }

        currentRigResolution = WeaponAlignmentUtility.ResolveRuntimeRig(definition, currentWeaponRig);
        currentRigSourceSummary = currentRigResolution.RigSourceSummary;
        currentProjectileSourceSummary = currentRigResolution.ProjectileSource;
        if (currentWeapon != null)
        {
            currentWeapon.ConfigureRigMode(currentRigResolution.ResolvedMode, definition);
            currentDisplayedWeaponRenderer = ResolveDisplayedWeaponRenderer();
        }

        WarnOnLegacyFieldsInPresetMode(definition, currentRigResolution);
        WarnIfVisualScaleCalibrationIsStale(definition, currentRigResolution);
        LogRigSource(definition, currentWeaponRig, currentRigResolution);

        logScaleDebugOnWeaponChange = true;
        if (currentWeaponVisual != null)
        {
            ResetTransform(currentWeaponVisual);
            SetOnlyActiveWeaponVisual(currentWeaponVisual);
        }

        LogVisibilitySnapshot("equip");
        ApplyCurrentPose();
    }

    public void ClearCurrentWeapon(Weapon weapon)
    {
        if (currentWeapon != weapon)
        {
            return;
        }

        currentWeapon = null;
        currentDefinition = null;
        currentWeaponVisual = null;
        currentWeaponRig = null;
        currentRigResolution = default;
        currentRigSourceSummary = "None";
        currentProjectileSourceSummary = "None";
        currentDisplayedWeaponRenderer = null;
        currentActualRenderedGripPoint = Vector3.zero;
        currentActualRenderedGripDistance = 0f;
        lastPresetGripValidationFailure = null;
        loggedEquipVisibility = false;
        loggedFirstPoseVisibility = false;
    }

    public Vector3 GetProjectileSpawnPoint(WeaponDefinitionSO definition, Vector2 aimDirection)
    {
        return CalculatePoseForDefinition(definition, aimDirection).ProjectileSpawnPoint;
    }

    public WeaponAlignmentPose CalculatePoseForDefinition(WeaponDefinitionSO definition, Vector2 aimDirection)
    {
        EnsureHierarchy();
        WeaponRig rig = definition == currentDefinition ? currentWeaponRig : null;
        return WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, aimDirection, definition, rig);
    }

    public WeaponAlignmentPose CalculateCurrentPose(Vector2 aimDirection)
    {
        return CalculatePoseForDefinition(currentDefinition, aimDirection);
    }

    public void ApplyCurrentPose()
    {
        if (currentWeapon == null || currentDefinition == null || currentWeaponVisual == null)
        {
            return;
        }

        EnsureHierarchy();
        if (currentWeapon.TryGetPoseAimDirectionOverride(out Vector2 overriddenAimDirection))
        {
            currentAimDirection = overriddenAimDirection.sqrMagnitude > 0.0001f ? overriddenAimDirection.normalized : Vector2.right;
        }
        else
        {
            currentAimDirection = GetAimDirection();
        }

        if (currentRigResolution.ResolvedMode == WeaponRigPointSourceMode.UsePresetRig)
        {
            switch (currentDefinition.AttackType)
            {
                case WeaponAttackType.Slash:
                    ApplySlashVisualPoseOrIdlePose();
                    break;
                case WeaponAttackType.Thrust:
                    ApplyThrustVisualPoseOrIdlePose();
                    break;
                default:
                    ApplyAimAlignedPose();
                    break;
            }
        }
        else
        {
            switch (currentDefinition.HandlingMode)
            {
                case WeaponHandlingMode.AimAligned:
                    ApplyAimAlignedPose();
                    break;
                case WeaponHandlingMode.SlashArc:
                    ApplySlashVisualPoseOrIdlePose();
                    break;
                case WeaponHandlingMode.Thrust:
                    ApplyThrustVisualPoseOrIdlePose();
                    break;
                default:
                    ApplyAimAlignedPose();
                    break;
            }
        }

        if (logScaleDebugOnWeaponChange)
        {
            LogScaleDebug();
            logScaleDebugOnWeaponChange = false;
        }

        LogSharedBoundsDebugIfChanged();
        LogVisibilitySnapshot("first-pose");
    }

    private void ApplyAimAlignedPose()
    {
        currentPose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, currentAimDirection, currentDefinition, currentWeaponRig);
        currentPose = currentWeapon != null ? currentWeapon.AdjustPose(currentPose) : currentPose;
        ApplyPoseToCurrentWeaponVisual(currentPose);
    }

    private void ApplySlashVisualPoseOrIdlePose()
    {
        currentPose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, currentAimDirection, currentDefinition, currentWeaponRig);
        currentPose = currentWeapon != null ? currentWeapon.AdjustPose(currentPose) : currentPose;
        ApplyPoseToCurrentWeaponVisual(currentPose);
    }

    private void ApplyThrustVisualPoseOrIdlePose()
    {
        currentPose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, currentAimDirection, currentDefinition, currentWeaponRig);
        currentPose = currentWeapon != null ? currentWeapon.AdjustPose(currentPose) : currentPose;
        ApplyPoseToCurrentWeaponVisual(currentPose);
    }

    private void ApplyPoseToCurrentWeaponVisual(WeaponAlignmentPose pose)
    {
        WeaponAlignmentUtility.ApplyPoseToVisualTransform(currentWeaponVisual, pose);
        currentRigResolution = WeaponAlignmentUtility.ResolveRuntimeRig(currentDefinition, currentWeaponRig);
        currentRigSourceSummary = pose.RigSourceSummary;
        currentProjectileSourceSummary = pose.ProjectileSource;
        currentDisplayedWeaponRenderer = ResolveDisplayedWeaponRenderer();
        ValidatePresetGripAlignment(pose);
    }

    private Vector2 GetAimDirection()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (playerMovement != null && playerMovement.LastAimDirection.sqrMagnitude > 0.0001f)
        {
            return playerMovement.LastAimDirection.normalized;
        }

        return currentAimDirection.sqrMagnitude > 0.0001f ? currentAimDirection.normalized : Vector2.right;
    }

    private void EnsureHierarchy()
    {
        if (weaponRoot == null)
        {
            Transform existingRoot = transform.Find("WeaponRoot");
            if (existingRoot == null)
            {
                GameObject rootObject = new GameObject("WeaponRoot");
                rootObject.transform.SetParent(transform, false);
                ResetTransform(rootObject.transform);
                existingRoot = rootObject.transform;
            }

            weaponRoot = existingRoot;
        }

        if (weaponAnchor == null)
        {
            Transform existingAnchor = weaponRoot.Find("WeaponAnchor");
            if (existingAnchor == null)
            {
                GameObject anchorObject = new GameObject("WeaponAnchor");
                anchorObject.transform.SetParent(weaponRoot, false);
                ResetTransform(anchorObject.transform);
                existingAnchor = anchorObject.transform;
            }

            weaponAnchor = existingAnchor;
        }
    }

    private static void ResetTransform(Transform target)
    {
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
    }

    private static WeaponRig EnsureRuntimeRig(Weapon weapon, WeaponDefinitionSO definition)
    {
        if (weapon == null)
        {
            return null;
        }

        WeaponRig rig = weapon.GetComponentInChildren<WeaponRig>(true);
        if (rig != null)
        {
            return rig;
        }

        if (definition == null || definition.RigPointSource != WeaponRigPointSourceMode.UsePresetRig)
        {
            return null;
        }

        return weapon.gameObject.AddComponent<WeaponRig>();
    }

    private void LogRigSource(WeaponDefinitionSO definition, WeaponRig rig, WeaponRigRuntimeResolution resolution)
    {
        if (definition == null)
        {
            return;
        }

        Vector3 gripLocal = rig != null ? rig.GripPointLocal : resolution.GripPoint;
        Vector3 tipLocal = rig != null ? rig.TipPointLocal : resolution.TipPoint;
        Vector3 projectileLocal = rig != null ? rig.ProjectileSpawnPointLocal : resolution.ProjectileSpawnPoint;
        Vector3 slashOriginLocal = rig != null ? rig.SlashOriginLocal : resolution.SlashOrigin;
        Vector3 slashArcStartLocal = rig != null ? rig.SlashArcStartLocal : resolution.SlashArcStart;
        Vector3 slashArcEndLocal = rig != null ? rig.SlashArcEndLocal : resolution.SlashArcEnd;

        Debug.Log(
            $"WeaponController rig source [{definition.name}] " +
            $"requestedMode={resolution.RequestedMode} " +
            $"resolvedMode={resolution.ResolvedMode} " +
            $"source={resolution.RigSourceSummary} " +
            $"projectileSource={resolution.ProjectileSource} " +
            $"gripLocal={FormatVector(gripLocal)} " +
            $"tipLocal={FormatVector(tipLocal)} " +
            $"projectileLocal={FormatVector(projectileLocal)} " +
            $"slashOriginLocal={FormatVector(slashOriginLocal)} " +
            $"slashArcStartLocal={FormatVector(slashArcStartLocal)} " +
            $"slashArcEndLocal={FormatVector(slashArcEndLocal)}",
            this);
    }

    private void WarnOnLegacyFieldsInPresetMode(WeaponDefinitionSO definition, WeaponRigRuntimeResolution resolution)
    {
        if (definition == null || resolution.ResolvedMode != WeaponRigPointSourceMode.UsePresetRig)
        {
            return;
        }

        if (definition.UsesLegacyAimPointOffset)
        {
            Debug.LogWarning($"WeaponController: '{definition.WeaponId}' has non-zero aimPointOffset while UsePresetRig is active. Normal runtime ignores this legacy offset.", this);
        }

        if (definition.UsesLegacyLocalPositionOffset)
        {
            Debug.LogWarning($"WeaponController: '{definition.WeaponId}' has non-zero localPositionOffset while UsePresetRig is active. Normal runtime ignores this legacy offset.", this);
        }

        if (definition.UsesLegacyProjectileSpawnOffset)
        {
            Debug.LogWarning($"WeaponController: '{definition.WeaponId}' has non-zero ProjectileSpawnPointOffset while UsePresetRig is active. Normal runtime ignores it and uses preset ProjectileSpawnPoint.", this);
        }

        if (definition.LocalRotationOffset.sqrMagnitude > 0.000001f)
        {
            Debug.LogWarning($"WeaponController: '{definition.WeaponId}' has non-zero localRotationOffset while UsePresetRig is active. Normal runtime rotates only by aim plus rotationOffsetDegrees.", this);
        }
    }

    private void WarnIfVisualScaleCalibrationIsStale(WeaponDefinitionSO definition, WeaponRigRuntimeResolution resolution)
    {
        if (definition == null || resolution.ResolvedMode != WeaponRigPointSourceMode.UsePresetRig)
        {
            return;
        }

        if (definition.VisualScaleSpace != WeaponVisualScaleSpace.LegacyPrefabCalibrated)
        {
            return;
        }

        float recommended = definition.GetNeutralPresetRigRecommendedVisualScale();
        if (Mathf.Abs(recommended - definition.VisualScale) < 0.05f)
        {
            return;
        }

        Debug.LogWarning(
            $"WeaponController: '{definition.WeaponId}' visualScale={definition.VisualScale:0.###} still appears legacy-prefab calibrated. " +
            $"Recommended neutral preset-rig reset is ~{recommended:0.###}.",
            this);
    }

    private void ValidatePresetGripAlignment(WeaponAlignmentPose pose)
    {
        if (currentDefinition == null || pose.RigSourceMode != WeaponRigPointSourceMode.UsePresetRig)
        {
            lastPresetGripValidationFailure = null;
            currentActualRenderedGripPoint = Vector3.zero;
            currentActualRenderedGripDistance = 0f;
            return;
        }

        float generatedDistance = Vector3.Distance(pose.WeaponAnchorPosition, pose.GripPoint);
        currentActualRenderedGripPoint = CalculateActualRenderedGripPointWorld();
        bool hasActualRenderedGrip = currentDisplayedWeaponRenderer != null;
        currentActualRenderedGripDistance = hasActualRenderedGrip
            ? Vector3.Distance(pose.WeaponAnchorPosition, currentActualRenderedGripPoint)
            : float.PositiveInfinity;

        if (generatedDistance <= PresetGripValidationTolerance
            && currentActualRenderedGripDistance <= PresetGripValidationTolerance)
        {
            lastPresetGripValidationFailure = null;
            return;
        }

        WeaponRenderBoundsReport boundsReport = currentDisplayedWeaponRenderer != null
            ? WeaponRenderBoundsUtility.CalculateRenderedBoundsRatio(currentDisplayedWeaponRenderer, transform, WeaponRenderBoundsMode.BodyRendererOnly)
            : default;

        string failureLine =
            $"WeaponController preset grip validation failed " +
            $"weaponId={currentDefinition.WeaponId} " +
            $"rigMode={pose.RigSourceMode} " +
            $"H={FormatVector(pose.WeaponAnchorPosition)} " +
            $"G={FormatVector(pose.GripPoint)} " +
            $"generatedDistance={generatedDistance:0.###} " +
            $"actualGrip={FormatVector(currentActualRenderedGripPoint)} " +
            $"actualDistance={currentActualRenderedGripDistance:0.###} " +
            $"rendererBoundsCenter={FormatVector(boundsReport.WeaponBounds.center)} " +
            $"rendererBoundsSize={FormatVector(boundsReport.WeaponBounds.size)} " +
            $"heightRatio={(boundsReport.IsValid ? boundsReport.Ratio.y.ToString("0.###") : "n/a")} " +
            $"projectileSource={pose.ProjectileSource}";

        if (failureLine == lastPresetGripValidationFailure)
        {
            return;
        }

        lastPresetGripValidationFailure = failureLine;
        Debug.LogWarning(failureLine, this);
    }

    private Vector3 CalculateActualRenderedGripPointWorld()
    {
        SpriteRenderer displayedRenderer = currentDisplayedWeaponRenderer != null
            ? currentDisplayedWeaponRenderer
            : ResolveDisplayedWeaponRenderer();
        if (displayedRenderer == null)
        {
            return Vector3.zero;
        }

        return displayedRenderer.transform.TransformPoint(currentRigResolution.GripPoint);
    }

    private SpriteRenderer ResolveDisplayedWeaponRenderer()
    {
        if (currentWeapon != null)
        {
            SpriteRenderer displayedRenderer = currentWeapon.DisplayedSpriteRenderer;
            if (IsUsableDisplayedRenderer(displayedRenderer))
            {
                return displayedRenderer;
            }
        }

        if (currentWeaponVisual == null)
        {
            return null;
        }

        SpriteRenderer firstActiveRenderer = null;
        foreach (SpriteRenderer renderer in currentWeaponVisual.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!IsUsableDisplayedRenderer(renderer))
            {
                continue;
            }

            if (firstActiveRenderer == null)
            {
                firstActiveRenderer = renderer;
            }

            if (currentDefinition != null
                && currentDefinition.ItemImage != null
                && renderer.sprite == currentDefinition.ItemImage)
            {
                return renderer;
            }
        }

        return firstActiveRenderer;
    }

    private static bool IsUsableDisplayedRenderer(SpriteRenderer renderer)
    {
        return renderer != null
            && renderer.enabled
            && renderer.gameObject.activeInHierarchy;
    }

    public string BuildCurrentWeaponHierarchyReport()
    {
        if (currentWeaponVisual == null)
        {
            return "No active currentWeaponVisual.";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"WeaponHierarchyReport weaponId={currentDefinition?.WeaponId ?? "None"} rigMode={currentRigResolution.ResolvedMode}");
        AppendTransformReport(builder, currentWeaponVisual, 0);
        builder.AppendLine($"AnchorWorld={FormatVector(weaponAnchor != null ? weaponAnchor.position : Vector3.zero)}");
        builder.AppendLine($"GeneratedGripWorld={FormatVector(currentPose.GripPoint)}");
        builder.AppendLine($"ActualRenderedGripWorld={FormatVector(currentActualRenderedGripPoint)}");
        builder.AppendLine($"GeneratedGripDistance={Vector3.Distance(currentPose.WeaponAnchorPosition, currentPose.GripPoint):0.###}");
        builder.AppendLine($"ActualGripDistance={currentActualRenderedGripDistance:0.###}");
        return builder.ToString();
    }

    private void AppendTransformReport(StringBuilder builder, Transform target, int depth)
    {
        string indent = new string(' ', depth * 2);
        builder.Append(indent)
            .Append(target.name)
            .Append(" localPos=").Append(FormatVector(target.localPosition))
            .Append(" localRot=").Append(FormatVector(target.localEulerAngles))
            .Append(" localScale=").Append(FormatVector(target.localScale))
            .Append(" worldPos=").Append(FormatVector(target.position))
            .Append(" worldScale=").Append(FormatVector(target.lossyScale));

        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            builder.Append(" sprite=").Append(spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "None")
                .Append(" localBoundsCenter=").Append(FormatVector(spriteRenderer.localBounds.center))
                .Append(" localBoundsSize=").Append(FormatVector(spriteRenderer.localBounds.size))
                .Append(" worldBoundsCenter=").Append(FormatVector(spriteRenderer.bounds.center))
                .Append(" worldBoundsSize=").Append(FormatVector(spriteRenderer.bounds.size))
                .Append(" enabled=").Append(spriteRenderer.enabled);
        }

        builder.AppendLine();
        for (int i = 0; i < target.childCount; i++)
        {
            AppendTransformReport(builder, target.GetChild(i), depth + 1);
        }
    }

    private void SetOnlyActiveWeaponVisual(Transform activeVisual)
    {
        if (weaponRoot == null || activeVisual == null)
        {
            return;
        }

        string activeNames = string.Empty;
        int activeCount = CountActiveWeaponVisualsRecursive(weaponRoot, ref activeNames);
        SetOnlyActiveWeaponVisualRecursive(weaponRoot, activeVisual);
        if (activeCount > 1)
        {
            Debug.LogWarning($"WeaponController: Found {activeCount} active CurrentWeaponVisual objects under WeaponRoot ({activeNames}). Disabled all except '{activeVisual.name}'.", this);
        }
    }

    private void SetOnlyActiveWeaponVisualRecursive(Transform root, Transform activeVisual)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("CurrentWeaponVisual_"))
            {
                child.gameObject.SetActive(child == activeVisual);
            }

            SetOnlyActiveWeaponVisualRecursive(child, activeVisual);
        }
    }

    private int CountActiveWeaponVisualsRecursive(Transform root, ref string activeNames)
    {
        int activeCount = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("CurrentWeaponVisual_") && child.gameObject.activeInHierarchy)
            {
                activeCount++;
                activeNames = string.IsNullOrEmpty(activeNames) ? child.name : activeNames + ", " + child.name;
            }

            activeCount += CountActiveWeaponVisualsRecursive(child, ref activeNames);
        }

        return activeCount;
    }

    private void LogScaleDebug()
    {
        if (currentDefinition == null || currentWeaponVisual == null)
        {
            return;
        }

        SpriteRenderer weaponRenderer = currentDisplayedWeaponRenderer != null
            ? currentDisplayedWeaponRenderer
            : ResolveDisplayedWeaponRenderer();
        WeaponRenderBoundsReport boundsReport = WeaponRenderBoundsUtility.CalculateRenderedBoundsRatio(weaponRenderer, transform, WeaponRenderBoundsMode.BodyRendererOnly);
        SpriteRenderer playerBodyRenderer = boundsReport.PlayerRenderer;
        float weaponPixelsPerUnit = weaponRenderer != null && weaponRenderer.sprite != null ? weaponRenderer.sprite.pixelsPerUnit : 0f;
        float playerPixelsPerUnit = playerBodyRenderer != null && playerBodyRenderer.sprite != null ? playerBodyRenderer.sprite.pixelsPerUnit : 0f;
        Debug.Log(WeaponRenderBoundsUtility.FormatSharedBoundsDebug("WeaponController", boundsReport), this);
        Debug.Log(
            $"Weapon scale debug [{currentDefinition.name}] " +
            $"scaleMultiplier={currentDefinition.VisualScale:0.###}, " +
            $"boundsMode={boundsReport.Mode}, " +
            $"weaponRendererPath={boundsReport.WeaponRendererPath}, playerBoundsSource={boundsReport.PlayerBoundsSourcePath}, playerRendererCount={boundsReport.PlayerRendererCount}, " +
            $"player.localScale={FormatVector(transform.localScale)}, " +
            $"visual.localScale={FormatVector(currentWeaponVisual.localScale)}, " +
            $"visual.lossyScale={FormatVector(currentWeaponVisual.lossyScale)}, " +
            $"visualParent.lossyScale={FormatVector(currentWeaponVisual.parent != null ? currentWeaponVisual.parent.lossyScale : Vector3.one)}, " +
            $"weaponRoot.lossyScale={FormatVector(weaponRoot != null ? weaponRoot.lossyScale : Vector3.one)}, " +
            $"player.lossyScale={FormatVector(transform.lossyScale)}, " +
            $"playerBody.localScale={FormatVector(playerBodyRenderer != null ? playerBodyRenderer.transform.localScale : Vector3.zero)}, " +
            $"playerBody.lossyScale={FormatVector(playerBodyRenderer != null ? playerBodyRenderer.transform.lossyScale : Vector3.zero)}, " +
            $"weaponSpritePPU={weaponPixelsPerUnit:0.###}, playerSpritePPU={playerPixelsPerUnit:0.###}, " +
            $"weaponBounds={FormatBounds(boundsReport.WeaponBounds)}, playerBounds={FormatBounds(boundsReport.PlayerBounds)}, " +
            $"weaponRenderedSize={FormatVector2(boundsReport.WeaponRenderedSize)}, playerRenderedSize={FormatVector2(boundsReport.PlayerRenderedSize)}, ratio={FormatVector2(boundsReport.Ratio)}, " +
            $"weaponRootInsidePlayerHierarchy={IsChildOf(weaponRoot, transform)}, weaponRootInsidePlayerBody={IsChildOf(weaponRoot, playerBodyRenderer != null ? playerBodyRenderer.transform : null)}",
            this);
    }

    private void LogSharedBoundsDebugIfChanged()
    {
        if (!logSharedBoundsDebugChanges || currentWeapon == null)
        {
            return;
        }

        SpriteRenderer weaponRenderer = currentDisplayedWeaponRenderer != null
            ? currentDisplayedWeaponRenderer
            : ResolveDisplayedWeaponRenderer();
        WeaponRenderBoundsReport boundsReport = WeaponRenderBoundsUtility.CalculateRenderedBoundsRatio(weaponRenderer, transform, WeaponRenderBoundsMode.BodyRendererOnly);
        string line = WeaponRenderBoundsUtility.FormatSharedBoundsDebug("WeaponController", boundsReport);
        if (line == lastSharedBoundsDebugLine)
        {
            return;
        }

        lastSharedBoundsDebugLine = line;
        Debug.Log(line, this);
    }

    private void LogVisibilitySnapshot(string phase)
    {
        if (phase == "equip")
        {
            if (loggedEquipVisibility)
            {
                return;
            }

            loggedEquipVisibility = true;
        }
        else if (phase == "first-pose")
        {
            if (loggedFirstPoseVisibility)
            {
                return;
            }

            loggedFirstPoseVisibility = true;
        }

        if (currentDefinition == null || currentWeaponVisual == null)
        {
            return;
        }

        SpriteRenderer renderer = currentWeapon != null ? currentWeapon.GetComponentInChildren<SpriteRenderer>(true) : null;
        Transform weaponTransform = currentWeapon != null ? currentWeapon.transform : null;
        Debug.Log(
            $"[WeaponVisibility:{phase}] " +
            $"weaponId={currentDefinition.WeaponId} " +
            $"definition={currentDefinition.name} " +
            $"prefab={(currentDefinition.WeaponPrefab != null ? currentDefinition.WeaponPrefab.name : "None")} " +
            $"visualPath={WeaponRenderBoundsUtility.GetTransformPath(currentWeaponVisual)} " +
            $"weaponPath={WeaponRenderBoundsUtility.GetTransformPath(weaponTransform)} " +
            $"visualLocalPosition={FormatVector(currentWeaponVisual.localPosition)} " +
            $"visualWorldPosition={FormatVector(currentWeaponVisual.position)} " +
            $"visualLocalRotation={FormatVector(currentWeaponVisual.localEulerAngles)} " +
            $"visualLocalScale={FormatVector(currentWeaponVisual.localScale)} " +
            $"poseWeaponPosition={FormatVector(currentPose.WeaponPosition)} " +
            $"poseGrip={FormatVector(currentPose.GripPoint)} " +
            $"poseProjectile={FormatVector(currentPose.ProjectileSpawnPoint)} " +
            $"poseVisualScale={FormatVector(currentPose.VisualScale)} " +
            $"finitePose={IsFinite(currentPose)} " +
            $"rendererEnabled={(renderer != null && renderer.enabled)} " +
            $"sprite={(renderer != null && renderer.sprite != null ? renderer.sprite.name : "None")} " +
            $"color={(renderer != null ? renderer.color.ToString() : "None")} " +
            $"sortingLayer={(renderer != null ? renderer.sortingLayerName : "None")} " +
            $"sortingOrder={(renderer != null ? renderer.sortingOrder.ToString() : "None")} " +
            $"rendererWorldPosition={FormatVector(renderer != null ? renderer.transform.position : Vector3.zero)} " +
            $"rendererLossyScale={FormatVector(renderer != null ? renderer.transform.lossyScale : Vector3.zero)} " +
            $"layer={(renderer != null ? LayerMask.LayerToName(renderer.gameObject.layer) : "None")}",
            this);
    }

    private static bool IsFinite(WeaponAlignmentPose pose)
    {
        return IsFinite(pose.WeaponAnchorPosition)
            && IsFinite(pose.GripPoint)
            && IsFinite(pose.WeaponPosition)
            && IsFinite(pose.MuzzleTipPoint)
            && IsFinite(pose.ProjectileSpawnPoint)
            && IsFinite(pose.SlashOrigin)
            && IsFinite(pose.SlashArcStart)
            && IsFinite(pose.SlashArcEnd)
            && IsFinite(pose.VisualScale)
            && float.IsFinite(pose.AimAngle);
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    private void OnGUI()
    {
        if (!showDebugReadout || currentDefinition == null || currentWeaponVisual == null || weaponRoot == null || weaponAnchor == null)
        {
            return;
        }

        GUI.Label(
            new Rect(12f, 12f, 760f, 150f),
            $"Weapon: {currentDefinition.name}\n" +
            $"RigMode: {currentPose.RigSourceMode}  RigSource: {currentPose.RigSourceSummary}  ProjectileSource: {currentPose.ProjectileSource}\n" +
            $"Player scale: {FormatVector(transform.lossyScale)}  WeaponRoot scale: {FormatVector(weaponRoot.lossyScale)}  Visual parent scale: {FormatVector(currentWeaponVisual.parent.lossyScale)}\n" +
            $"Visual localScale: {FormatVector(currentWeaponVisual.localScale)}  Visual lossyScale: {FormatVector(currentWeaponVisual.lossyScale)}  DisplayedRenderer={(currentDisplayedWeaponRenderer != null ? currentDisplayedWeaponRenderer.transform.name : "None")}\n" +
            $"Rendered ratio weapon/player: {FormatVector2(GetRenderedRatio())}\n" +
            $"H: {FormatVector(weaponAnchor.position)}  G: {FormatVector(currentPose.GripPoint)}  H-G: {Vector3.Distance(weaponAnchor.position, currentPose.GripPoint):0.###}  ActualGrip: {FormatVector(currentActualRenderedGripPoint)}  H-ActualGrip: {currentActualRenderedGripDistance:0.###}\n" +
            $"Muzzle: {FormatVector(currentPose.MuzzleTipPoint)}  P: {FormatVector(currentPose.ProjectileSpawnPoint)}  VisualScale: {FormatVector(currentPose.VisualScale)}\n" +
            GetSortingDebugText());
    }

    private string GetSortingDebugText()
    {
        SpriteRenderer weaponRenderer = currentWeapon != null ? currentWeapon.GetComponentInChildren<SpriteRenderer>(true) : null;
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        if (weaponRenderer == null || playerRenderer == null)
        {
            return "Sorting: missing player or weapon SpriteRenderer";
        }

        return $"Sorting: player({playerRenderer.sortingLayerName}, {playerRenderer.sortingOrder}) weapon({weaponRenderer.sortingLayerName}, {weaponRenderer.sortingOrder})";
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";
    }

    private static string FormatVector2(Vector2 value)
    {
        return $"({value.x:0.###}, {value.y:0.###})";
    }

    private SpriteRenderer GetPlayerBodyRenderer()
    {
        return WeaponRenderBoundsUtility.GetBodyRenderer(transform);
    }

    private Vector2 GetRenderedRatio()
    {
        SpriteRenderer weaponRenderer = currentWeapon != null ? currentWeapon.GetComponentInChildren<SpriteRenderer>(true) : null;
        return WeaponRenderBoundsUtility.CalculateRenderedBoundsRatio(weaponRenderer, transform, WeaponRenderBoundsMode.BodyRendererOnly).Ratio;
    }

    private static string FormatBounds(Bounds bounds)
    {
        return $"center {FormatVector(bounds.center)} size {FormatVector(bounds.size)}";
    }

    private static bool IsChildOf(Transform child, Transform potentialParent)
    {
        if (child == null || potentialParent == null)
        {
            return false;
        }

        Transform current = child;
        while (current != null)
        {
            if (current == potentialParent)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || currentDefinition == null)
        {
            return;
        }

        EnsureHierarchy();
        WeaponAlignmentPose pose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, currentAimDirection, currentDefinition, currentWeaponRig);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.05f);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(weaponAnchor.position, 0.055f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(weaponAnchor.position, weaponAnchor.position + (Vector3)(pose.AimDirection * 0.8f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pose.GripPoint, 0.045f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(pose.WeaponPosition, 0.045f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pose.MuzzleTipPoint, 0.045f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pose.ProjectileSpawnPoint, 0.045f);

        Gizmos.color = new Color(1f, 0.45f, 0.1f);
        Gizmos.DrawWireSphere(pose.SlashOrigin, 0.045f);
        DrawSlashArcGizmo(pose);
    }

    private static void DrawSlashArcGizmo(WeaponAlignmentPose pose)
    {
        Vector3 startOffset = pose.SlashArcStart - pose.SlashOrigin;
        Vector3 endOffset = pose.SlashArcEnd - pose.SlashOrigin;
        if (startOffset.sqrMagnitude < 0.0001f || endOffset.sqrMagnitude < 0.0001f)
        {
            Gizmos.DrawLine(pose.SlashArcStart, pose.SlashArcEnd);
            return;
        }

        const int segments = 24;
        Vector3 previous = pose.SlashArcStart;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = pose.SlashOrigin + Vector3.Slerp(startOffset, endOffset, t);
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}
