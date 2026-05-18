using System;
using UnityEngine;

public class EquipmentController : MonoBehaviour
{
    public enum ArmorSlot
    {
        Shield,
        Helmet,
        Greaves,
        Boots
    }

    [Header("Starting Armor")]
    [SerializeField] private ArmorDefinitionSO startingShield;
    [SerializeField] private ArmorDefinitionSO startingHelmet;
    [SerializeField] private ArmorDefinitionSO startingGreaves;
    [SerializeField] private ArmorDefinitionSO startingBoots;

    private ArmorDefinitionSO equippedShield;
    private ArmorDefinitionSO equippedHelmet;
    private ArmorDefinitionSO equippedGreaves;
    private ArmorDefinitionSO equippedBoots;

    public event Action<ArmorSlot, ArmorDefinitionSO, ArmorDefinitionSO> OnArmorEquipped;

    private void Start()
    {
        EquipStartingArmor(ArmorSlot.Shield, startingShield);
        EquipStartingArmor(ArmorSlot.Helmet, startingHelmet);
        EquipStartingArmor(ArmorSlot.Greaves, startingGreaves);
        EquipStartingArmor(ArmorSlot.Boots, startingBoots);
    }

    public void EquipArmor(ArmorDefinitionSO armor)
    {
        if (armor == null)
        {
            return;
        }

        EquipArmor(GetSlotForArmor(armor), armor);
    }

    public void EquipArmor(ArmorSlot slot, ArmorDefinitionSO armor)
    {
        ArmorDefinitionSO previousArmor = GetArmor(slot);
        if (previousArmor == armor)
        {
            return;
        }

        SetArmor(slot, armor);
        OnArmorEquipped?.Invoke(slot, previousArmor, armor);
    }

    public ArmorDefinitionSO GetArmor(ArmorSlot slot)
    {
        return slot switch
        {
            ArmorSlot.Shield => equippedShield,
            ArmorSlot.Helmet => equippedHelmet,
            ArmorSlot.Greaves => equippedGreaves,
            ArmorSlot.Boots => equippedBoots,
            _ => null
        };
    }

    private void EquipStartingArmor(ArmorSlot slot, ArmorDefinitionSO armor)
    {
        if (armor != null)
        {
            EquipArmor(slot, armor);
        }
    }

    private ArmorSlot GetSlotForArmor(ArmorDefinitionSO armor)
    {
        return armor.ArmorType switch
        {
            ArmorType.Shield => ArmorSlot.Shield,
            ArmorType.Helmet => ArmorSlot.Helmet,
            ArmorType.Greaves => ArmorSlot.Greaves,
            ArmorType.Boots => ArmorSlot.Boots,
            _ => ArmorSlot.Shield
        };
    }

    private void SetArmor(ArmorSlot slot, ArmorDefinitionSO armor)
    {
        switch (slot)
        {
            case ArmorSlot.Shield:
                equippedShield = armor;
                break;
            case ArmorSlot.Helmet:
                equippedHelmet = armor;
                break;
            case ArmorSlot.Greaves:
                equippedGreaves = armor;
                break;
            case ArmorSlot.Boots:
                equippedBoots = armor;
                break;
        }
    }
}
