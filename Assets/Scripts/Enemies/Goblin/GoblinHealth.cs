using UnityEngine;

/// <summary>
/// Goblin Health: Similar to slime but with different default health
/// </summary>
[RequireComponent(typeof(EnemyDeathNotifier))]
public class GoblinHealth : MonoBehaviour, IEnemy
{
    [SerializeField] private int startingHealth = 4;

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
        if (knockback) knockback.GetKnockedBack(PlayerController.Instance.transform, 15f);
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
