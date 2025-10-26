using UnityEngine;

public class PlayerStats : MonoBehaviour
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
        currentHP = Mathf.Min(maxHP, currentHP + hpRegen * Time.deltaTime);
        currentMana = Mathf.Min(maxMana, currentMana + manaRegen * Time.deltaTime);
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
    }

    public void TakeDamage(float damage)
    {
        if (damageTimer > 0)
            return;

        float reducedDamage = Mathf.Max(0, damage - defense);
        currentHP -= reducedDamage;
        Debug.Log($"Player took {reducedDamage} damage, current HP: {currentHP}");

        damageTimer = damageCooldown;

        if (currentHP <= 0)
            Die();
    }


    private void Die()
    {
        Debug.Log("Player died!");
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
}
