using UnityEngine;
using System.Collections;

public class EnemyDamageSource : MonoBehaviour, IDdaAdaptiveEnemy
{
    [SerializeField] private float damageAmount = 10; // Increased from 3 to be more noticeable
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private float knockbackForce = 10f; // Force applied to player
    private float damageTimer = 0;
    private float baseDamageAmount;
    private float baseDamageCooldown;
    private float currentDamageAmount;
    private float currentDamageCooldown;

    private void Awake()
    {
        baseDamageAmount = damageAmount;
        baseDamageCooldown = damageCooldown;
        currentDamageAmount = baseDamageAmount;
        currentDamageCooldown = baseDamageCooldown;
    }

    private void Update()
    {
        if (damageTimer > 0)
            damageTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        DealDamageToPlayer(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DealDamageToPlayer(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        DealDamageToPlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        DealDamageToPlayer(collision.gameObject);
    }

    private void DealDamageToPlayer(GameObject target)
    {
        if (target.TryGetComponent(out PlayerStats playerStats) && damageTimer <= 0)
        {
            Debug.Log($"Enemy dealing {currentDamageAmount} damage to player");
            
            // Deal damage
            playerStats.TakeDamage(currentDamageAmount);
            damageTimer = currentDamageCooldown;
            
            // Apply knockback to player
            if (target.TryGetComponent(out Knockback playerKnockback))
            {
                playerKnockback.GetKnockedBack(transform, knockbackForce);
            }
            else
            {
                Debug.LogWarning("Player doesn't have Knockback component!");
            }
            
            // Flash player red
            if (target.TryGetComponent(out Flash playerFlash))
            {
                StartCoroutine(playerFlash.FlashRoutine());
            }
            else
            {
                Debug.LogWarning("Player doesn't have Flash component!");
            }
        }
    }

    public void ApplyDdaProfile(DdaDifficultyProfile profile)
    {
        if (profile == null)
        {
            profile = DdaDifficultyProfile.Balanced();
        }

        currentDamageAmount = baseDamageAmount * DdaDifficultyProfile.ClampDamage(profile.damageMultiplier);
        currentDamageCooldown = baseDamageCooldown * DdaDifficultyProfile.ClampDamageCooldown(profile.damageCooldownMultiplier);

        Debug.Log(
            $"[DDA] EnemyDamageSource profile={profile.profileName} " +
            $"damage={baseDamageAmount:0.##}->{currentDamageAmount:0.##} " +
            $"cooldown={baseDamageCooldown:0.##}->{currentDamageCooldown:0.##}",
            this);
    }
}
