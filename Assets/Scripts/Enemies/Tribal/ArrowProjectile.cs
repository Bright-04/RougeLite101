using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 1f;

    [Header("Direction Points")]
    [SerializeField] private Transform arrowHead;
    [SerializeField] private Transform arrowTail;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction, float speed)
    {
        if (rb == null)
        {
            Debug.LogError("Arrow missing Rigidbody2D", this);
            return;
        }

        direction.Normalize();

        RotateArrowToDirection(direction);

        rb.linearVelocity = direction * speed;
    }

    private void RotateArrowToDirection(Vector2 targetDirection)
    {
        if (arrowHead == null || arrowTail == null)
            return;

        Vector2 currentArrowDirection =
            arrowHead.position - arrowTail.position;

        float angle = Vector2.SignedAngle(
            currentArrowDirection,
            targetDirection
        );

        transform.Rotate(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStats playerStats = other.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}