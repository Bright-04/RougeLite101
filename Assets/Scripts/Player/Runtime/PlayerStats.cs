using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private ArmorController armorController;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private string fallbackHubSceneName = "GameHome";

    [SerializeField] private float maxHP = 100;
    [SerializeField] private float currentHP;
    [SerializeField] private float hpRegen = 1;

    [SerializeField] private float maxMana = 100;
    [SerializeField] private float currentMana;
    [SerializeField] private float manaRegen = 1;

    [SerializeField] private float maxStamina = 50;
    [SerializeField] private float currentStamina = 50;
    [SerializeField] private float staminaRegen = 2;

    [SerializeField] private float attackDamage = 2; // AD
    [SerializeField] private float abilityPower = 5;  // AP
    [SerializeField] private float defense = 0;       // DEF - Changed from 2 to 0 so enemies can deal damage

    [SerializeField] private float critChance = 0.1f; // 10%
    [SerializeField] private float critDamage = 1.5f; // 1.5x
    [SerializeField] private float luck = 0;

    [SerializeField] private float damageCooldown = 1.0f; // seconds of invulnerability after taking damage
    [SerializeField] private float damageTimer = 0;
    [SerializeField] private bool isDead = false;
    public bool IsDead => isDead;

    [SerializeField] private float moveSpeed = 10f;

    [SerializeField] private float currentExp = 0;
    [SerializeField] private float levelUpExp = 10;
    [SerializeField] private float level = 0;

    //STAT BUFF
    [SerializeField] private float buffMaxHP = 0;
    [SerializeField] private float buffHpRegen = 0;

    [SerializeField] private float buffMaxMana = 0;
    [SerializeField] private float buffManaRegen = 0;

    [SerializeField] private float buffMaxStamina = 0;
    [SerializeField] private float buffStaminaRegen = 0;

    [SerializeField] private float buffAttackDamage = 0;
    [SerializeField] private float buffAbilityPower = 0;
    [SerializeField] private float buffDefense = 0;

    [SerializeField] private float buffCritChance = 0;
    [SerializeField] private float buffCritDamage = 0;
    [SerializeField] private float buffLuck = 0;

    [SerializeField] private float buffSpeed = 0;

    private bool hasLoadedSave;

    private void Start()
    {
        if (equipmentManager == null)
        {
            equipmentManager = GetComponent<EquipmentManager>();
        }

        if (armorController == null)
        {
            armorController = GetComponent<ArmorController>();
        }

        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }

        if (hasLoadedSave) return;
        currentHP = GetMaxHP();
        currentMana = GetMaxMana();
        currentStamina = GetMaxStamina();
    }

   

    private void Update()
    {
        //if (currentHP <= 0)
        //{
        //    Die();
        //}
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
        currentHP = Mathf.Min(GetMaxHP(), currentHP + GetRegenHP() * Time.deltaTime);
        currentMana = Mathf.Min(GetMaxMana(), currentMana + GetRegenMana() * Time.deltaTime);
        currentStamina = Mathf.Min(GetMaxStamina(), currentStamina + GetRegenStamina() * Time.deltaTime);
    }

    //Get functions
    public float GetMoveSpeed()
    {
        return moveSpeed + buffSpeed;
    }

    public float GetNoBuffMoveSpeed()
    {
        return moveSpeed;
    }

    public float GetMaxHP()
    {
        if ((maxHP + buffMaxHP) <= 0){
            return 1;
        }
        return maxHP + buffMaxHP;
    }

    public float GetNoBuffMaxHP()
    {
        return maxHP;
    }

    public float GetCurrentHP()
    {
        return currentHP;
    }

    public float GetRegenHP()
    {
        if((hpRegen + buffHpRegen) <= 0){
            return 1;
        }
        return hpRegen + buffHpRegen;
    }

    public float GetNoBuffRegenHP()
    {
        return hpRegen;
    }

    public float GetMaxMana()
    {
        if((maxMana + buffMaxMana) <= 0){
            return 1;
        }
        return maxMana + buffMaxMana;
    }

    public float GetNoBuffMaxMana()
    {
        return maxMana;
    }

    public float GetCurrentMana()
    {
        return currentMana;
    }

    public float GetRegenMana()
    {
        if ((manaRegen + buffManaRegen) <= 0)
        {
            return 1;
        }
        return manaRegen + buffManaRegen;
    }

    public float GetNoBuffRegenMana()
    {
        return manaRegen;
    }

    public float GetMaxStamina()
    {
        if((maxStamina + buffMaxStamina) <= 0){
            return 1;
        }
        return maxStamina + buffMaxStamina;
    }

    public float GetNoBuffMaxStamina()
    {
        return maxStamina;
    }

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetRegenStamina()
    {
        if ((staminaRegen + buffStaminaRegen) <= 0)
        {
            return 1;
        }
        return staminaRegen + buffStaminaRegen;
    }

    public float GetNoBuffRegenStamina()
    {
        return staminaRegen;
    }

    public float GetDefense()
    {
        return defense + buffDefense;
    }

    public float GetNoBuffDefense()
    {
        return defense;
    }

    public float GetAttackDamage()
    {
        if((attackDamage + buffAttackDamage) <= 0)
        {
            return 1;
        }
        return attackDamage + buffAttackDamage;
    }

    public float GetNoBuffAttackDamage()
    {
        return attackDamage;
    }

    public float GetAbilityPower()
    {
        if((abilityPower + buffAbilityPower) <= 0)
        {
            return 1;
        }
        return abilityPower + buffAbilityPower;
    }

    public float GetNoBuffAbilityPower()
    {
        return abilityPower;
    }

    public float GetCritChance()
    {
        if((critChance + buffCritChance) <= 0)
        {
            return 1;
        }
        return critChance + buffCritChance;
    }

    public float GetNoBuffCritChance()
    {
        return critChance;
    }

    public float GetCritDamage()
    {
        if((critDamage + buffCritDamage) <= 0)
        {
            return 1;
        }
        return critDamage + buffCritDamage;
    }

    public float GetNoBuffCritDamage()
    {
        return critDamage;
    }

    public float GetLuck()
    {
        return luck + buffLuck;
    }

    public float GetNoBuffLuck()
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
        currentHP = GetMaxHP() * hpRatio;
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

    public void ResetAllBuffs()
    {
        buffMaxHP = 0;
        buffHpRegen = 0;

        buffMaxMana = 0;
        buffManaRegen = 0;

        buffMaxStamina = 0;
        buffStaminaRegen = 0;

        buffAttackDamage = 0;
        buffAbilityPower = 0;
        buffDefense = 0;

        buffCritChance = 0;
        buffCritDamage = 0;

        buffLuck = 0;
        buffSpeed = 0;
    }

    public void BuffSpeed(float speed)
    {
        buffSpeed += speed;
    }

    public void BuffMaxHealth(float health)
    {
        bool wasFullHP = currentHP >= GetMaxHP();

        buffMaxHP += health;

        if (wasFullHP)
        {
            currentHP = GetMaxHP();
        }

        currentHP = Mathf.Min(currentHP, GetMaxHP());

    }

    public void BuffHpRegen(float regen)
    {
        buffHpRegen += regen;
    }

    public void BuffMaxMana(float mana)
    {
        bool wasFullMana = currentMana >= GetMaxMana();

        buffMaxMana += mana;

        if (wasFullMana)
        {
            currentMana = GetMaxMana();
        }

        currentMana = Mathf.Min(currentMana, GetMaxMana());

    }

    public void BuffManaRegen(float regen)
    {
        buffManaRegen += regen;
    }

    public void BuffMaxStamina(float stamina)
    {
        bool wasFullStamina = currentStamina >= GetMaxStamina();

        buffMaxStamina += stamina;

        if (wasFullStamina)
        {
            currentStamina = GetMaxStamina();
        }

        currentStamina = Mathf.Min(currentStamina, GetMaxStamina());
    }

    public void BuffStaminaRegen(float regen)
    {
        buffStaminaRegen += regen;
    }

    public void BuffAttackDamage(float damage)
    {
        buffAttackDamage += damage;
    }

    public void BuffAbilityPower(float power)
    {
        buffAbilityPower += power;
    }

    public void BuffCritChance(float crit)
    {
        buffCritChance += crit;
    }

    public void BuffCritDamage(float crit)
    {
        buffCritDamage += crit;
    }

    public void BuffDefense(float def)
    {
        buffDefense += def;
    }

    public void BuffLuck(float val)
    {
        buffLuck += val;
    }
    
    //restore effect
    public void HealthRestore(float health)
    {
        currentHP = Mathf.Min(GetMaxHP(), currentHP + health);
    }

    public void ManaRestore(float mana)
    {
        currentMana = Mathf.Min(GetMaxMana(), currentMana + mana);
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

        float reducedDamage = Mathf.Max(0, damage - GetDefense());
        currentHP -= reducedDamage;

        if (reducedDamage > 0f)
        {
            DdaTelemetryService.Instance?.RecordDamageTaken(reducedDamage);
        }

        Debug.Log($"Player took {reducedDamage} damage (from {damage}). HP: {currentHP}/{maxHP}");

        damageTimer = damageCooldown;

        if (currentHP <= 0 && isDead == false)
        {
            Die();
        }

    }


    private void Die()
    {
        Debug.Log("Player is dead");
        ResetStatsOnRespawn();
        isDead = true;
        if (inventoryController != null)
        {
            inventoryController.OnPlayerDeath();
        }

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
            player.transform.position = new Vector3(0f, 9f, 0f);

            SceneManager.LoadScene(fallbackHubSceneName);
        }
    }

    public void ResetStatsOnRespawn()
    {
        Debug.Log("RESET STATS CALLED");
        isDead = false;
        //reset buffs
        ResetAllBuffs();
        equipmentManager?.ReapplyWeaponBuffs();
        armorController?.ReapplyArmorBuffs();

        currentHP = GetMaxHP();
        currentMana = GetMaxMana();
        currentStamina = GetMaxStamina();
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
        return Random.value < GetCritChance();
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
        if (data == null)
        {
            Debug.LogWarning("PlayerStats: LoadFromData called with null data.", this);
            return;
        }
        hasLoadedSave = true;
        // reset buffs
        ResetAllBuffs();

        // load base stats
        // Level System
        level = data.level;
        currentExp = data.currentExp;
        levelUpExp = data.levelUpExp;

        // Core Stats
        currentHP = data.currentHP;
        currentMana = data.currentMana;
        currentStamina = data.currentStamina;
        maxHP = data.maxHP;
        maxMana = data.maxMana;
        maxStamina = data.maxStamina;

        // Regeneration
        hpRegen = data.hpRegen;
        manaRegen = data.manaRegen;
        staminaRegen = data.staminaRegen;

        // Combat Stats
        attackDamage = data.attackDamage;
        abilityPower = data.abilityPower;
        defense = data.defense;
        critChance = data.critChance;
        critDamage = data.critDamage;
        luck = data.luck;

        // Reset current values to max after loading
        currentHP = maxHP;
        currentMana = maxMana;
        currentStamina = maxStamina;
        Debug.Log($"Loaded Player Stats: Level {level}, HP {maxHP}, ATK {attackDamage}");
    }

    public void RefreshAfterEquipmentLoad()
    {
        currentHP = Mathf.Min(currentHP, GetMaxHP());
        currentMana = Mathf.Min(currentMana, GetMaxMana());
        currentStamina = Mathf.Min(currentStamina, GetMaxStamina());
    }

    public void RebuildAllBuffs()
    {
        ResetAllBuffs();

        equipmentManager?.ReapplyWeaponBuffs();
        armorController?.ReapplyArmorBuffs();
        
        currentHP = Mathf.Min(currentHP, GetMaxHP());
        currentMana = Mathf.Min(currentMana, GetMaxMana());
        currentStamina = Mathf.Min(currentStamina, GetMaxStamina());
    }
}
