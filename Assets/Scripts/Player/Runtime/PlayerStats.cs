using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private string fallbackHubSceneName = "GameHome";

    private float maxHP = 100;
    private float currentHP;
    private float hpRegen = 1;

    private float maxMana = 100;
    private float currentMana;
    private float manaRegen = 1;

    private float maxStamina = 50;
    private float currentStamina = 50;
    private float staminaRegen = 2;

    private float attackDamage = 2; // AD
    private float abilityPower = 5;  // AP
    private float defense = 0;       // DEF - Changed from 2 to 0 so enemies can deal damage

    private float critChance = 0.1f; // 10%
    private float critDamage = 1.5f; // 1.5x
    private float luck = 0;

    private float damageCooldown = 1.0f; // seconds of invulnerability after taking damage
    private float damageTimer = 0;
    private bool isDead = false;
    public bool IsDead => isDead;

    private float currentExp = 0;
    private float levelUpExp = 10;
    private float level = 0;

    //STAT BUFF
    private float buffMaxHP = 0;
    private float buffHpRegen = 0;

    private float buffMaxMana = 0;
    private float buffManaRegen = 0;

    private float buffMaxStamina = 0;
    private float buffStaminaRegen = 0;

    private float buffAttackDamage = 0;
    private float buffAbilityPower = 0;
    private float buffDefense = 0;

    private float buffCritChance = 0;
    private float buffCritDamage = 0;
    private float buffLuck = 0;

    private void Start()
    {
        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
        }

        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }


        currentHP = maxHP;
        currentMana = maxMana;
        currentStamina = maxStamina;
    }

   

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        Regenerate();

        if (damageTimer > 0)
            damageTimer -= Time.deltaTime;
    }


    private void Regenerate()
    {
        currentHP = Mathf.Min(maxHP, currentHP + hpRegen * Time.deltaTime);
        currentMana = Mathf.Min(maxMana, currentMana + manaRegen * Time.deltaTime);
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
    }

    //Get functions
    public float GetMaxHP()
    {
        return maxHP;
    }

    public float GetNoBuffMaxHP()
    {
        return maxHP - buffMaxHP;
    }

    public float GetCurrentHP()
    {
        return currentHP;
    }

    public float GetRegenHP()
    {
        return hpRegen;
    }

    public float GetNoBuffRegenHP()
    {
        return hpRegen - buffHpRegen;
    }

    public float GetMaxMana()
    {
        return maxMana;
    }

    public float GetNoBuffMaxMana()
    {
        return maxMana - buffMaxMana;
    }

    public float GetCurrentMana()
    {
        return currentMana;
    }

    public float GetRegenMana()
    {
        return manaRegen;
    }

    public float GetNoBuffRegenMana()
    {
        return manaRegen - buffManaRegen;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }

    public float GetNoBuffMaxStamina()
    {
        return maxStamina - buffMaxStamina;
    }

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetRegenStamina()
    {
        return staminaRegen;
    }

    public float GetNoBuffRegenStamina()
    {
        return staminaRegen - buffStaminaRegen;
    }

    public float GetDefense()
    {
        return defense;
    }

    public float GetAttackDamage()
    {
        return attackDamage;
    }

    public float GetAbilityPower()
    {
        return abilityPower;
    }

    public float GetCritChance()
    {
        return critChance;
    }

    public float GetCritDamage()
    {
        return critDamage;
    }

    public float GetLuck()
    {
        return luck;
    }

    public float GetCurrentExp()
    {
        return currentExp;
    }

    public float GetLevelUpExp()
    {
        return levelUpExp;
    }

    public float GetLevel()
    {
        return level;
    }

    //WinFlowValidator
    public void WinFlowSetCurrentHP(float hpRatio)
    {
        currentHP = maxHP * hpRatio;
    }
    //level up stat functions
    public void LevelUpHP(float hpGrowth)
    {
        maxHP += hpGrowth;
    }

    public void LevelUpMana(float manaGrowth)
    {
        maxMana += manaGrowth;
    }

    public void LevelUpStamina(float staminaGrowth)
    {
        maxStamina += staminaGrowth;
    }

    public void LevelUpAttackDamage(float attackDamageGrowth)
    {
        attackDamage += attackDamageGrowth;
    }

    public void LevelUpAbilityPower(float abilityPowerGrowth)
    {
        abilityPower += abilityPowerGrowth;
    }

    public void LevelUpDefense(float defenseGrowth)
    {
        defense += defenseGrowth;
    }

    public void LevelUpHPRegen(float hpRegenGrowth)
    {
        hpRegen += hpRegenGrowth;
    }

    public void LevelUpManaRegen(float manaRegenGrowth)
    {
        manaRegen += manaRegenGrowth;
    }

    public void LevelUpStaminaRegen(float staminaRegenGrowth)
    {
        staminaRegen += staminaRegenGrowth;
    }

    public void IncreaseLevel(float expMultiplier)
    {
        level++;
        currentExp -= levelUpExp;
        levelUpExp = Mathf.RoundToInt(levelUpExp * expMultiplier);
    }

    public void IncreaseExp(float amount)
    {
        currentExp += amount;
    }
    
    //buff functions
    public void BuffMaxHealth(float health)
    {
        buffMaxHP = health;
        if (currentHP == maxHP)
        {
            currentHP = maxHP + buffMaxHP;
        }
        maxHP = maxHP + buffMaxHP;

    }

    public void BuffHpRegen(float regen)
    {
        buffHpRegen = regen;
        hpRegen = hpRegen + buffHpRegen;
    }

    public void BuffMaxMana(float mana)
    {
        buffMaxMana = mana;
        if (currentMana == maxMana)
        {
            currentMana = maxMana + buffMaxMana;
        }
        maxMana = maxMana + buffMaxMana;

    }

    public void BuffManaRegen(float regen)
    {
        buffManaRegen = regen;
        manaRegen = manaRegen + buffManaRegen;
    }

    public void BuffMaxStamina(float stamina)
    {
        buffMaxStamina = stamina;
        if (currentStamina == maxStamina)
        {
            currentStamina = maxStamina + buffMaxStamina;
        }
        maxStamina = maxStamina + buffMaxStamina;
    }

    public void BuffStaminaRegen(float regen)
    {
        buffStaminaRegen = regen;
        staminaRegen = staminaRegen + buffStaminaRegen;
    }

    public void BuffAttackDamage(float damage)
    {
        buffAttackDamage = damage;
        attackDamage = attackDamage + buffAttackDamage;
    }

    public void BuffAbilityPower(float power)
    {
        buffAbilityPower = power;
        abilityPower = abilityPower + buffAbilityPower;
    }

    public void BuffCritChance(float crit)
    {
        buffCritChance = crit;
        critChance = critChance + buffCritChance;
    }

    public void BuffCritDamage(float crit)
    {
        buffCritDamage = crit;
        critDamage = critDamage + buffCritDamage;
    }

    public void BuffDefense(float def)
    {
        buffDefense = def;
        defense = defense + buffDefense;
    }

    public void BuffLuck(float val)
    {
        buffLuck = val;
        luck = Mathf.Max(100, luck + val);
    }
    
    //restore effect
    public void HealthRestore(float health)
    {
        currentHP = Mathf.Min(maxHP, currentHP + health);
    }

    public void ManaRestore(float mana)
    {
        currentMana = Mathf.Min(maxMana, currentMana + mana);
    }

    public void TriggerInvincibility(float duration)
    {
        damageTimer = duration;
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

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
        }

    }


    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        if (inventoryController != null)
        {
            inventoryController.OnPlayerDeath();
        }

        Debug.Log("Player is dead");

        RunResultController runResultController = RunResultController.Instance != null
            ? RunResultController.Instance
            : FindAnyObjectByType<RunResultController>(FindObjectsInactive.Include);

        if (runResultController == null)
        {
            Debug.LogError("PlayerStats: RunResultController not found. Falling back to hub return.", this);
            RespawnToHubFallback();
            return;
        }

        if (!runResultController.ShowLose(this))
        {
            Debug.LogError("PlayerStats: Failed to show lose result UI. Falling back to hub return.", this);
            RespawnToHubFallback();
        }
    }

    private void RespawnToHubFallback()
    {
        AutoSaveManager.TrySaveActiveSceneState();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            ResetTransientState(player);

            ResetStatsOnRespawn();
            player.transform.position = Vector3.zero;

            SceneManager.LoadScene(fallbackHubSceneName);
        }
    }

    private void ResetStatsOnRespawn()
    {
        isDead = false;
        currentHP = maxHP;
        currentMana = maxMana;
        currentStamina = maxStamina;
        damageTimer = 0;
    }

    public void ResetTransientState()
    {
        ResetTransientState(gameObject);
    }

    private static void ResetTransientState(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        var flash = target.GetComponent<Flash>();
        if (flash != null)
        {
            flash.ResetMaterial();
        }

        var knockback = target.GetComponent<Knockback>();
        if (knockback != null)
        {
            knockback.ResetState();
        }
    }

    public bool TryCrit()
    {
        return Random.value < critChance;
    }


    

    //public float GetCritMultiplier()
    //{   
    //    return critDamage;
    //}


    public void UseMana(float amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
    }

    // ============ SAVE/LOAD METHODS ============

    /// <summary>
    /// Load data từ PlayerStatsData vào PlayerStats
    /// </summary>
    public void LoadFromData(PlayerStatsData data)
    {
        // Level System
        level = data.level;
        currentExp = data.currentExp;
        levelUpExp = data.levelUpExp;

        // Core Stats
        maxHP = data.maxHP + buffMaxHP;
        maxMana = data.maxMana + buffMaxMana;
        maxStamina = data.maxStamina + buffMaxStamina;

        // Regeneration
        hpRegen = data.hpRegen + buffHpRegen;
        manaRegen = data.manaRegen + buffManaRegen;
        staminaRegen = data.staminaRegen + buffStaminaRegen;

        // Combat Stats
        attackDamage = data.attackDamage + buffAttackDamage;
        abilityPower = data.abilityPower + buffAbilityPower;
        defense = data.defense + buffDefense;
        critChance = data.critChance + buffCritChance;
        critDamage = data.critDamage + buffCritDamage;
        luck = data.luck + buffLuck;

        // Reset current values to max after loading
        currentHP = maxHP;
        currentMana = maxMana;
        currentStamina = maxStamina;

        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
        }

        //if (equipmentController == null)
        //{
        //    equipmentController = GetComponent<EquipmentController>();
        //}

        //if (equipmentController != null)
        //{
        //    equipmentController.LoadArmor(data.shieldArmorId, data.helmetArmorId, data.greavesArmorId, data.bootsArmorId);
        //}

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
