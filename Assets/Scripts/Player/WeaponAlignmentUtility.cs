using UnityEngine;

public struct WeaponAlignmentPose
{
    public Vector3 WeaponAnchorPosition;
    public Vector3 GripPoint;
    public Vector3 WeaponPosition;
    public Vector3 MuzzleTipPoint;
    public Vector3 ProjectileSpawnPoint;
    public Vector2 AimDirection;
    public float AimAngle;
    public Quaternion WeaponRotation;
    public Vector3 VisualScale;
}

public static class WeaponAlignmentUtility
{
    public static WeaponAlignmentPose CalculateWeaponPose(Vector2 weaponAnchorPosition, Vector2 aimDirection, WeaponDefinitionSO weapon)
    {
        Vector2 safeAim = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
        float aimAngle = Mathf.Atan2(safeAim.y, safeAim.x) * Mathf.Rad2Deg;
        Quaternion aimRotation = Quaternion.Euler(0f, 0f, aimAngle);
        Vector3 anchor = new Vector3(weaponAnchorPosition.x, weaponAnchorPosition.y, 0f);

        Vector3 gripPoint = anchor + aimRotation * weapon.GripPointOffset;
        Vector3 weaponPosition = gripPoint + aimRotation * weapon.LocalPositionOffset;
        Vector3 muzzleTipPoint = weaponPosition + aimRotation * weapon.AimPointOffset;
        Vector3 projectileSpawnPoint = weaponPosition + aimRotation * weapon.ProjectileSpawnPointOffset;
        Quaternion weaponRotation = Quaternion.Euler(0f, 0f, aimAngle + weapon.LocalRotationOffset.z);
        Vector3 visualScale = CalculateVisualScale(safeAim, weapon);

        return new WeaponAlignmentPose
        {
            WeaponAnchorPosition = anchor,
            GripPoint = gripPoint,
            WeaponPosition = weaponPosition,
            MuzzleTipPoint = muzzleTipPoint,
            ProjectileSpawnPoint = projectileSpawnPoint,
            AimDirection = safeAim,
            AimAngle = aimAngle,
            WeaponRotation = weaponRotation,
            VisualScale = visualScale
        };
    }

    public static WeaponAlignmentPose CalculatePose(WeaponDefinitionSO definition, Vector3 playerCenter, Vector2 aimDirection)
    {
        return CalculateWeaponPose(playerCenter, aimDirection, definition);
    }

    public static Vector3 CalculateVisualScale(Vector2 aimDirection, WeaponDefinitionSO weapon)
    {
        float scale = weapon != null && weapon.VisualScale > 0f ? weapon.VisualScale : 1f;
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
}
