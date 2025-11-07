using UnityEngine;

/// <summary>
/// Orc Health: Very high health tank enemy
/// </summary>
[RequireComponent(typeof(EnemyDeathNotifier))]
public class OrcHealth : MonoBehaviour, IEnemy
{
    [SerializeField] private int startingHealth = 8;

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
        if (knockback) knockback.GetKnockedBack(PlayerController.Instance.transform, 8f); // Less knockback (heavy)
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
