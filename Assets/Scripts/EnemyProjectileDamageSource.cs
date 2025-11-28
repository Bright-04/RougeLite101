using UnityEngine;
using System.Collections;

public class EnemyProjectileDamageSource : MonoBehaviour
{
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private bool destroyOnHit = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react to player
        if (!other.TryGetComponent(out PlayerStats playerStats))
            return;

        Debug.Log($"[PROJECTILE] Dealing {damageAmount} damage to player");

        // Deal damage
        playerStats.TakeDamage(damageAmount);

        // Knockback
        if (other.TryGetComponent(out Knockback playerKnockback))
        {
            playerKnockback.GetKnockedBack(transform, knockbackForce);
        }
        else
        {
            Debug.LogWarning("[PROJECTILE] Player doesn't have Knockback component!");
        }

        // Flash player red - IMPORTANT: run coroutine on playerFlash, not on this projectile
        if (other.TryGetComponent(out Flash playerFlash))
        {
            // This way, the coroutine keeps running even after the projectile is destroyed
            playerFlash.StartCoroutine(playerFlash.FlashRoutine());
        }
        else
        {
            Debug.LogWarning("[PROJECTILE] Player doesn't have Flash component!");
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
