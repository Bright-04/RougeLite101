using UnityEngine;

/// <summary>
/// Bat Health: Low health, fast flying enemy
/// </summary>
[RequireComponent(typeof(EnemyDeathNotifier))]
public class BatHealth : MonoBehaviour, IEnemy
{
    [SerializeField] private int startingHealth = 2;

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
    }

    public void TakeDamage(int damage)
    {
        if (dead) return;

        currentHealth -= damage;
        if (knockback) knockback.GetKnockedBack(PlayerController.Instance.transform, 12f);
        if (flash) StartCoroutine(flash.FlashRoutine());

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
        Destroy(gameObject);
    }
}
