using UnityEngine;

public abstract class EquipmentDefinitionSO : ItemSO, IDestroyableItem, IItemAction
{
    [Header("Identity")]
    //[SerializeField] private string equipmentId;
    [SerializeField] private string series;

    [SerializeField] private string equipmentClass;

    [Header("Optional")]
    
    [SerializeField] private string[] tags;

    /// <summary>
    /// Stable authored equipment identity used by registries, save/load, and other serialized lookups.
    /// Keep this string-based and do not treat ItemSO.ID as a substitute.
    /// </summary>
    public string EquipmentId => ItemId;
    public string Series => series;

    public string EquipmentClass => equipmentClass;
    
    public string[] Tags => tags;

    public virtual string ActionName => "Equip";

    public virtual bool PerformAction(GameObject character)
    {
        return false;
    }

    public virtual bool ResetModifierData(GameObject character)
    {
        return false;
    }
}
