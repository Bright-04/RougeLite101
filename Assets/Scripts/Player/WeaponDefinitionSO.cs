using UnityEngine;
using UnityEngine.Serialization;

public enum WeaponType
{
    Melee,
    Projectile
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

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Definition")]
public class WeaponDefinitionSO : EquipmentDefinitionSO
{
    [Header("Offset")]
    [SerializeField] private WeaponHandlingMode handlingMode = WeaponHandlingMode.SlashArc;
    [FormerlySerializedAs("localPositionOffset")]
    [SerializeField] private Vector3 gripPointOffset = Vector3.zero;
    [SerializeField] private Vector3 aimPointOffset = new Vector3(0.45f, 0f, 0f);
    [SerializeField] private Vector3 localRotationOffset = Vector3.zero;
    [FormerlySerializedAs("visualScale")]
    [SerializeField] private float visualScale = 1f;
    [FormerlySerializedAs("visualPositionOffset")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private WeaponFlipBehavior flipBehavior = WeaponFlipBehavior.None;
    [SerializeField] private Vector3 projectileSpawnPointOffset = Vector3.zero;
    [SerializeField] private Vector3 slashVfxOffset = Vector3.zero;

    [Header("Runtime")]
    [SerializeField] private GameObject weaponPrefab;

    [Header("Combat")]
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float range = 1f;

    [Header("Melee")]
    [SerializeField] private float hitboxDistance = 0.15f;
    [SerializeField] private Vector3 hitboxScale = new Vector3(2f, 2f, 1f);

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float spreadAngle = 0f;

    public string WeaponId => EquipmentId;
    public string WeaponClass => EquipmentClass;
    public WeaponHandlingMode HandlingMode => handlingMode;
    public Vector3 GripPointOffset => gripPointOffset;
    public Vector3 AimPointOffset => aimPointOffset;
    public Vector3 MuzzleTipPointOffset => aimPointOffset;
    public Vector3 LocalPositionOffset => localPositionOffset;
    public Vector3 LocalRotationOffset => localRotationOffset;
    public float VisualScale => visualScale;
    public Vector3 VisualPositionOffset => localPositionOffset;
    public float AimPointDistance => aimPointOffset.magnitude;
    public WeaponFlipBehavior FlipBehavior => flipBehavior;
    public Vector3 ProjectileSpawnPointOffset => projectileSpawnPointOffset;
    public Vector3 SlashVfxOffset => slashVfxOffset;
    public GameObject WeaponPrefab => weaponPrefab;
    public WeaponType WeaponType => weaponType;
    public int BaseDamage => baseDamage;
    public float Cooldown => cooldown;
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
            EquipmentManager.WeaponSlot activeSlot = equipmentManager.GetActiveSlot();
            equipmentManager.ReplaceWeapon(activeSlot, this);
            return true;
        }
        return false;
    }
}
