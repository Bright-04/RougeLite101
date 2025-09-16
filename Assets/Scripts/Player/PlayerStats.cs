using UnityEngine;
using RougeLite.Events;

public class PlayerStats : EventBehaviour
{
    public float maxHP = 100;
    public float currentHP;
    public float hpRegen = 1;

    public float maxMana = 50;
    public float currentMana;
    public float manaRegen = 1;

    public float maxStamina = 50;
    public float currentStamina  = 50;
    public float staminaRegen = 2;

    public float attackDamage = 10; // AD
    public float abilityPower = 5;  // AP
    public float defense = 2;       // DEF

    public float critChance = 1f; // 10%
    public float critDamage = 1.5f; // 1.5x
    public float luck = 0;

    private float damageCooldown = 1.0f; // seconds of invulnerability after taking damage
    private float damageTimer = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
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


    private void Regenerate()
    {
        float previousHP = currentHP;
        float previousMana = currentMana;
        
        currentHP = Mathf.Min(maxHP, currentHP + hpRegen * Time.deltaTime);
        currentMana = Mathf.Min(maxMana, currentMana + manaRegen * Time.deltaTime);
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
        
        // Broadcast healing event if health was restored
        if (currentHP > previousHP)
        {
            var healData = new PlayerHealthData(currentHP, maxHP, currentHP - previousHP, gameObject);
            var healEvent = new PlayerHealedEvent(healData, gameObject);
            BroadcastEvent(healEvent);
        }
        
        // Broadcast mana restoration event if mana was restored
        if (currentMana > previousMana)
        {
            var manaData = new PlayerManaData(currentMana, maxMana, 0f, "Regeneration");
            var manaEvent = new PlayerManaRestoredEvent(manaData, gameObject);
            BroadcastEvent(manaEvent);
        }
    }

    public void TakeDamage(float damage)
    {
        if (damageTimer > 0)
            return;

        float reducedDamage = Mathf.Max(0, damage - defense);
        currentHP -= reducedDamage;
        Debug.Log($"Player took {reducedDamage} damage, current HP: {currentHP}");

        damageTimer = damageCooldown;

        // Broadcast damage event
        var damageData = new PlayerHealthData(currentHP, maxHP, reducedDamage, null);
        var damageEvent = new PlayerDamagedEvent(damageData, gameObject);
        BroadcastEvent(damageEvent);

        if (currentHP <= 0)
            Die();
    }


    private void Die()
    {
        Debug.Log("Player died!");
        
        // Broadcast death event
        var deathData = new PlayerHealthData(0f, maxHP, 0f, null);
        var deathEvent = new PlayerDeathEvent(deathData, gameObject);
        BroadcastEvent(deathEvent);
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
        float previousMana = currentMana;
        currentMana = Mathf.Max(0, currentMana - amount);
        
        // Broadcast mana used event
        if (amount > 0)
        {
            var manaData = new PlayerManaData(currentMana, maxMana, amount, "Spell");
            var manaEvent = new PlayerManaUsedEvent(manaData, gameObject);
            BroadcastEvent(manaEvent);
        }
    }

    /// <summary>
    /// Heal the player by a specific amount
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0) return;
        
        float previousHP = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        
        if (currentHP > previousHP)
        {
            var healData = new PlayerHealthData(currentHP, maxHP, currentHP - previousHP, gameObject);
            var healEvent = new PlayerHealedEvent(healData, gameObject);
            BroadcastEvent(healEvent);
            
            Debug.Log($"Player healed for {currentHP - previousHP} HP. Current HP: {currentHP}");
        }
    }

    /// <summary>
    /// Restore mana by a specific amount
    /// </summary>
    public void RestoreMana(float amount)
    {
        if (amount <= 0) return;
        
        float previousMana = currentMana;
        currentMana = Mathf.Min(maxMana, currentMana + amount);
        
        if (currentMana > previousMana)
        {
            var manaData = new PlayerManaData(currentMana, maxMana, 0f, "Potion");
            var manaEvent = new PlayerManaRestoredEvent(manaData, gameObject);
            BroadcastEvent(manaEvent);
            
            Debug.Log($"Player restored {currentMana - previousMana} mana. Current mana: {currentMana}");
        }
    }
}
