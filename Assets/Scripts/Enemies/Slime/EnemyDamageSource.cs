using UnityEngine;
using System.Collections;

public class EnemyDamageSource : MonoBehaviour
{
    [SerializeField] private float damageAmount = 10; // Increased from 3 to be more noticeable
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private float knockbackForce = 10f; // Force applied to player
    private float damageTimer = 0;

    private void Update()
    {
        if (damageTimer > 0)
            damageTimer -= Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerStats playerStats) && damageTimer <= 0)
        {
            Debug.Log($"Enemy dealing {damageAmount} damage to player");
            
            // Deal damage
            playerStats.TakeDamage(damageAmount);
            damageTimer = damageCooldown;
            
            // Apply knockback to player
            if (other.TryGetComponent(out Knockback playerKnockback))
            {
                playerKnockback.GetKnockedBack(transform, knockbackForce);
            }
            else
            {
                Debug.LogWarning("Player doesn't have Knockback component!");
            }
            
            // Flash player red
            if (other.TryGetComponent(out Flash playerFlash))
            {
                StartCoroutine(playerFlash.FlashRoutine());
            }
            else
            {
                Debug.LogWarning("Player doesn't have Flash component!");
            }
        }
    }

}
