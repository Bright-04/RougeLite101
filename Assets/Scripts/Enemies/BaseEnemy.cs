using UnityEngine;

/// <summary>
/// Base class for all enemy types. Provides common functionality.
/// </summary>
public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Base Enemy Stats")]
    [SerializeField] protected int startingHealth = 3;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float detectionRange = 10f;
    [SerializeField] protected int damageToPlayer = 1;

    protected int currentHealth;
    protected Transform playerTransform;
    protected Rigidbody2D rb;
    protected Knockback knockback;
    protected Flash flash;
    protected EnemyDeathNotifier notifier;
    protected bool dead = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockback = GetComponent<Knockback>();
        flash = GetComponent<Flash>();
        notifier = GetComponent<EnemyDeathNotifier>();
    }

    protected virtual void Start()
    {
        currentHealth = startingHealth;
        
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No GameObject tagged 'Player' found.");
        }
    }

    public virtual void TakeDamage(int damage)
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

    protected virtual void Die()
    {
        if (dead) return;
        dead = true;

        notifier?.NotifyDied();
        Destroy(gameObject);
    }

    protected bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector2.Distance(transform.position, playerTransform.position) < detectionRange;
    }

    protected Vector2 GetDirectionToPlayer()
    {
        if (playerTransform == null) return Vector2.zero;
        return (playerTransform.position - transform.position).normalized;
    }

    protected float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector2.Distance(transform.position, playerTransform.position);
    }
}
