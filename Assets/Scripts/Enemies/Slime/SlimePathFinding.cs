using UnityEngine;

public class SlimePathFinding : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arrivalDistance = 0.08f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleDetectionDistance = 0.8f;
    [SerializeField] private float avoidanceForce = 3f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int rayCount = 7;

    [Header("Debug")]
    [SerializeField] private bool showDebugRays = false;

    private Rigidbody2D rb;
    private Knockback knockback;
    private Vector2 currentTargetPosition;
    private bool hasTarget;
    private Vector2 currentMoveDirection;
    private float currentMoveSpeed;

    public bool HasTarget => hasTarget;
    public Vector2 CurrentTargetPosition => currentTargetPosition;
    public Vector2 CurrentMoveDirection => currentMoveDirection;
    public float CurrentMoveSpeed => currentMoveSpeed;
    public bool IsMoving => currentMoveSpeed > 0.01f && currentMoveDirection.sqrMagnitude > 0.0001f;

    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (obstacleLayer == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default", "Environment", "Obstacle");
        }
        else
        {
            obstacleLayer |= LayerMask.GetMask("Default", "Environment", "Obstacle");
        }
    }

    private void FixedUpdate()
    {
        currentMoveDirection = Vector2.zero;
        currentMoveSpeed = 0f;

        if (rb == null || (knockback != null && knockback.gettingKnockedBack) || !hasTarget)
        {
            return;
        }

        Vector2 toTarget = currentTargetPosition - rb.position;
        float distanceToTarget = toTarget.magnitude;
        if (distanceToTarget <= arrivalDistance)
        {
            StopMoving();
            return;
        }

        Vector2 desiredDirection = toTarget / Mathf.Max(distanceToTarget, 0.0001f);
        Vector2 unstuckDirection = CheckIfStuck();
        Vector2 avoidance = CalculateObstacleAvoidance(desiredDirection);

        Vector2 finalDirection;
        if (unstuckDirection != Vector2.zero)
        {
            finalDirection = (unstuckDirection * 2f + avoidance).normalized;
        }
        else
        {
            finalDirection = (desiredDirection + avoidance * 1.5f).normalized;
        }

        currentMoveDirection = finalDirection;
        currentMoveSpeed = moveSpeed;

        Vector2 newPosition = rb.position + finalDirection * (moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }

    private Vector2 CheckIfStuck()
    {
        Collider2D obstacle = Physics2D.OverlapCircle(rb.position, 0.3f, obstacleLayer);
        if (obstacle == null)
        {
            return Vector2.zero;
        }

        Vector2 closestPoint = obstacle.ClosestPoint(rb.position);
        if (Vector2.Distance(closestPoint, rb.position) < 0.01f)
        {
            return (rb.position - (Vector2)obstacle.bounds.center).normalized;
        }

        return (rb.position - closestPoint).normalized;
    }

    private Vector2 CalculateObstacleAvoidance(Vector2 desiredDirection)
    {
        Vector2 avoidance = Vector2.zero;
        Vector2 currentPos = rb.position;
        float spreadAngle = 90f;
        float angleStep = spreadAngle / Mathf.Max(rayCount - 1, 1);
        float startAngle = -spreadAngle / 2f;
        float desiredAngle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;
        int obstaclesDetected = 0;

        for (int i = 0; i < rayCount; i++)
        {
            float currentAngle = desiredAngle + startAngle + (angleStep * i);
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 rayDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            RaycastHit2D hit = Physics2D.Raycast(currentPos, rayDirection, obstacleDetectionDistance, obstacleLayer);

            if (showDebugRays)
            {
                Debug.DrawRay(currentPos, rayDirection * obstacleDetectionDistance, hit.collider != null ? Color.red : Color.green);
            }

            if (hit.collider == null)
            {
                continue;
            }

            obstaclesDetected++;
            Vector2 awayFromObstacle = (currentPos - hit.point).normalized;
            float distance = Mathf.Max(hit.distance, 0.01f);
            float weight = Mathf.Pow(1f - (distance / obstacleDetectionDistance), 2f);
            weight = Mathf.Clamp(weight, 0.5f, 3f);

            avoidance += awayFromObstacle * weight * avoidanceForce;
            Vector2 perpendicular = new Vector2(-rayDirection.y, rayDirection.x);
            avoidance += perpendicular * weight * avoidanceForce * 0.5f;
        }

        if (obstaclesDetected > 0)
        {
            avoidance *= 1f + (obstaclesDetected * 0.3f);
        }

        return avoidance;
    }

    public void MoveTo(Vector2 targetPosition)
    {
        currentTargetPosition = targetPosition;
        hasTarget = true;
    }

    public void StopMoving()
    {
        hasTarget = false;
        currentMoveDirection = Vector2.zero;
        currentMoveSpeed = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || rb == null)
        {
            return;
        }

        if (hasTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTargetPosition, 0.2f);
            Gizmos.DrawLine(transform.position, currentTargetPosition);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, obstacleDetectionDistance);
    }
}
