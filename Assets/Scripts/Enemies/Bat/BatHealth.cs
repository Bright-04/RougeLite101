using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyDeathNotifier))]
public class BatHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int startingHealth = 2; // Lower health than slime (slime has 3)
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
        
        // Initialize healthbar
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
        
        if (knockback)
        {
            knockback.GetKnockedBack(PlayerMovement.Instance.transform, 18f); // Slightly more knockback than slime
        }
        
        if (flash)
        {
            StartCoroutine(flash.FlashRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (dead) return;
        dead = true;

        // Inform the DungeonManager
        notifier?.NotifyDied();

        // Play death animation if available, otherwise just destroy
        if (deathAnimation != null)
        {
            deathAnimation.PlayDeathAnimation(PlayerMovement.Instance.transform);
        }
        else
        {
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

}
