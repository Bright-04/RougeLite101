using UnityEngine;

[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private Transform weaponAnchor;
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugReadout = true;
    [SerializeField] private bool logScaleDebugOnWeaponChange = true;

    private PlayerMovement playerMovement;
    private Weapon currentWeapon;
    private WeaponDefinitionSO currentDefinition;
    private Transform currentWeaponVisual;
    private WeaponAlignmentPose currentPose;
    private Vector2 currentAimDirection = Vector2.right;

    public WeaponAlignmentPose CurrentPose => currentPose;
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
        logScaleDebugOnWeaponChange = true;
        if (currentWeaponVisual != null)
        {
            ResetTransform(currentWeaponVisual);
            SetOnlyActiveWeaponVisual(currentWeaponVisual);
        }

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
    }

    public Vector3 GetProjectileSpawnPoint(WeaponDefinitionSO definition, Vector2 aimDirection)
    {
        if (definition == null)
        {
            return weaponAnchor != null ? weaponAnchor.position : transform.position;
        }

        EnsureHierarchy();
        return WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, aimDirection, definition).ProjectileSpawnPoint;
    }

    public void ApplyCurrentPose()
    {
        if (currentWeapon == null || currentDefinition == null || currentWeaponVisual == null)
        {
            return;
        }

        EnsureHierarchy();
        currentAimDirection = GetAimDirection();
        currentPose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, currentAimDirection, currentDefinition);

        currentWeaponVisual.position = currentPose.WeaponPosition;
        currentWeaponVisual.rotation = currentPose.WeaponRotation;
        currentWeaponVisual.localScale = currentPose.VisualScale;

        if (logScaleDebugOnWeaponChange)
        {
            LogScaleDebug();
            logScaleDebugOnWeaponChange = false;
        }
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

        SetOnlyActiveWeaponVisualRecursive(weaponRoot, activeVisual);
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

    private void LogScaleDebug()
    {
        if (currentDefinition == null || currentWeaponVisual == null)
        {
            return;
        }

        SpriteRenderer weaponRenderer = currentWeapon != null ? currentWeapon.GetComponentInChildren<SpriteRenderer>(true) : null;
        SpriteRenderer playerBodyRenderer = GetPlayerBodyRenderer();
        float weaponPixelsPerUnit = weaponRenderer != null && weaponRenderer.sprite != null ? weaponRenderer.sprite.pixelsPerUnit : 0f;
        float playerPixelsPerUnit = playerBodyRenderer != null && playerBodyRenderer.sprite != null ? playerBodyRenderer.sprite.pixelsPerUnit : 0f;
        Vector2 weaponRenderedSize = GetRenderedSize(weaponRenderer);
        Vector2 playerRenderedSize = GetRenderedSize(playerBodyRenderer);
        Vector2 sizeRatio = new Vector2(
            playerRenderedSize.x > 0.0001f ? weaponRenderedSize.x / playerRenderedSize.x : 0f,
            playerRenderedSize.y > 0.0001f ? weaponRenderedSize.y / playerRenderedSize.y : 0f);
        Debug.Log(
            $"Weapon scale debug [{currentDefinition.name}] " +
            $"scaleMultiplier={currentDefinition.VisualScale:0.###}, " +
            $"player.localScale={FormatVector(transform.localScale)}, " +
            $"visual.localScale={FormatVector(currentWeaponVisual.localScale)}, " +
            $"visual.lossyScale={FormatVector(currentWeaponVisual.lossyScale)}, " +
            $"visualParent.lossyScale={FormatVector(currentWeaponVisual.parent != null ? currentWeaponVisual.parent.lossyScale : Vector3.one)}, " +
            $"weaponRoot.lossyScale={FormatVector(weaponRoot != null ? weaponRoot.lossyScale : Vector3.one)}, " +
            $"player.lossyScale={FormatVector(transform.lossyScale)}, " +
            $"playerBody.localScale={FormatVector(playerBodyRenderer != null ? playerBodyRenderer.transform.localScale : Vector3.zero)}, " +
            $"playerBody.lossyScale={FormatVector(playerBodyRenderer != null ? playerBodyRenderer.transform.lossyScale : Vector3.zero)}, " +
            $"weaponSpritePPU={weaponPixelsPerUnit:0.###}, playerSpritePPU={playerPixelsPerUnit:0.###}, " +
            $"weaponRenderedSize={FormatVector2(weaponRenderedSize)}, playerRenderedSize={FormatVector2(playerRenderedSize)}, ratio={FormatVector2(sizeRatio)}, " +
            $"weaponRootInsidePlayerHierarchy={IsChildOf(weaponRoot, transform)}, weaponRootInsidePlayerBody={IsChildOf(weaponRoot, playerBodyRenderer != null ? playerBodyRenderer.transform : null)}",
            this);
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
        SpriteRenderer ownRenderer = GetComponent<SpriteRenderer>();
        if (ownRenderer != null)
        {
            return ownRenderer;
        }

        return GetComponentInChildren<SpriteRenderer>();
    }

    private Vector2 GetRenderedRatio()
    {
        Vector2 weaponSize = GetRenderedSize(currentWeapon != null ? currentWeapon.GetComponentInChildren<SpriteRenderer>(true) : null);
        Vector2 playerSize = GetRenderedSize(GetPlayerBodyRenderer());
        return new Vector2(
            playerSize.x > 0.0001f ? weaponSize.x / playerSize.x : 0f,
            playerSize.y > 0.0001f ? weaponSize.y / playerSize.y : 0f);
    }

    private static Vector2 GetRenderedSize(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return Vector2.zero;
        }

        Bounds bounds = renderer.bounds;
        return new Vector2(bounds.size.x, bounds.size.y);
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
        WeaponAlignmentPose pose = WeaponAlignmentUtility.CalculateWeaponPose(weaponAnchor.position, currentAimDirection, currentDefinition);

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
    }
}
