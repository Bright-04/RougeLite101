using UnityEngine;

public abstract class EquipmentDefinitionSO : ItemSO, IDestroyableItem, IItemAction
{
    [Header("Identity")]
    [SerializeField] private string equipmentId;
    [SerializeField] private string series;
    [SerializeField] private int tier = 1;
    [SerializeField] private string equipmentClass;

    [Header("Optional")]
    [SerializeField] private string rarity;
    [SerializeField] private string[] tags;

    public string EquipmentId => equipmentId;
    public string Series => series;
    public int Tier => tier;
    public string EquipmentClass => equipmentClass;
    public string Rarity => rarity;
    public string[] Tags => tags;

    public virtual string ActionName => "Equip";

    public virtual bool PerformAction(GameObject character)
    {
        return false;
    }
}
