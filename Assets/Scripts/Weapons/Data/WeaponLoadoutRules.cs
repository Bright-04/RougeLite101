using System;

// Pure decision rules only.
// No Unity object orchestration, asset mutation, save/load side effects, or scene/UI access belongs here.
public static class WeaponLoadoutRules
{
    public static bool CanAcceptPickup(WeaponDefinitionSO newWeaponDef, bool testingWeaponOverrideActive)
    {
        return newWeaponDef != null && !testingWeaponOverrideActive;
    }

    public static bool CanAutoEquipMain(WeaponDefinitionSO mainWeaponDef)
    {
        return mainWeaponDef == null;
    }

    public static bool CanAutoEquipSub(WeaponDefinitionSO subWeaponDef)
    {
        return subWeaponDef == null;
    }

    public static bool ShouldShowPickupChoice(WeaponPickupModalUI modal, WeaponDefinitionSO mainWeaponDef, WeaponDefinitionSO subWeaponDef)
    {
        return modal != null && mainWeaponDef != null && subWeaponDef != null;
    }

    public static string GetStableWeaponId(WeaponDefinitionSO definition)
    {
        return definition != null ? definition.WeaponId : string.Empty;
    }

    public static bool HasStableWeaponIdentity(WeaponDefinitionSO left, WeaponDefinitionSO right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return ReferenceEquals(left, right)
            || string.Equals(left.WeaponId, right.WeaponId, StringComparison.Ordinal);
    }

    public static bool ShouldRefreshEquippedWeapon(WeaponDefinitionSO currentDefinition, WeaponDefinitionSO changedDefinition, Weapon currentWeapon)
    {
        return currentWeapon != null && HasStableWeaponIdentity(currentDefinition, changedDefinition);
    }

    public static EquipmentManager.WeaponSlot ResolveFallbackActiveSlot(WeaponDefinitionSO mainWeaponDef, WeaponDefinitionSO subWeaponDef)
    {
        return mainWeaponDef != null ? EquipmentManager.WeaponSlot.Main : EquipmentManager.WeaponSlot.Sub;
    }

    public static EquipmentManager.WeaponSlot ResolveLoadedActiveSlot(EquipmentManager.WeaponSlot requestedSlot, WeaponDefinitionSO mainWeaponDef, WeaponDefinitionSO subWeaponDef)
    {
        return IsSlotOccupied(requestedSlot, mainWeaponDef, subWeaponDef)
            ? requestedSlot
            : ResolveFallbackActiveSlot(mainWeaponDef, subWeaponDef);
    }

    public static bool IsSlotOccupied(EquipmentManager.WeaponSlot slot, WeaponDefinitionSO mainWeaponDef, WeaponDefinitionSO subWeaponDef)
    {
        return GetWeaponDefinition(slot, mainWeaponDef, subWeaponDef) != null;
    }

    public static WeaponDefinitionSO GetWeaponDefinition(EquipmentManager.WeaponSlot slot, WeaponDefinitionSO mainWeaponDef, WeaponDefinitionSO subWeaponDef)
    {
        return slot == EquipmentManager.WeaponSlot.Main ? mainWeaponDef : subWeaponDef;
    }
}