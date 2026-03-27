using UnityEngine;
using System.Collections;
using RougeLite.System;

[RequireComponent(typeof(EnemyDeathNotifier))]
public class SlimeHealth : MonoBehaviour, IDamageable
{
    public float expReward = 3;

    [SerializeField] private int startingHealth = 30;
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
        // Scale health based on adaptive difficulty
        if (DifficultyManager.Instance != null)
        {
            int baseHP = startingHealth;
            startingHealth = Mathf.RoundToInt(startingHealth * DifficultyManager.Instance.GetHealthMultiplier());
            Debug.Log($"<color=orange>[Adaptive AI]</color> {gameObject.name} HP: {baseHP} → {startingHealth} (x{DifficultyManager.Instance.GetHealthMultiplier():F2})");
        }

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
            knockback.GetKnockedBack(PlayerMovement.Instance.transform, 15f);
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

        ExpManager.Instance.GainExperience(expReward);

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
