using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;

    [Header("Skill Tree Data")]
    public int skillPoints = 0;

    [Header("Base Stats")]
    [UnityEngine.Serialization.FormerlySerializedAs("maxHP")] public float baseMaxHP = 100;
    [UnityEngine.Serialization.FormerlySerializedAs("hpRegen")] public float baseHpRegen = 1;

    [UnityEngine.Serialization.FormerlySerializedAs("maxMana")] public float baseMaxMana = 100;
    [UnityEngine.Serialization.FormerlySerializedAs("manaRegen")] public float baseManaRegen = 1;

    [UnityEngine.Serialization.FormerlySerializedAs("maxStamina")] public float baseMaxStamina = 50;
    [UnityEngine.Serialization.FormerlySerializedAs("staminaRegen")] public float baseStaminaRegen = 2;

    [UnityEngine.Serialization.FormerlySerializedAs("attackDamage")] public float baseAttackDamage = 2; // AD
    [UnityEngine.Serialization.FormerlySerializedAs("abilityPower")] public float baseAbilityPower = 5;  // AP
    [UnityEngine.Serialization.FormerlySerializedAs("defense")] public float baseDefense = 0;       // DEF 

    [UnityEngine.Serialization.FormerlySerializedAs("critChance")] public float baseCritChance = 0.1f; // 10%
    [UnityEngine.Serialization.FormerlySerializedAs("critDamage")] public float baseCritDamage = 1.5f; // 1.5x
    [UnityEngine.Serialization.FormerlySerializedAs("luck")] public float baseLuck = 0;

    [Header("Active Current Stats (Calculated)")]
    [HideInInspector] public float maxHP;
    [HideInInspector] public float hpRegen;
    [HideInInspector] public float maxMana;
    [HideInInspector] public float manaRegen;
    [HideInInspector] public float maxStamina;
    [HideInInspector] public float staminaRegen;
    [HideInInspector] public float attackDamage;
    [HideInInspector] public float abilityPower;
    [HideInInspector] public float defense;
    [HideInInspector] public float critChance;
    [HideInInspector] public float critDamage;
    [HideInInspector] public float luck;

    public float currentHP;
    public float currentMana;
    public float currentStamina;

    private float damageCooldown = 1.0f; // seconds of invulnerability after taking damage
    private float damageTimer = 0;

    public float currentExp = 0;
    public float levelUpExp = 10;
    public float level = 0;

    private List<StatModifier> activeModifiers = new List<StatModifier>();

    private void Start()
    {
        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
        }

        RecalculateStats();

        currentHP = maxHP;
        currentMana = maxMana;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        Regenerate();

        if (damageTimer > 0)
            damageTimer -= Time.deltaTime;
    }

    #region Stat Calculation

    public void AddModifier(StatModifier mod)
    {
        activeModifiers.Add(mod);
        RecalculateStats();
    }

    public void ResetModifiers()
    {
        activeModifiers.Clear();
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        // 1. Reset to base values
        maxHP = baseMaxHP;
        hpRegen = baseHpRegen;
        maxMana = baseMaxMana;
        manaRegen = baseManaRegen;
        maxStamina = baseMaxStamina;
        staminaRegen = baseStaminaRegen;
        attackDamage = baseAttackDamage;
        abilityPower = baseAbilityPower;
        defense = baseDefense;
        critChance = baseCritChance;
        critDamage = baseCritDamage;
        luck = baseLuck;

        // 2. Apply modifications (Calculate grouping Flat vs Percentage)
        // A simple approach is applying Flat first, then PercentAdd, then PercentMult.
        foreach (var mod in activeModifiers)
        {
            ApplyModifierValues(mod);
        }

        // Clamp values if necessary
        maxHP = Mathf.Max(1, maxHP);
        maxMana = Mathf.Max(0, maxMana);
        maxStamina = Mathf.Max(0, maxStamina);
        currentHP = Mathf.Min(currentHP, maxHP);
        currentMana = Mathf.Min(currentMana, maxMana);
        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }

    private void ApplyModifierValues(StatModifier mod)
    {
        // To handle Flat vs Percentage correctly mathematically, we'd need Dictionaries per stat.
        // For simplicity and immediate effect without breaking architecture:
        float val = mod.value;
        ref float statRef = ref GetStatRef(mod.stat);

        switch (mod.type)
        {
            case ModifierType.Flat:
                statRef += val;
                break;
            case ModifierType.PercentAdd:
                // Note: PercentAdd ideally calculates from base. 
                // For a highly robust system, we would group these. This assumes sequential scaling.
                statRef += statRef * (val / 100f);
                break;
            case ModifierType.PercentMult:
                statRef *= val;
                break;
        }
    }

    private ref float GetStatRef(StatType statType)
    {
        // Uses C# 7 ref returns to modify local fields cleanly
        switch (statType)
        {
            case StatType.MaxHP: return ref maxHP;
            case StatType.MaxMana: return ref maxMana;
            case StatType.MaxStamina: return ref maxStamina;
            case StatType.AttackDamage: return ref attackDamage;
            case StatType.AbilityPower: return ref abilityPower;
            case StatType.Defense: return ref defense;
            case StatType.CritChance: return ref critChance;
            case StatType.CritDamage: return ref critDamage;
            case StatType.Luck: return ref luck;
            // Regen stats fallback if added to enum later
            default: return ref luck; 
        }
    }

    #endregion

    private void Regenerate()
    {
        currentHP = Mathf.Min(maxHP, currentHP + hpRegen * Time.deltaTime);
        currentMana = Mathf.Min(maxMana, currentMana + manaRegen * Time.deltaTime);
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
    }

    public void TriggerInvincibility(float duration)
    {
        damageTimer = duration;
    }

    public void TakeDamage(float damage)
    {
        if (damageTimer > 0)
        {
            Debug.Log("Damage blocked by cooldown");
            return;
        }

        float reducedDamage = Mathf.Max(0, damage - defense);
        currentHP -= reducedDamage;

        Debug.Log($"Player took {reducedDamage} damage (from {damage}). HP: {currentHP}/{maxHP}");

        damageTimer = damageCooldown;

        if (currentHP <= 0)
        {
            Die();
            Respawn();
        }

    }

    private void Die()
    {
        // TODO: Implement proper death handling (game over screen, restart, etc.)
        Debug.Log("Player is dead");
    }

    private void Respawn()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            var flash = player.GetComponent<Flash>();
            if (flash != null)
            {
                flash.ResetMaterial();
            }

            ResetStatsOnRespawn();
            player.transform.position = Vector3.zero;

            SceneManager.LoadScene("GameHome");
        }
    }

    private void ResetStatsOnRespawn()
    {
        currentHP = maxHP;
        currentMana = maxMana;
        currentStamina = maxStamina;
        damageTimer = 0;
    }

    public bool TryCrit()
    {
        return Random.value < critChance;
    }

    public float GetCritMultiplier()
    {
        return critDamage;
    }

    public void UseMana(float amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
    }

    // ============ SAVE/LOAD METHODS ============

    public void LoadFromData(PlayerStatsData data)
    {
        // Level System
        level = data.level;
        currentExp = data.currentExp;
        levelUpExp = data.levelUpExp;
        skillPoints = data.skillPoints;

        // Core Stats (Base stats loaded)
        baseMaxHP = data.maxHP;
        baseMaxMana = data.maxMana;
        baseMaxStamina = data.maxStamina;

        // Regeneration
        baseHpRegen = data.hpRegen;
        baseManaRegen = data.manaRegen;
        baseStaminaRegen = data.staminaRegen;

        // Combat Stats
        baseAttackDamage = data.attackDamage;
        baseAbilityPower = data.abilityPower;
        baseDefense = data.defense;
        baseCritChance = data.critChance;
        baseCritDamage = data.critDamage;
        baseLuck = data.luck;

        // Clear existing modifiers since they will be re-applied by SkillManager
        ResetModifiers();

        // Pass unlocked items mapping to SkillManager so it can re-apply them.
        SkillManager skillManager = FindAnyObjectByType<SkillManager>();
        if (skillManager != null)
        {
            skillManager.LoadUnlockedSkills(data.unlockedSkillIDs);
        }

        // Reset current values to max after loading
        currentHP = maxHP;
        currentMana = maxMana;
        currentStamina = maxStamina;

        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
        }

        if (equipmentManager != null)
        {
            EquipmentManager.WeaponSlot loadedSlot = data.activeSlot == (int)EquipmentManager.WeaponSlot.Sub
                ? EquipmentManager.WeaponSlot.Sub
                : EquipmentManager.WeaponSlot.Main;

            equipmentManager.LoadWeapons(data.mainWeaponId, data.subWeaponId, loadedSlot);
        }
        else
        {
            Debug.LogWarning("PlayerStats: EquipmentManager not found while loading weapon data.");
        }

        Debug.Log($"Loaded Player Stats: Level {level}, HP {maxHP}, ATK {attackDamage}");
    }
}
