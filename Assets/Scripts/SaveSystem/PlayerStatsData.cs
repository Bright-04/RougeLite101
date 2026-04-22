using System;
using System.Collections.Generic;

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
    public int skillPoints;
    
    // Skill Tree Data
    public List<string> unlockedSkillIDs = new List<string>();

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
    public PlayerStatsData(PlayerStats stats, EquipmentManager equipment, SkillManager skillManager)
    {
        // Level
        level = stats.level;
        currentExp = stats.currentExp;
        levelUpExp = stats.levelUpExp;
        skillPoints = stats.skillPoints;

        if (skillManager != null)
        {
            unlockedSkillIDs = new List<string>(skillManager.unlockedSkillIDs);
        }

        // Core Stats (Store Base Stats only)
        maxHP = stats.baseMaxHP;
        maxMana = stats.baseMaxMana;
        maxStamina = stats.baseMaxStamina;

        // Regen
        hpRegen = stats.baseHpRegen;
        manaRegen = stats.baseManaRegen;
        staminaRegen = stats.baseStaminaRegen;

        // Combat
        attackDamage = stats.baseAttackDamage;
        abilityPower = stats.baseAbilityPower;
        defense = stats.baseDefense;
        critChance = stats.baseCritChance;
        critDamage = stats.baseCritDamage;
        luck = stats.baseLuck;

        mainWeaponId = equipment != null ? equipment.GetMainWeaponId() : string.Empty;
        subWeaponId = equipment != null ? equipment.GetSubWeaponId() : string.Empty;
        activeSlot = equipment != null ? (int)equipment.GetActiveSlot() : 0;
    }
}
