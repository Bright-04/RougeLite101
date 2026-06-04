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
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showDebugRays = false;
    [SerializeField] private float navigationProbeRadius = 0.16f;

    private Rigidbody2D rb;
    private Collider2D selfCollider;
    private Knockback knockback;
    private DungeonNavigationProvider navigationProvider;
    private Vector2 currentTargetPosition;
    private bool hasTarget;
    private Vector2 currentMoveDirection;
    private float currentMoveSpeed;
    private float speedMultiplier = 1f;
    private bool wasBlockedByNavigation;
    private string lastStopReason;
    private float nextMovementDebugLogTime;
    private float nextOpposingMoveLogTime;

    public bool HasTarget => hasTarget;
    public Vector2 CurrentTargetPosition => currentTargetPosition;
    public Vector2 CurrentMoveDirection => currentMoveDirection;
    public float CurrentMoveSpeed => currentMoveSpeed;
    public float BaseMoveSpeed => moveSpeed;
    public float SpeedMultiplier => speedMultiplier;
    public bool WasBlockedByNavigation => wasBlockedByNavigation;
    public bool IsMoving => currentMoveSpeed > 0.01f && currentMoveDirection.sqrMagnitude > 0.0001f;

    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        rb = GetComponent<Rigidbody2D>();
        selfCollider = GetComponent<Collider2D>();
        navigationProvider = GetComponent<DungeonNavigationProvider>();
        if (navigationProvider == null)
        {
            navigationProvider = FindFirstObjectByType<DungeonNavigationProvider>();
        }

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
            obstacleLayer = LayerMask.GetMask("InvisibleWall", "Obstacle", "Environment");
        }

        LogObstacleMask();
    }

    private void FixedUpdate()
    {
        currentMoveDirection = Vector2.zero;
        currentMoveSpeed = 0f;
        wasBlockedByNavigation = false;
        if (navigationProvider == null)
        {
            navigationProvider = GetComponent<DungeonNavigationProvider>();
            if (navigationProvider == null)
            {
                navigationProvider = FindFirstObjectByType<DungeonNavigationProvider>();
            }
        }

        if (rb == null || (knockback != null && knockback.gettingKnockedBack) || !hasTarget)
        {
            return;
        }

        Vector2 toTarget = currentTargetPosition - rb.position;
        float distanceToTarget = toTarget.magnitude;
        if (distanceToTarget <= arrivalDistance)
        {
            StopMoving("ReachedTarget");
            return;
        }

        Vector2 desiredDirection = toTarget / Mathf.Max(distanceToTarget, 0.0001f);
        Vector2 unstuckDirection = CheckIfStuck();
        Vector2 avoidance = CalculateObstacleAvoidance(desiredDirection);

        Vector2 adjustedAvoidance = AdjustAvoidance(desiredDirection, avoidance);
        Vector2 finalDirection;
        if (unstuckDirection != Vector2.zero)
        {
            finalDirection = (desiredDirection + unstuckDirection * 0.6f + adjustedAvoidance * 0.25f).normalized;
        }
        else
        {
            finalDirection = (desiredDirection + adjustedAvoidance * 0.4f).normalized;
        }

        float directionDot = Vector2.Dot(finalDirection, desiredDirection);
        if (directionDot < 0.2f)
        {
            LogOpposingMove(desiredDirection, adjustedAvoidance, finalDirection, directionDot);
            finalDirection = desiredDirection;
            directionDot = 1f;
        }

        currentMoveDirection = finalDirection;
        currentMoveSpeed = moveSpeed * Mathf.Max(0f, speedMultiplier);

        Vector2 newPosition = rb.position + finalDirection * (currentMoveSpeed * Time.fixedDeltaTime);
        float movedDistance = Vector2.Distance(rb.position, newPosition);
        if (navigationProvider != null && !navigationProvider.IsWalkable(newPosition, selfCollider, navigationProbeRadius))
        {
            wasBlockedByNavigation = true;
            if (enableDebugLogs)
            {
                Debug.Log($"[SlimePathFinding] {name} nav blocked nextPos={newPosition:F2} target={currentTargetPosition:F2}", this);
            }
            StopMoving("NavigationBlocked");
            return;
        }

        LogMovementDiagnostic(desiredDirection, adjustedAvoidance, finalDirection, directionDot, movedDistance);
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

    private Vector2 AdjustAvoidance(Vector2 desiredDirection, Vector2 avoidance)
    {
        if (avoidance.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        Vector2 avoidanceDirection = avoidance.normalized;
        float oppositeDot = Vector2.Dot(avoidanceDirection, desiredDirection);
        if (oppositeDot < -0.25f)
        {
            avoidance *= 0.2f;
        }

        return Vector2.ClampMagnitude(avoidance, avoidanceForce);
    }

    public void MoveTo(Vector2 targetPosition)
    {
        currentTargetPosition = targetPosition;
        hasTarget = true;
        if (enableDebugLogs)
        {
            Debug.Log($"[SlimePathFinding] {name} MoveTo target={targetPosition:F2}", this);
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void StopMoving(string reason = null)
    {
        if (enableDebugLogs && hasTarget && !string.Equals(lastStopReason, reason))
        {
            Debug.Log($"[SlimePathFinding] {name} StopMoving reason={reason ?? "None"} target={currentTargetPosition:F2}", this);
        }

        hasTarget = false;
        currentMoveDirection = Vector2.zero;
        currentMoveSpeed = 0f;
        lastStopReason = reason;
    }

    private void LogObstacleMask()
    {
        if (!enableDebugLogs && !showDebugRays)
        {
            return;
        }

        Debug.Log($"[SlimePathFinding] {name} obstacleLayer={DescribeMask(obstacleLayer)}", this);
    }

    private void LogMovementDiagnostic(Vector2 desiredDirection, Vector2 avoidance, Vector2 finalDirection, float directionDot, float movedDistance)
    {
        if (!enableDebugLogs || Time.time < nextMovementDebugLogTime)
        {
            return;
        }

        nextMovementDebugLogTime = Time.time + 0.5f;
        Debug.Log(
            $"[SlimePathFinding] {name} move target={currentTargetPosition:F2} desired={desiredDirection:F2} avoidMag={avoidance.magnitude:F2} final={finalDirection:F2} dot={directionDot:F2} moved={movedDistance:F3} navBlocked={wasBlockedByNavigation}",
            this);
    }

    private void LogOpposingMove(Vector2 desiredDirection, Vector2 avoidance, Vector2 finalDirection, float directionDot)
    {
        if (!enableDebugLogs || Time.time < nextOpposingMoveLogTime)
        {
            return;
        }

        nextOpposingMoveLogTime = Time.time + 1f;
        Debug.LogWarning(
            $"[SlimePathFinding] {name} opposing move desired={desiredDirection:F2} avoidMag={avoidance.magnitude:F2} final={finalDirection:F2} dot={directionDot:F2} target={currentTargetPosition:F2}",
            this);
    }

    private static string DescribeMask(LayerMask mask)
    {
        if (mask.value == 0)
        {
            return "None";
        }

        string names = string.Empty;
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) == 0)
            {
                continue;
            }

            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName))
            {
                layerName = i.ToString();
            }

            names = string.IsNullOrEmpty(names) ? layerName : $"{names}|{layerName}";
        }

        return names;
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
