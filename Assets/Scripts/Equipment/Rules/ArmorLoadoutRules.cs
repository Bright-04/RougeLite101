using System;

// Pure decision rules only.
// No Unity object orchestration, asset mutation, save/load side effects, or scene/UI access belongs here.
public static class ArmorLoadoutRules
{
    public static bool CanAcceptArmor(ArmorDefinitionSO armor)
    {
        return armor != null;
    }

    public static bool AreSameArmor(ArmorDefinitionSO left, ArmorDefinitionSO right)
    {
        return ReferenceEquals(left, right);
    }

    public static bool HasSameStableIdentity(ArmorDefinitionSO left, ArmorDefinitionSO right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return string.Equals(left.EquipmentId, right.EquipmentId, StringComparison.Ordinal);
    }

    public static bool IsSameArmorSlot(EquipmentController.ArmorSlot slot, ArmorDefinitionSO armor)
    {
        return armor != null && GetSlotForArmor(armor) == slot;
    }

    public static EquipmentController.ArmorSlot GetSlotForArmor(ArmorDefinitionSO armor)
    {
        if (armor == null)
        {
            return EquipmentController.ArmorSlot.Shield;
        }

        return armor.ArmorType switch
        {
            ArmorType.Shield => EquipmentController.ArmorSlot.Shield,
            ArmorType.Helmet => EquipmentController.ArmorSlot.Helmet,
            ArmorType.Greaves => EquipmentController.ArmorSlot.Greaves,
            ArmorType.Boots => EquipmentController.ArmorSlot.Boots,
            _ => EquipmentController.ArmorSlot.Shield
        };
    }

    public static ArmorDefinitionSO GetArmor(EquipmentController.ArmorSlot slot, ArmorDefinitionSO shield, ArmorDefinitionSO helmet, ArmorDefinitionSO greaves, ArmorDefinitionSO boots)
    {
        return slot switch
        {
            EquipmentController.ArmorSlot.Shield => shield,
            EquipmentController.ArmorSlot.Helmet => helmet,
            EquipmentController.ArmorSlot.Greaves => greaves,
            EquipmentController.ArmorSlot.Boots => boots,
            _ => null
        };
    }

    public static string GetStableArmorId(ArmorDefinitionSO armor)
    {
        return armor != null ? armor.EquipmentId : string.Empty;
    }
}