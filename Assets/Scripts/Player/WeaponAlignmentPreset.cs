using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAlignmentPreset", menuName = "Weapons/Weapon Alignment Preset")]
public sealed class WeaponAlignmentPreset : ScriptableObject
{
    [SerializeField] private WeaponArchetype archetype = WeaponArchetype.Generic;
    [SerializeField] private Vector2 normalizedGripPoint = new Vector2(0.2f, 0.5f);
    [SerializeField] private Vector2 normalizedTipPoint = new Vector2(0.85f, 0.5f);
    [SerializeField] private Vector2 normalizedProjectileSpawnPoint = new Vector2(0.9f, 0.5f);
    [SerializeField] private Vector2 normalizedSlashOrigin = new Vector2(0.45f, 0.5f);
    [SerializeField] private Vector2 normalizedSlashArcStart = new Vector2(0.7f, 0.2f);
    [SerializeField] private Vector2 normalizedSlashArcEnd = new Vector2(0.7f, 0.8f);

    public WeaponArchetype Archetype => archetype;

    public bool TryBuildPoints(Sprite sprite, out WeaponAlignmentPresetPoints points)
    {
        points = default;
        if (sprite == null)
        {
            return false;
        }

        Bounds bounds = sprite.bounds;
        Vector3 min = bounds.min;
        Vector3 size = bounds.size;
        if (Mathf.Abs(size.x) < 0.0001f || Mathf.Abs(size.y) < 0.0001f)
        {
            return false;
        }

        points = new WeaponAlignmentPresetPoints(
            ToLocalPoint(min, size, normalizedGripPoint),
            ToLocalPoint(min, size, normalizedTipPoint),
            ToLocalPoint(min, size, normalizedProjectileSpawnPoint),
            ToLocalPoint(min, size, normalizedSlashOrigin),
            ToLocalPoint(min, size, normalizedSlashArcStart),
            ToLocalPoint(min, size, normalizedSlashArcEnd));
        return true;
    }

    private static Vector3 ToLocalPoint(Vector3 min, Vector3 size, Vector2 normalized)
    {
        return new Vector3(
            min.x + size.x * normalized.x,
            min.y + size.y * normalized.y,
            0f);
    }
}

public readonly struct WeaponAlignmentPresetPoints
{
    public readonly Vector3 GripPoint;
    public readonly Vector3 TipPoint;
    public readonly Vector3 ProjectileSpawnPoint;
    public readonly Vector3 SlashOrigin;
    public readonly Vector3 SlashArcStart;
    public readonly Vector3 SlashArcEnd;

    public WeaponAlignmentPresetPoints(
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
}
