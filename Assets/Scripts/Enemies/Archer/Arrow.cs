using UnityEngine;

/// <summary>
/// Arrow projectile fired by Archer enemies
/// </summary>
public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 2;

    private Rigidbody2D rb;
    private Vector2 direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;
        rb.linearVelocity = direction * speed;

        // Rotate arrow to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Damage player
        if (other.TryGetComponent(out PlayerStats playerStats))
        {
            playerStats.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destroy on collision with walls
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall") || 
            other.CompareTag("Wall") || 
            other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
}
