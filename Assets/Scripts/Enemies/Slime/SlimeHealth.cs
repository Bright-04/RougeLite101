using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyDeathNotifier))]
public class SlimeHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 3;
    [SerializeField] private EnemyHealthBar healthBar;

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;
    private EnemyDeathNotifier notifier;
    private bool dead;

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        notifier = GetComponent<EnemyDeathNotifier>();
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

        Debug.Log($"{gameObject.name} taking {damage} damage. Current health: {currentHealth}");
        
        currentHealth -= damage;
        
        // Update healthbar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
        }
        
        if (knockback)
        {
            Debug.Log("Applying knockback!");
            knockback.GetKnockedBack(PlayerController.Instance.transform, 15f);
        }
        else
        {
            Debug.LogWarning($"No Knockback component on {gameObject.name}");
        }
        
        if (flash)
        {
            Debug.Log("Applying flash effect!");
            StartCoroutine(flash.FlashRoutine());
        }
        else
        {
            Debug.LogWarning($"No Flash component on {gameObject.name}");
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

        // Destroy the enemy
        Destroy(gameObject);
    }

    public void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            Die();
        }
    }

}
