using UnityEngine;

[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private Transform weaponAnchor;
    [SerializeField] private bool showDebugGizmos = true;

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
        visual.transform.SetParent(weaponRoot, false);
        ResetTransform(visual.transform);
        return visual.transform;
    }

    public void SetCurrentWeapon(Weapon weapon, WeaponDefinitionSO definition)
    {
        currentWeapon = weapon;
        currentDefinition = definition;
        currentWeaponVisual = weapon != null ? weapon.transform.parent : null;
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
        currentWeaponVisual.localScale = Vector3.one;
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
