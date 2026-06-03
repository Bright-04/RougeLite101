using System;
using UnityEngine;

public class ArmorController : MonoBehaviour
{
    public ArmorDefinitionSO Helmet { get; private set; }
    public ArmorDefinitionSO Chestplate { get; private set; }
    public ArmorDefinitionSO Leggings { get; private set; }
    public ArmorDefinitionSO Boots { get; private set; }

    [SerializeField] private InventoryController inventoryController;

    public event Action OnArmourChanged;

    private void Start()
    {

        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }

        if (inventoryController == null)
        {
            Debug.LogError("InventoryController not found on " + gameObject.name);

        }
    }

    public void Equip(ArmorDefinitionSO armour)
    {
        ArmorDefinitionSO removedOldArmour = null;

        switch (armour.ArmorType)
        {
            case ArmorType.Helmet:
                if (Helmet != null)
                {
                    removedOldArmour = Helmet;
                }
                Helmet = armour;
                break;

            case ArmorType.Chestplate:
                if (Chestplate != null)
                {
                    removedOldArmour = Chestplate;
                }
                Chestplate = armour;
                break;

            case ArmorType.Leggings:
                if (Leggings != null)
                {
                    removedOldArmour = Leggings;
                }
                Leggings = armour;
                break;

            case ArmorType.Boots:
                if (Boots != null)
                {
                    removedOldArmour = Boots;
                }
                Boots = armour;
                break;
        }

        if (removedOldArmour != null && inventoryController != null)
        {
            removedOldArmour.ResetModifierData(gameObject);
            inventoryController.CurrentInventoryData.AddItem(removedOldArmour, 1);
        }

        OnArmourChanged?.Invoke();
    }

    public void Unequip(ArmorType type)
    {
        ArmorDefinitionSO removed = null;

        switch (type)
        {
            case ArmorType.Helmet:
                if (Helmet == null) break;
                removed = Helmet;
                Helmet = null;
                break;

            case ArmorType.Chestplate:
                if (Chestplate == null) break;
                removed = Chestplate;
                Chestplate = null;
                break;

            case ArmorType.Leggings:
                if (Leggings == null) break;
                removed = Leggings;
                Leggings = null;
                break;

            case ArmorType.Boots:
                if (Boots == null) break;
                removed = Boots;
                Boots = null;
                break;
        }

        if (removed != null && inventoryController != null)
        {
            removed.ResetModifierData(gameObject);
            inventoryController.CurrentInventoryData.AddItem(removed, 1);
        }

        OnArmourChanged?.Invoke();
    }    
}
