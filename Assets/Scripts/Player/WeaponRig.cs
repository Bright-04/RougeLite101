using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponRig : MonoBehaviour
{
    public const string GripPointName = "GripPoint";
    public const string TipPointName = "TipPoint";
    public const string ProjectileSpawnPointName = "ProjectileSpawnPoint";
    public const string SlashOriginName = "SlashOrigin";
    public const string SlashArcStartName = "SlashArcStart";
    public const string SlashArcEndName = "SlashArcEnd";

    [Header("Required Rig Points")]
    [SerializeField] private Transform gripPoint;
    [SerializeField] private Transform tipPoint;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private Transform slashOrigin;
    [SerializeField] private Transform slashArcStart;
    [SerializeField] private Transform slashArcEnd;

    public Transform GripPoint => gripPoint;
    public Transform TipPoint => tipPoint;
    public Transform ProjectileSpawnPoint => projectileSpawnPoint;
    public Transform SlashOrigin => slashOrigin;
    public Transform SlashArcStart => slashArcStart;
    public Transform SlashArcEnd => slashArcEnd;

    public bool HasAllNamedPoints =>
        gripPoint != null
        && tipPoint != null
        && projectileSpawnPoint != null
        && slashOrigin != null
        && slashArcStart != null
        && slashArcEnd != null;

    public bool HasAllRequiredPoints =>
        HasAllNamedPoints;

    public Vector3 GripPointLocal => GetLocalPoint(gripPoint, Vector3.zero);
    public Vector3 TipPointLocal => GetLocalPoint(tipPoint, new Vector3(0.45f, 0f, 0f));
    public Vector3 ProjectileSpawnPointLocal => GetLocalPoint(projectileSpawnPoint, TipPointLocal);
    public Vector3 SlashOriginLocal => GetLocalPoint(slashOrigin, Vector3.zero);
    public Vector3 SlashArcStartLocal => GetLocalPoint(slashArcStart, new Vector3(0.2f, -0.25f, 0f));
    public Vector3 SlashArcEndLocal => GetLocalPoint(slashArcEnd, new Vector3(0.2f, 0.25f, 0f));

    private void Reset()
    {
        AutoBindRequiredPoints(false);
    }

    private void OnValidate()
    {
        AutoBindRequiredPoints(false);
        ValidateRequiredPoints(false);
    }

    public bool ValidateRequiredPoints(bool logWarnings = true)
    {
        return ValidateRequiredPoints(null, logWarnings);
    }

    public bool ValidateRequiredPoints(WeaponDefinitionSO definition, bool logWarnings = true)
    {
        if (HasRequiredPointsFor(definition))
        {
            return true;
        }

        if (logWarnings)
        {
            string archetypeLabel = definition != null ? definition.ResolvedArchetype.ToString() : "Generic";
            Debug.LogWarning($"{nameof(WeaponRig)} '{name}' is missing required point(s) for {archetypeLabel}: {GetMissingPointsLabel(definition)}.", this);
        }

        return false;
    }

    public bool HasRequiredPointsFor(WeaponDefinitionSO definition)
    {
        WeaponType weaponType = definition != null ? definition.WeaponType : WeaponType.Melee;
        WeaponArchetype archetype = definition != null ? definition.ResolvedArchetype : WeaponArchetype.Generic;
        return HasRequiredPointsFor(archetype, weaponType);
    }

    public bool HasRequiredPointsFor(WeaponArchetype archetype, WeaponType weaponType)
    {
        if (gripPoint == null || tipPoint == null)
        {
            return false;
        }

        if (weaponType == WeaponType.Projectile || archetype == WeaponArchetype.Bow || archetype == WeaponArchetype.Gun || archetype == WeaponArchetype.Wand || archetype == WeaponArchetype.Staff)
        {
            return projectileSpawnPoint != null;
        }

        return slashOrigin != null && slashArcStart != null && slashArcEnd != null;
    }

    public void AutoBindRequiredPoints(bool includeInactive)
    {
        BindIfMissing(ref gripPoint, GripPointName, includeInactive);
        BindIfMissing(ref tipPoint, TipPointName, includeInactive);
        BindIfMissing(ref projectileSpawnPoint, ProjectileSpawnPointName, includeInactive);
        BindIfMissing(ref slashOrigin, SlashOriginName, includeInactive);
        BindIfMissing(ref slashArcStart, SlashArcStartName, includeInactive);
        BindIfMissing(ref slashArcEnd, SlashArcEndName, includeInactive);
    }

    private Vector3 GetLocalPoint(Transform point, Vector3 fallback)
    {
        if (point == null)
        {
            return fallback;
        }

        if (transform.parent != null)
        {
            return transform.parent.InverseTransformPoint(point.position);
        }

        return Vector3.Scale(transform.InverseTransformPoint(point.position), transform.localScale);
    }

    private void BindIfMissing(ref Transform field, string childName, bool includeInactive)
    {
        if (field != null)
        {
            return;
        }

        field = FindChildRecursive(transform, childName, includeInactive);
    }

    private static Transform FindChildRecursive(Transform parent, string childName, bool includeInactive)
    {
        if (parent == null)
        {
            return null;
        }

        if ((includeInactive || parent.gameObject.activeInHierarchy) && parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName, includeInactive);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private string GetMissingPointsLabel(WeaponDefinitionSO definition)
    {
        StringBuilder builder = new StringBuilder();
        AppendMissing(builder, gripPoint, GripPointName);
        AppendMissing(builder, tipPoint, TipPointName);

        WeaponType weaponType = definition != null ? definition.WeaponType : WeaponType.Melee;
        WeaponArchetype archetype = definition != null ? definition.ResolvedArchetype : WeaponArchetype.Generic;
        bool needsProjectile = weaponType == WeaponType.Projectile || archetype == WeaponArchetype.Bow || archetype == WeaponArchetype.Gun || archetype == WeaponArchetype.Wand || archetype == WeaponArchetype.Staff;
        if (needsProjectile)
        {
            AppendMissing(builder, projectileSpawnPoint, ProjectileSpawnPointName);
        }
        else
        {
            AppendMissing(builder, slashOrigin, SlashOriginName);
            AppendMissing(builder, slashArcStart, SlashArcStartName);
            AppendMissing(builder, slashArcEnd, SlashArcEndName);
        }

        return builder.ToString();
    }

    private static void AppendMissing(StringBuilder builder, Transform point, string label)
    {
        if (point != null)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(label);
    }

    private void OnDrawGizmosSelected()
    {
        AutoBindRequiredPoints(true);

        DrawPoint(gripPoint, Color.yellow, 0.045f);
        DrawPoint(tipPoint, Color.green, 0.045f);
        DrawPoint(projectileSpawnPoint, Color.red, 0.04f);
        DrawPoint(slashOrigin, new Color(1f, 0.45f, 0.1f), 0.045f);
        DrawPoint(slashArcStart, new Color(1f, 0.25f, 0.2f), 0.035f);
        DrawPoint(slashArcEnd, new Color(1f, 0.25f, 0.2f), 0.035f);
        DrawSlashArc();
    }

    private static void DrawPoint(Transform point, Color color, float radius)
    {
        if (point == null)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawWireSphere(point.position, radius);
    }

    private void DrawSlashArc()
    {
        if (slashOrigin == null || slashArcStart == null || slashArcEnd == null)
        {
            return;
        }

        Vector3 origin = slashOrigin.position;
        Vector3 start = slashArcStart.position;
        Vector3 end = slashArcEnd.position;
        Vector3 startOffset = start - origin;
        Vector3 endOffset = end - origin;
        if (startOffset.sqrMagnitude < 0.0001f || endOffset.sqrMagnitude < 0.0001f)
        {
            Gizmos.color = new Color(1f, 0.35f, 0.25f);
            Gizmos.DrawLine(start, end);
            return;
        }

        Gizmos.color = new Color(1f, 0.35f, 0.25f);
        const int segments = 24;
        Vector3 previous = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = origin + Vector3.Slerp(startOffset, endOffset, t);
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}
