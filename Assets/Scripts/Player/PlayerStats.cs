using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    public float maxHP = 100;
    public float currentHP;
    public float hpRegen = 1;

    public float maxMana = 500;
    public float currentMana;
    public float manaRegen = 1;

    public float maxStamina = 50;
    public float currentStamina  = 50;
    public float staminaRegen = 2;

    public float attackDamage = 10; // AD
    public float abilityPower = 5;  // AP
    public float defense = 0;       // DEF - Changed from 2 to 0 so enemies can deal damage

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
}
