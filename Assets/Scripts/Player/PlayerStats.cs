using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private EquipmentController equipmentController;
    [SerializeField] private string fallbackHubSceneName = "GameHome";

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
    private float armorMaxHealthBonus;
    private float armorDefenseBonus;
    private bool isDead;

    public float currentExp = 0;
    public float levelUpExp = 10;
    public float level = 0;

    private void Start()
    {
        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
        }

        if (equipmentController == null)
        {
            equipmentController = GetComponent<EquipmentController>();
        }

        if (equipmentController != null)
        {
            equipmentController.OnArmorEquipped += OnArmorEquipped;
            equipmentController.ReplayEquippedArmor();
        }

        currentHP = GetTotalMaxHP();
        currentMana = maxMana;
        currentStamina = maxStamina;
    }

    private void OnDestroy()
    {
        if (equipmentController != null)
        {
            equipmentController.OnArmorEquipped -= OnArmorEquipped;
        }
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
        currentHP = Mathf.Min(GetTotalMaxHP(), currentHP + hpRegen * Time.deltaTime);
        currentMana = Mathf.Min(maxMana, currentMana + manaRegen * Time.deltaTime);
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
    }

    public void HealthRestore(float health)
    {
        currentHP = Mathf.Min(GetTotalMaxHP(), currentHP + health);
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

        float reducedDamage = Mathf.Max(0, damage - GetTotalDefense());
        currentHP -= reducedDamage;

        Debug.Log($"Player took {reducedDamage} damage (from {damage}). HP: {currentHP}/{GetTotalMaxHP()}");

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
            // Reset any ongoing flash effects
            var flash = player.GetComponent<Flash>();
            if (flash != null)
            {
                flash.ResetMaterial();
            }

            ResetStatsOnRespawn();
            player.transform.position = Vector3.zero;

            SceneManager.LoadScene(fallbackHubSceneName);
        }
    }

    private void ResetStatsOnRespawn()
    {
        isDead = false;
        currentHP = GetTotalMaxHP();
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
        currentHP = GetTotalMaxHP();
        currentMana = maxMana;
        currentStamina = maxStamina;

        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
        }

        if (equipmentController == null)
        {
            equipmentController = GetComponent<EquipmentController>();
        }

        if (equipmentController != null)
        {
            equipmentController.LoadArmor(data.shieldArmorId, data.helmetArmorId, data.greavesArmorId, data.bootsArmorId);
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

    public float GetTotalMaxHP()
    {
        return maxHP + armorMaxHealthBonus;
    }

    public float GetTotalDefense()
    {
        return defense + armorDefenseBonus;
    }

    private void OnArmorEquipped(EquipmentController.ArmorSlot slot, ArmorDefinitionSO previousArmor, ArmorDefinitionSO newArmor)
    {
        if (previousArmor != null)
        {
            armorMaxHealthBonus -= previousArmor.MaxHealthBonus;
            armorDefenseBonus -= previousArmor.Defense;
        }

        if (newArmor != null)
        {
            armorMaxHealthBonus += newArmor.MaxHealthBonus;
            armorDefenseBonus += newArmor.Defense;
        }

        currentHP = Mathf.Min(currentHP, GetTotalMaxHP());
    }
}
