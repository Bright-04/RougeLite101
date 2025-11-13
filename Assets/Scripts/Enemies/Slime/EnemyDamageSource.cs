using UnityEngine;
using System.Collections;

public class EnemyDamageSource : MonoBehaviour
{
    [SerializeField] private float damageAmount = 3;
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
            // Deal damage
            playerStats.TakeDamage(damageAmount);
            damageTimer = damageCooldown;
            
            // Apply knockback to player
            if (other.TryGetComponent(out Knockback playerKnockback))
            {
                playerKnockback.GetKnockedBack(transform, knockbackForce);
            }
            
            // Flash player red
            if (other.TryGetComponent(out Flash playerFlash))
            {
                StartCoroutine(playerFlash.FlashRoutine());
            }
        }
    }

}
