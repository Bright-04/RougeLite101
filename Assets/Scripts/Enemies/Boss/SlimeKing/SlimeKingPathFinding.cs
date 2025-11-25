using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeKingPathFinding : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 0.5f; // how close before he stops

    [Header("Debug")]
    [SerializeField] private bool showTargetLine = false;

    private Rigidbody2D rb;
    private Transform target;        // usually the player
    private bool canMove = false;

    private Knockback knockback;     // optional, if you have it

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockback = GetComponent<Knockback>();
    }

    private void FixedUpdate()
    {
        if (!canMove || target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // if getting knocked back, don't override physics
        if (knockback != null && knockback.gettingKnockedBack)
            return;

        Vector2 toTarget = (target.position - transform.position);
        float distance = toTarget.magnitude;

        if (distance <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = toTarget.normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>Enable or disable movement completely (used by AI when attacking).</summary>
    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!canMove && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showTargetLine && target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
