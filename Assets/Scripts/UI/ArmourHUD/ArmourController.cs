using System;
using UnityEngine;

public class ArmourController : MonoBehaviour
{
    public ArmourItemSO Helmet { get; private set; }
    public ArmourItemSO Chestplate { get; private set; }
    public ArmourItemSO Leggings { get; private set; }
    public ArmourItemSO Boots { get; private set; }

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

    public void Equip(ArmourItemSO armour)
    {
        ArmourItemSO removedOldArmour = null;

        switch (armour.ArmourType)
        {
            case ArmourType.Helmet:
                if(Helmet != null)
                {
                    removedOldArmour = Helmet;
                }
                Helmet = armour;
                break;

            case ArmourType.Chestplate:
                if (Chestplate != null)
                {
                    removedOldArmour = Chestplate;
                }
                Chestplate = armour;
                break;

            case ArmourType.Leggings:
                if (Leggings != null)
                {
                    removedOldArmour = Leggings;
                }
                Leggings = armour;
                break;

            case ArmourType.Boots:
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

    public void Unequip(ArmourType type)
    {
        ArmourItemSO removed = null;

        switch (type)
        {
            case ArmourType.Helmet:
                if (Helmet == null) break;
                removed = Helmet;
                Helmet = null;
                break;

            case ArmourType.Chestplate:
                if (Chestplate == null) break;
                removed = Chestplate;
                Chestplate = null;
                break;

            case ArmourType.Leggings:
                if (Leggings == null) break;
                removed = Leggings;
                Leggings = null;
                break;

            case ArmourType.Boots:
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
