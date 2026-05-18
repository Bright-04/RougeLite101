using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;

    [SerializeField] private InventoryController inventoryController;

    public float maxHP = 100;
    public float currentHP;
    public float hpRegen = 1;

    public float maxMana = 100;
    public float currentMana;
    public float manaRegen = 1;

    public float maxStamina = 50;
    public float currentStamina = 50;
    public float staminaRegen = 2;

    public float attackDamage = 2; // AD
    public float abilityPower = 5;  // AP
    public float defense = 0;       // DEF - Changed from 2 to 0 so enemies can deal damage

    public float critChance = 0.1f; // 10%
    public float critDamage = 1.5f; // 1.5x
    public float luck = 0;

    private float damageCooldown = 1.0f; // seconds of invulnerability after taking damage
    private float damageTimer = 0;

    public float currentExp = 0;
    public float levelUpExp = 10;
    public float level = 0;

    //STAT BUFF
    public float buffMaxHP = 0;
    public float buffHpRegen = 0;

    public float buffMaxMana = 0;
    public float buffManaRegen = 0;

    public float buffMaxStamina = 0;
    public float buffStaminaRegen = 0;

    public float buffAttackDamage = 0; 
    public float buffAbilityPower = 0;  
    public float buffDefense = 0;      

    public float buffCritChance = 0; 
    public float buffCritDamage = 0;
    public float buffLuck = 0;

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
        //Regenerate();
        //if (currentHP <= 0)
        //{
        //    Die();
        //    Respawn();
        //}

        if (damageTimer > 0)
            damageTimer -= Time.deltaTime;
    }


    private void Regenerate()
    {
        currentHP = Mathf.Min(maxHP, currentHP + hpRegen * Time.deltaTime);
        currentMana = Mathf.Min(maxMana, currentMana + manaRegen * Time.deltaTime);
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
    }

    public void BuffMaxHealth(float health)
    {
        buffMaxHP = health;
        if(currentHP == maxHP)
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
        if(currentStamina == maxStamina)
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
        critDamage = critDamage + buffCritChance;
    }

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
        if (inventoryController != null)
        {
            inventoryController.OnPlayerDeath();
        }
        Debug.Log("Player is dead");
    }

    private void Respawn()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            // Reset any ongoing flash effects
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
