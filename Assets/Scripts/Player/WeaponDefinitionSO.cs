using UnityEngine;

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

    [Header("Optional")]
    [SerializeField] private string rarity;
    [SerializeField] private string[] tags;

    public string WeaponId => weaponId;
    //public string Name => Name;
    //public Sprite ItemImage => ItemImage;
    public Vector3 LocalPositionOffset => localPositionOffset;
    public Vector3 LocalRotationOffset => localRotationOffset;
    public GameObject WeaponPrefab => weaponPrefab;
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
