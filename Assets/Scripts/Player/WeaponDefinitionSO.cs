using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Definition")]
public class WeaponDefinitionSO : ItemSO, IDestroyableItem, IItemAction
{
    [Header("Identity")]
    [SerializeField] private string weaponId;
    //[SerializeField] private string displayName;

    //[Header("Presentation")]
    //[SerializeField] 
    //private Sprite icon;

    [Header("Offset")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 localRotationOffset = Vector3.zero;

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

    [Header("Optional")]
    [SerializeField] private string rarity;
    [SerializeField] private string[] tags;

    public string WeaponId => weaponId;
    //public string Name => Name;
    //public Sprite ItemImage => ItemImage;
    public Vector3 LocalPositionOffset => localPositionOffset;
    public Vector3 LocalRotationOffset => localRotationOffset;
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
    public string Rarity => rarity;
    public string[] Tags => tags;

    public string ActionName => "Equip";

    public bool PerformAction(GameObject character)
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
