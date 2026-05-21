using UnityEngine;

[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
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
    private Vector2 currentAimDirection = Vector2.right;
    private string lastSharedBoundsDebugLine;
    private bool loggedEquipVisibility;
    private bool loggedFirstPoseVisibility;

    public WeaponAlignmentPose CurrentPose => currentPose;
    public WeaponRig CurrentWeaponRig => currentWeaponRig;
    public Transform WeaponRoot => weaponRoot;
    public Transform WeaponAnchor => weaponAnchor;

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
        currentWeaponRig = weapon != null ? weapon.GetComponentInChildren<WeaponRig>(true) : null;
        loggedEquipVisibility = false;
        loggedFirstPoseVisibility = false;
        if (currentWeaponRig != null)
        {
            currentWeaponRig.ValidateRequiredPoints(definition);
            if (definition != null && definition.UsesLegacyProjectileSpawnOffset && currentWeaponRig.ProjectileSpawnPoint == null)
            {
                Debug.LogWarning($"WeaponController: '{definition.name}' is using legacy ProjectileSpawnPointOffset because its active rig has no ProjectileSpawnPoint.", this);
            }
            else if (definition != null && definition.UsesLegacyProjectileSpawnOffset)
            {
                Debug.LogWarning($"WeaponController: '{definition.name}' still serializes ProjectileSpawnPointOffset, but runtime is expected to use WeaponRig.ProjectileSpawnPoint.", this);
            }
        }
        else if (definition != null)
        {
            string presetLabel = definition.AlignmentPreset != null ? $" preset '{definition.AlignmentPreset.name}'" : " legacy WeaponDefinition offsets";
            Debug.LogWarning($"WeaponController: '{definition.name}' has no WeaponRig and is using{presetLabel}.", this);
        }

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
        currentAimDirection = GetAimDirection();

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
        ApplyPoseToCurrentWeaponVisual(currentPose);
    }

    private void ApplySlashVisualPoseOrIdlePose()
    {
        // TODO: SlashArc should use slash-specific idle/swing visual rules instead of aim-aligned behavior.
        currentPose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, currentAimDirection, currentDefinition, currentWeaponRig);
        ApplyPoseToCurrentWeaponVisual(currentPose);
    }

    private void ApplyThrustVisualPoseOrIdlePose()
    {
        // TODO: Thrust should use thrust-specific idle/extension visual rules instead of aim-aligned behavior.
        currentPose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, currentAimDirection, currentDefinition, currentWeaponRig);
        ApplyPoseToCurrentWeaponVisual(currentPose);
    }

    private void ApplyPoseToCurrentWeaponVisual(WeaponAlignmentPose pose)
    {
        WeaponAlignmentUtility.ApplyPoseToVisualTransform(currentWeaponVisual, pose);
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

        SpriteRenderer weaponRenderer = currentWeapon != null ? currentWeapon.GetComponentInChildren<SpriteRenderer>(true) : null;
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

        SpriteRenderer weaponRenderer = currentWeapon.GetComponentInChildren<SpriteRenderer>(true);
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
            $"Player scale: {FormatVector(transform.lossyScale)}  WeaponRoot scale: {FormatVector(weaponRoot.lossyScale)}  Visual parent scale: {FormatVector(currentWeaponVisual.parent.lossyScale)}\n" +
            $"Visual localScale: {FormatVector(currentWeaponVisual.localScale)}  Visual lossyScale: {FormatVector(currentWeaponVisual.lossyScale)}\n" +
            $"Rendered ratio weapon/player: {FormatVector2(GetRenderedRatio())}\n" +
            $"Anchor: {FormatVector(weaponAnchor.position)}  Grip: {FormatVector(currentPose.GripPoint)}  WeaponPos: {FormatVector(currentPose.WeaponPosition)}\n" +
            $"Muzzle: {FormatVector(currentPose.MuzzleTipPoint)}  Projectile: {FormatVector(currentPose.ProjectileSpawnPoint)}  VisualScale: {FormatVector(currentPose.VisualScale)}\n" +
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
