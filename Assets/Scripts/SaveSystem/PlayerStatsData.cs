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
        maxHP = stats.maxHP;
        maxMana = stats.maxMana;
        maxStamina = stats.maxStamina;

        // Regen
        hpRegen = stats.hpRegen;
        manaRegen = stats.manaRegen;
        staminaRegen = stats.staminaRegen;

        // Combat
        attackDamage = stats.attackDamage;
        abilityPower = stats.abilityPower;
        defense = stats.defense;
        critChance = stats.critChance;
        critDamage = stats.critDamage;
        luck = stats.luck;

        mainWeaponId = equipment != null ? equipment.GetMainWeaponId() : string.Empty;
        subWeaponId = equipment != null ? equipment.GetSubWeaponId() : string.Empty;
        activeSlot = equipment != null ? (int)equipment.GetActiveSlot() : 0;
    }
}
