using UnityEngine;

[RequireComponent(typeof(EnemyDeathNotifier))]
[RequireComponent(typeof(EnemyDeathAnimation))]
public class SlimeKingHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 200f;
    private float currentHealth;

    private EnemyDeathNotifier deathNotifier;
    private EnemyDeathAnimation deathAnimation;

    private Knockback knockback;
    private Flash flash;

    private bool isDead;

    // NEW ------------------------
    private bool isInvulnerable = false;
    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }
    // ----------------------------

    private void Awake()
    {
        deathNotifier = GetComponent<EnemyDeathNotifier>();
        deathAnimation = GetComponent<EnemyDeathAnimation>();
        knockback = GetComponent<Knockback>();
        flash = GetComponent<Flash>();

        currentHealth = maxHealth;
    }

    // IDamageable compatible
    public void TakeDamage(int damage)
    {
        TakeDamageInternal(damage, PlayerMovement.Instance.transform);
    }

    // Optional extended version with damageSource
    public void TakeDamage(float amount, Transform damageSource)
    {
        TakeDamageInternal(amount, damageSource);
    }

    private void TakeDamageInternal(float amount, Transform source)
    {
        if (isDead) return;

        // NEW: Ignore damage while invulnerable
        if (isInvulnerable)
            return;

        currentHealth -= amount;

        // effects
        if (flash != null)
            StartCoroutine(flash.FlashRoutine());

        if (knockback != null)
            knockback.GetKnockedBack(source, 12f);

        if (currentHealth <= 0f)
            Die(source);
    }

    private void CleanupSlimeKingProjectiles()
    {
        var bullets = FindObjectsByType<SlimeKingProjectile>(FindObjectsSortMode.None);
        foreach (var b in bullets)
        {
            Destroy(b.gameObject);
        }
    }

    private void Die(Transform damageSource)
    {
        if (isDead) return;
        isDead = true;

        deathNotifier.NotifyDied();
        deathAnimation.PlayDeathAnimation(damageSource);

        CleanupSlimeKingProjectiles();

    }
}
