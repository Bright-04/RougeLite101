using System;
using UnityEngine;

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
    public float currentHP;
    public float currentMana;
    public float currentStamina;
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

    // <summary>
    /// Constructor từ PlayerStats
    /// </summary>
    public PlayerStatsData(PlayerStats stats)
    {
        // Level
        level = stats.GetLevel();
        currentExp = stats.GetCurrentExp();
        levelUpExp = stats.GetLevelUpExp();

        // Core Stats
        currentHP = stats.GetCurrentHP();
        currentMana = stats.GetCurrentMana();
        currentStamina = stats.GetCurrentStamina();
        maxHP = stats.GetNoBuffMaxHP();
        maxMana = stats.GetNoBuffMaxMana();
        maxStamina = stats.GetNoBuffMaxStamina();

        // Regen
        hpRegen = stats.GetNoBuffRegenHP();
        manaRegen = stats.GetNoBuffRegenMana();
        staminaRegen = stats.GetNoBuffRegenStamina();

        // Combat
        attackDamage = stats.GetNoBuffAttackDamage();
        abilityPower = stats.GetNoBuffAbilityPower();
        defense = stats.GetNoBuffDefense();
        critChance = stats.GetNoBuffCritChance();
        critDamage = stats.GetNoBuffCritDamage();
        luck = stats.GetNoBuffLuck();
    }
}
