using UnityEngine;
using System.Collections;
using RougeLite.System;

public class EnemyDamageSource : MonoBehaviour
{
    [SerializeField] private float damageAmount = 10; // Increased from 3 to be more noticeable
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private float knockbackForce = 10f; // Force applied to player
    private float damageTimer = 0;

    private void Start()
    {
        // Scale damage based on adaptive difficulty
        if (DifficultyManager.Instance != null)
        {
            float baseDmg = damageAmount;
            damageAmount *= DifficultyManager.Instance.GetDamageMultiplier();
            Debug.Log($"<color=orange>[Adaptive AI]</color> {gameObject.name} DMG: {baseDmg} → {damageAmount:F1} (x{DifficultyManager.Instance.GetDamageMultiplier():F2})");
        }
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
            Debug.Log($"Enemy dealing {damageAmount} damage to player");
            
            // Deal damage
            playerStats.TakeDamage(damageAmount);
            damageTimer = damageCooldown;
            
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

}
