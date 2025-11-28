using UnityEngine;

[RequireComponent(typeof(EnemyDeathNotifier))]
public class BnnyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int startingHealth = 50;
    [SerializeField] private EnemyHealthBar healthBar;

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;
    private EnemyDeathNotifier notifier;
    private EnemyDeathAnimation deathAnimation; // optional
    private bool dead;

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        notifier = GetComponent<EnemyDeathNotifier>();
        deathAnimation = GetComponent<EnemyDeathAnimation>(); // can be null
    }

    private void Start()
    {
        currentHealth = startingHealth;

        if (healthBar != null)
        {
            healthBar.Initialize(transform, startingHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (dead) return;

        currentHealth -= damage;

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

        notifier?.NotifyDied();

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
