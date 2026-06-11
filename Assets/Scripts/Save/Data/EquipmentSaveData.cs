using System;

[Serializable]
public class EquipmentSaveData
{
    public string mainWeaponId;
    public string subWeaponId;
    public int activeSlot;

    public string helmetArmorId;
    public string chestplateArmorId;
    public string leggingsArmorId;
    public string bootsArmorId;

    public EquipmentSaveData(EquipmentManager equipment, ArmorController armorController)
    {
        mainWeaponId = equipment != null ? equipment.GetMainWeaponId() : string.Empty;
        subWeaponId = equipment != null ? equipment.GetSubWeaponId() : string.Empty;
        activeSlot = equipment != null ? (int)equipment.GetActiveSlot() : 0;

        helmetArmorId = armorController.Helmet != null? armorController.Helmet.EquipmentId : string.Empty;
        chestplateArmorId = armorController.Chestplate != null? armorController.Chestplate.EquipmentId : string.Empty;
        leggingsArmorId = armorController.Leggings != null? armorController.Leggings.EquipmentId : string.Empty;
        bootsArmorId = armorController.Boots != null? armorController.Boots.EquipmentId : string.Empty;
    }
}
