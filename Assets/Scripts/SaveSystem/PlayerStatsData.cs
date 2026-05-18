using System;

/// <summary>
/// Class chứa data cần save - PHẢI có [Serializable]
/// </summary>
[Serializable]
public class PlayerStatsData
{
    // Level System
    public float level;
    public float currentExp;
    public float levelUpExp;

    // Core Stats
    public float maxHP;
    public float maxMana;
    public float maxStamina;

    // Regeneration
    public float hpRegen;
    public float manaRegen;
    public float staminaRegen;

    // Combat Stats
    public float attackDamage;
    public float abilityPower;
    public float defense;
    public float critChance;
    public float critDamage;
    public float luck;

    // Weapon Loadout
    public string mainWeaponId;
    public string subWeaponId;
    public int activeSlot;


    // <summary>
    /// Constructor từ PlayerStats
    /// </summary>
    public PlayerStatsData(PlayerStats stats, EquipmentManager equipment)
    {
        // Level
        level = stats.level;
        currentExp = stats.currentExp;
        levelUpExp = stats.levelUpExp;

        // Core Stats
        maxHP = stats.maxHP - stats.buffMaxHP;
        maxMana = stats.maxMana - stats.buffMaxMana;
        maxStamina = stats.maxStamina - stats.buffMaxStamina;

        // Regen
        hpRegen = stats.hpRegen - stats.buffHpRegen;
        manaRegen = stats.manaRegen - stats.buffManaRegen;
        staminaRegen = stats.staminaRegen - stats.buffStaminaRegen;

        // Combat
        attackDamage = stats.attackDamage - stats.buffAttackDamage;
        abilityPower = stats.abilityPower - stats.buffAbilityPower;
        defense = stats.defense - stats.buffDefense;
        critChance = stats.critChance - stats.buffCritChance;
        critDamage = stats.critDamage - stats.buffCritDamage;
        luck = stats.luck - stats.buffLuck;

        mainWeaponId = equipment != null ? equipment.GetMainWeaponId() : string.Empty;
        subWeaponId = equipment != null ? equipment.GetSubWeaponId() : string.Empty;
        activeSlot = equipment != null ? (int)equipment.GetActiveSlot() : 0;
    }
}
