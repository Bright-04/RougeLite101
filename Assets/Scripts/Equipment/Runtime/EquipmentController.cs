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
    [SerializeField] private ArmorRegistry armorRegistry;
    [SerializeField] private ArmorDefinitionSO startingShield;
    [SerializeField] private ArmorDefinitionSO startingHelmet;
    [SerializeField] private ArmorDefinitionSO startingGreaves;
    [SerializeField] private ArmorDefinitionSO startingBoots;

    private ArmorDefinitionSO equippedShield;
    private ArmorDefinitionSO equippedHelmet;
    private ArmorDefinitionSO equippedGreaves;
    private ArmorDefinitionSO equippedBoots;

    public event Action<ArmorSlot, ArmorDefinitionSO, ArmorDefinitionSO> OnArmorEquipped;

    private void Awake()
    {
        if (armorRegistry != null)
        {
            armorRegistry.Initialize();
        }

        EquipStartingArmor(ArmorSlot.Shield, startingShield);
        EquipStartingArmor(ArmorSlot.Helmet, startingHelmet);
        EquipStartingArmor(ArmorSlot.Greaves, startingGreaves);
        EquipStartingArmor(ArmorSlot.Boots, startingBoots);
    }

    public void EquipArmor(ArmorDefinitionSO armor)
    {
        if (!ArmorLoadoutRules.CanAcceptArmor(armor))
        {
            return;
        }

        EquipArmor(ArmorLoadoutRules.GetSlotForArmor(armor), armor);
    }

    public void EquipArmor(ArmorSlot slot, ArmorDefinitionSO armor)
    {
        ArmorDefinitionSO previousArmor = GetArmor(slot);
        if (ArmorLoadoutRules.AreSameArmor(previousArmor, armor))
        {
            return;
        }

        // EquipmentController remains the orchestration/state owner for armor slots and runtime notifications.
        SetArmor(slot, armor);
        OnArmorEquipped?.Invoke(slot, previousArmor, armor);
    }

    public ArmorDefinitionSO GetArmor(ArmorSlot slot)
    {
        return ArmorLoadoutRules.GetArmor(slot, equippedShield, equippedHelmet, equippedGreaves, equippedBoots);
    }

    public string GetArmorId(ArmorSlot slot)
    {
        ArmorDefinitionSO armor = GetArmor(slot);
        return ArmorLoadoutRules.GetStableArmorId(armor);
    }

    public void LoadArmor(string shieldId, string helmetId, string greavesId, string bootsId)
    {
        if (armorRegistry == null)
        {
            Debug.LogWarning("EquipmentController: ArmorRegistry is not assigned. Armor load skipped.", this);
            return;
        }

        EquipArmor(ArmorSlot.Shield, armorRegistry.GetById(shieldId));
        EquipArmor(ArmorSlot.Helmet, armorRegistry.GetById(helmetId));
        EquipArmor(ArmorSlot.Greaves, armorRegistry.GetById(greavesId));
        EquipArmor(ArmorSlot.Boots, armorRegistry.GetById(bootsId));
    }

    public void ReplayEquippedArmor()
    {
        ReplayArmor(ArmorSlot.Shield);
        ReplayArmor(ArmorSlot.Helmet);
        ReplayArmor(ArmorSlot.Greaves);
        ReplayArmor(ArmorSlot.Boots);
    }

    private void EquipStartingArmor(ArmorSlot slot, ArmorDefinitionSO armor)
    {
        if (ArmorLoadoutRules.CanAcceptArmor(armor))
        {
            EquipArmor(slot, armor);
        }
    }

    private ArmorSlot GetSlotForArmor(ArmorDefinitionSO armor)
    {
        return ArmorLoadoutRules.GetSlotForArmor(armor);
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

    private void ReplayArmor(ArmorSlot slot)
    {
        ArmorDefinitionSO armor = GetArmor(slot);
        if (armor != null)
        {
            OnArmorEquipped?.Invoke(slot, null, armor);
        }
    }
}
