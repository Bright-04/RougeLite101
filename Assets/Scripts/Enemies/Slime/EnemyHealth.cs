using UnityEngine;
using System.Collections;

/// <summary>
/// Generic health component for enemies. Can be used for any enemy type.
/// Handles damage, hit reactions (flash + knockback), and death animation.
/// </summary>
[RequireComponent(typeof(EnemyDeathNotifier))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int startingHealth = 3;
    
    [Header("Hit Reaction Settings")]
    [SerializeField] private float hitKnockbackForce = 15f;
    [SerializeField] private float flashDuration = 0.2f;
    
    [Header("References (Optional)")]
    [SerializeField] private EnemyHealthBar healthBar;

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;
    private EnemyDeathNotifier notifier;
    private EnemyDeathAnimation deathAnimation;
    private bool dead;

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        notifier = GetComponent<EnemyDeathNotifier>();
        deathAnimation = GetComponent<EnemyDeathAnimation>();
    }

    private void Start()
    {
        currentHealth = startingHealth;
        
        // Initialize healthbar if present
        if (healthBar != null)
        {
            healthBar.Initialize(transform, startingHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (dead) return;
        
        currentHealth -= damage;
        
        // Update healthbar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
        }
        
        // Play hit reactions (knockback + flash)
        PlayHitReaction();

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void PlayHitReaction()
    {
        // Knockback effect
        if (knockback && PlayerController.Instance != null)
        {
            knockback.GetKnockedBack(PlayerController.Instance.transform, hitKnockbackForce);
        }
        
        // Flash white effect
        if (flash)
        {
            StartCoroutine(flash.FlashRoutine());
        }
    }

    private void Die()
    {
        if (dead) return;
        dead = true;

        // Inform the DungeonManager
        notifier?.NotifyDied();

        // Play death animation if available
        if (deathAnimation != null)
        {
            deathAnimation.PlayDeathAnimation(PlayerController.Instance?.transform);
        }
        else
        {
            // Fallback: just destroy immediately
            Destroy(gameObject);
        }
    }

    public void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Public getters for other systems
    public int CurrentHealth => currentHealth;
    public int MaxHealth => startingHealth;
    public bool IsDead => dead;
}
