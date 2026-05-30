public enum WeaponArchetype
{
    Generic = 0,
    Sword = 1,
    Dagger = 2,
    Axe = 3,
    Greatsword = 4,
    Spear = 5,
    Staff = 6,
    Wand = 7,
    Bow = 8,
    Gun = 9
}

public static class WeaponArchetypeUtility
{
    public static WeaponArchetype Resolve(WeaponDefinitionSO definition)
    {
        if (definition == null)
        {
            return WeaponArchetype.Generic;
        }

        if (definition.Archetype != WeaponArchetype.Generic)
        {
            return definition.Archetype;
        }

        string equipmentClass = definition.EquipmentClass;
        if (string.IsNullOrWhiteSpace(equipmentClass))
        {
            return definition.WeaponType == WeaponType.Projectile ? WeaponArchetype.Gun : WeaponArchetype.Generic;
        }

        switch (equipmentClass.Trim().ToLowerInvariant())
        {
            case "sword":
                return WeaponArchetype.Sword;
            case "dagger":
                return WeaponArchetype.Dagger;
            case "axe":
            case "hammer":
            case "mace":
            case "whip":
                return WeaponArchetype.Axe;
            case "greatsword":
                return WeaponArchetype.Greatsword;
            case "spear":
            case "lance":
            case "harpoon":
            case "trident":
                return WeaponArchetype.Spear;
            case "staff":
                return WeaponArchetype.Staff;
            case "wand":
                return WeaponArchetype.Wand;
            case "bow":
            case "crossbow":
                return WeaponArchetype.Bow;
            case "gun":
            case "pistol":
            case "sniper":
                return WeaponArchetype.Gun;
            default:
                return definition.WeaponType == WeaponType.Projectile ? WeaponArchetype.Gun : WeaponArchetype.Generic;
        }
    }
}
