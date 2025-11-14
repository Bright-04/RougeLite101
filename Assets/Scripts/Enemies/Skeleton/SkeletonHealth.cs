using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyDeathNotifier))]
public class SkeletonHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int startingHealth = 4; // More durable than Slime (3) and Bat (2)
    [SerializeField] private EnemyHealthBar healthBar;
    [SerializeField] private GameObject blockSparkVFXPrefab; // Optional VFX for successful block

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;
    private EnemyDeathNotifier notifier;
    private EnemyDeathAnimation deathAnimation;
    private SkeletonAI skeletonAI;
    private Animator animator;
    private bool dead;

    // Animation parameter for taking damage
    private int takeDamageParam;

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        notifier = GetComponent<EnemyDeathNotifier>();
        deathAnimation = GetComponent<EnemyDeathAnimation>();
        skeletonAI = GetComponent<SkeletonAI>();
        animator = GetComponent<Animator>();
        
        // Initialize animator parameter in Awake - find the hurt/damage parameter
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    if (param.name.ToLower().Contains("hurt") || param.name.ToLower().Contains("damage"))
                    {
                        takeDamageParam = Animator.StringToHash(param.name);
                        Debug.Log($"SkeletonHealth: Using '{param.name}' for hurt trigger");
                        break;
                    }
                }
            }
            
            if (takeDamageParam == 0)
            {
                Debug.LogWarning("SkeletonHealth: No 'hurt' trigger parameter found in animator!", this);
            }
        }
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

        // Check if skeleton is blocking
        float damageMultiplier = 1f;
        bool wasBlocking = false;

        if (skeletonAI != null && skeletonAI.IsBlocking())
        {
            damageMultiplier = skeletonAI.GetBlockDamageReduction();
            wasBlocking = true;

            // Spawn block VFX if damage was reduced
            if (damageMultiplier < 1f && blockSparkVFXPrefab != null)
            {
                Instantiate(blockSparkVFXPrefab, transform.position, Quaternion.identity);
            }

            Debug.Log($"Skeleton blocked attack! Damage reduced by {(1f - damageMultiplier) * 100f}%");
        }

        // Calculate final damage
        int finalDamage = Mathf.CeilToInt(damage * damageMultiplier);
        currentHealth -= finalDamage;
        
        // Update healthbar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
        }

        // Trigger damage animation (only if not blocking or block was partial)
        if (animator != null && takeDamageParam != 0 && (!wasBlocking || damageMultiplier > 0.5f))
        {
            animator.SetTrigger(takeDamageParam);
        }

        // Apply knockback (reduced if blocking)
        if (knockback && PlayerController.Instance != null)
        {
            float knockbackForce = wasBlocking ? 8f : 15f; // Reduced knockback when blocking
            knockback.GetKnockedBack(PlayerController.Instance.transform, knockbackForce);
        }
        
        // Flash effect (reduced if blocking)
        if (flash && (!wasBlocking || damageMultiplier > 0.5f))
        {
            StartCoroutine(flash.FlashRoutine());
        }

        // Cancel attack if hit during attack (skill-based mechanic)
        if (skeletonAI != null && !wasBlocking)
        {
            skeletonAI.CancelAttack();
        }

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (dead) return;
        dead = true;

        // Stop AI immediately to prevent color glitches
        if (skeletonAI != null)
        {
            skeletonAI.StopAI();
        }

        // Inform the DungeonManager
        notifier?.NotifyDied();

        // Play death animation if available, otherwise just destroy
        if (deathAnimation != null)
        {
            deathAnimation.PlayDeathAnimation(PlayerController.Instance?.transform);
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
