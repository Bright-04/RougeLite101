using UnityEngine;
using UnityEngine.Serialization;

public enum WeaponType
{
    Melee,
    Projectile
}

public enum WeaponAttackType
{
    None,
    Slash,
    Thrust,
    Projectile,
    MagicProjectile
}

public enum WeaponHandlingMode
{
    SlashArc,
    AimAligned,
    Thrust
}

public enum WeaponFlipBehavior
{
    None,
    FlipXOnAimLeft,
    FlipYOnAimLeft,
    FlipBothOnAimLeft
}

public enum WeaponRigPointSourceMode
{
    UsePresetRig,
    UsePrefabRig,
    LegacyFallback
}

public enum WeaponVisualScaleSpace
{
    LegacyPrefabCalibrated,
    NeutralPresetRigCalibrated
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Definition")]
public class WeaponDefinitionSO : EquipmentDefinitionSO
{
    [Header("Normal Workflow")]
    [SerializeField] private WeaponArchetype archetype = WeaponArchetype.Generic;
    [SerializeField] private WeaponAlignmentPreset alignmentPreset;
    [SerializeField] private WeaponAttackType attackType = WeaponAttackType.None;
    [FormerlySerializedAs("localPositionOffset")]
    [SerializeField] private Vector3 gripPointOffset = Vector3.zero;
    [FormerlySerializedAs("visualScale")]
    [SerializeField] private float visualScale = 1f;
    [SerializeField] private float rotationOffsetDegrees = 0f;

    [Header("Normal Combat")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float knockback = 0f;
    [SerializeField] private float anticipationDuration = 0.05f;
    [SerializeField] private float activeDuration = 0.08f;
    [SerializeField] private float recoveryDuration = 0.12f;
    [SerializeField] private float slashArcDegrees = 90f;
    [SerializeField] private float slashRange = 1f;
    [SerializeField] private float thrustDistance = 1f;
    [SerializeField] private float thrustWidth = 0.35f;

    [Header("Melee Visuals")]
    [SerializeField] private float slashVisualExtraAnticipationDegrees = 0f;
    [SerializeField] private float slashVisualExtraFollowThroughDegrees = 0f;
    [SerializeField] private float thrustVisualPullbackFactor = -1f;
    [SerializeField] private float thrustVisualLungeFactor = -1f;
    [SerializeField] private float thrustVisualStretchFactor = -1f;
    [SerializeField] private float slashVfxLifetime = -1f;
    [SerializeField] private float slashVfxStartScaleMultiplier = 0f;
    [SerializeField] private float slashVfxEndScaleMultiplier = 0f;
    [SerializeField] private bool slashVfxFadeOut;
    [SerializeField] private float meleeVisualPulseScaleAmount = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float meleeVisualPulseBlend = 0f;

    [Header("Advanced / Legacy Alignment")]
    [SerializeField] private WeaponRigPointSourceMode rigPointSource = WeaponRigPointSourceMode.UsePresetRig;
    [SerializeField] private WeaponHandlingMode handlingMode = WeaponHandlingMode.SlashArc;
    [SerializeField] private Vector3 aimPointOffset = new Vector3(0.45f, 0f, 0f);
    [SerializeField] private Vector3 localRotationOffset = Vector3.zero;
    [SerializeField] private WeaponVisualScaleSpace visualScaleSpace = WeaponVisualScaleSpace.LegacyPrefabCalibrated;
    [FormerlySerializedAs("visualPositionOffset")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private WeaponFlipBehavior flipBehavior = WeaponFlipBehavior.None;
    [SerializeField] private Vector3 projectileSpawnPointOffset = Vector3.zero;
    [SerializeField] private Vector3 slashVfxOffset = Vector3.zero;

    [Header("Runtime")]
    [SerializeField] private GameObject weaponPrefab;

    [Header("Compatibility Combat")]
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private float range = 1f;

    [Header("Melee")]
    [SerializeField] private float hitboxDistance = 0.15f;
    [SerializeField] private Vector3 hitboxScale = new Vector3(2f, 2f, 1f);

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float spreadAngle = 0f;

    /// <summary>
    /// Stable authored weapon identity.
    /// This resolves to the shared equipment identity field so weapons participate in the canonical string-based ID model.
    /// </summary>
    public string WeaponId => EquipmentId;
    public string WeaponClass => EquipmentClass;
    public WeaponArchetype Archetype => archetype;
    public WeaponArchetype ResolvedArchetype => WeaponArchetypeUtility.Resolve(this);
    public WeaponAlignmentPreset AlignmentPreset => alignmentPreset;
    public WeaponAttackType AttackType => attackType;
    public float RotationOffsetDegrees => rotationOffsetDegrees;
    public WeaponRigPointSourceMode RigPointSource => rigPointSource;
    public WeaponHandlingMode HandlingMode => handlingMode;
    public Vector3 GripPointOffset => gripPointOffset;
    public Vector3 AimPointOffset => aimPointOffset;
    public Vector3 MuzzleTipPointOffset => aimPointOffset;
    public Vector3 LocalPositionOffset => localPositionOffset;
    public Vector3 LocalRotationOffset => localRotationOffset;
    public float VisualScale => visualScale;
    public WeaponVisualScaleSpace VisualScaleSpace => visualScaleSpace;
    public Vector3 VisualPositionOffset => localPositionOffset;
    public float AimPointDistance => aimPointOffset.magnitude;
    public WeaponFlipBehavior FlipBehavior => flipBehavior;
    public Vector3 ProjectileSpawnPointOffset => projectileSpawnPointOffset;
    public bool UsesNormalPresetRuntime => rigPointSource == WeaponRigPointSourceMode.UsePresetRig;
    public bool IsProjectileAttack => attackType == WeaponAttackType.Projectile || attackType == WeaponAttackType.MagicProjectile;
    public bool IsMeleeAttack => attackType == WeaponAttackType.Slash || attackType == WeaponAttackType.Thrust;
    public bool UsesLegacyProjectileSpawnOffset => projectileSpawnPointOffset.sqrMagnitude > 0.000001f;
    public bool UsesLegacyAimPointOffset => aimPointOffset.sqrMagnitude > 0.000001f;
    public bool UsesLegacyLocalPositionOffset => localPositionOffset.sqrMagnitude > 0.000001f;
    public Vector3 SlashVfxOffset => slashVfxOffset;
    public GameObject WeaponPrefab => weaponPrefab;
    public WeaponType WeaponType => weaponType;
    public int Damage => damage > 0 ? damage : baseDamage;
    public int BaseDamage => baseDamage;
    public float Cooldown => cooldown;
    public float Knockback => knockback;
    public float AnticipationDuration => anticipationDuration;
    public float ActiveDuration => activeDuration;
    public float RecoveryDuration => recoveryDuration;
    public float SlashArcDegrees => slashArcDegrees;
    public float SlashRange => slashRange;
    public float ThrustDistance => thrustDistance;
    public float ThrustWidth => thrustWidth;
    public float SlashVisualExtraAnticipationDegrees => slashVisualExtraAnticipationDegrees;
    public float SlashVisualExtraFollowThroughDegrees => slashVisualExtraFollowThroughDegrees;
    public float ThrustVisualPullbackFactor => thrustVisualPullbackFactor;
    public float ThrustVisualLungeFactor => thrustVisualLungeFactor;
    public float ThrustVisualStretchFactor => thrustVisualStretchFactor;
    public float SlashVfxLifetime => slashVfxLifetime;
    public float SlashVfxStartScaleMultiplier => slashVfxStartScaleMultiplier;
    public float SlashVfxEndScaleMultiplier => slashVfxEndScaleMultiplier;
    public bool SlashVfxFadeOut => slashVfxFadeOut;
    public float MeleeVisualPulseScaleAmount => meleeVisualPulseScaleAmount;
    public float MeleeVisualPulseBlend => meleeVisualPulseBlend;
    public float Range => range;
    public float HitboxDistance => hitboxDistance;
    public Vector3 HitboxScale => hitboxScale;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public int ProjectileCount => projectileCount;
    public float SpreadAngle => spreadAngle;

    public override bool PerformAction(GameObject character)
    {
        EquipmentManager equipmentManager = character.GetComponent<EquipmentManager>();
        if (equipmentManager != null)
        {
            foreach (ModifierData data in modifiersData)
            {
                data.statModifier.AffectCharacter(character, data.value);
            }
            return equipmentManager.EquipWeapon(this);          
        }
        return false;
    }

    public override bool ResetModifierData(GameObject character)
    {
        EquipmentManager equipmentManager = character.GetComponent<EquipmentManager>();
        if (equipmentManager != null)
        {
            foreach (ModifierData data in modifiersData)
            {
                data.statModifier.AffectCharacter(character, -data.value);
            }
            return true;
        }
        return false;
    }

    public float GetUsePresetRigRuntimeScaleCompensation()
    {
        if (visualScaleSpace != WeaponVisualScaleSpace.LegacyPrefabCalibrated)
        {
            return 1f;
        }

        return TryGetPrefabPrimaryVisualScale(out float compensation)
            ? compensation
            : 1f;
    }

    public float GetNeutralPresetRigRecommendedVisualScale()
    {
        return visualScale * GetUsePresetRigRuntimeScaleCompensation();
    }

    public bool TryGetPrefabPrimaryVisualTransformSummary(out string rendererPath, out Vector3 localPosition, out Vector3 localEulerAngles, out Vector3 cumulativeLocalScale)
    {
        rendererPath = string.Empty;
        localPosition = Vector3.zero;
        localEulerAngles = Vector3.zero;
        cumulativeLocalScale = Vector3.one;

        if (weaponPrefab == null)
        {
            return false;
        }

        SpriteRenderer renderer = weaponPrefab.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer == null)
        {
            return false;
        }

        Transform rendererTransform = renderer.transform;
        rendererPath = BuildRelativePath(weaponPrefab.transform, rendererTransform);
        localPosition = rendererTransform.localPosition;
        localEulerAngles = rendererTransform.localEulerAngles;
        cumulativeLocalScale = GetCumulativeLocalScale(weaponPrefab.transform, rendererTransform);
        return true;
    }

    private bool TryGetPrefabPrimaryVisualScale(out float compensation)
    {
        compensation = 1f;
        if (!TryGetPrefabPrimaryVisualTransformSummary(out _, out _, out _, out Vector3 cumulativeLocalScale))
        {
            return false;
        }

        compensation = Mathf.Max(0.0001f, Mathf.Abs(cumulativeLocalScale.x));
        return true;
    }

    private static Vector3 GetCumulativeLocalScale(Transform root, Transform target)
    {
        Vector3 scale = Vector3.one;
        Transform current = target;
        while (current != null)
        {
            scale = Vector3.Scale(current.localScale, scale);
            if (current == root)
            {
                break;
            }

            current = current.parent;
        }

        return scale;
    }

    private static string BuildRelativePath(Transform root, Transform target)
    {
        if (root == null || target == null)
        {
            return string.Empty;
        }

        if (root == target)
        {
            return root.name;
        }

        System.Collections.Generic.Stack<string> segments = new System.Collections.Generic.Stack<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        segments.Push(root.name);
        return string.Join("/", segments.ToArray());
    }
}
