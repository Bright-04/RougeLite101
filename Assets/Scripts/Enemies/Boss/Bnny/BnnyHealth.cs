using UnityEngine;

[RequireComponent(typeof(EnemyDeathNotifier))]
public class BnnyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private string bossName = "Bnny";
    [SerializeField] private float maxHealth = 50f;
    public float expReward = 12;
    //[SerializeField] private EnemyHealthBar healthBar;

    private float currentHealth;
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
        currentHealth = maxHealth;

        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.Initialize(maxHealth, bossName);
        }
    }

    public void TakeDamage(int damage)
    {
        if (dead) return;

        currentHealth -= damage;

        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.UpdateHealthUI(damage);
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

        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.HideHealthBar();
        }

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
