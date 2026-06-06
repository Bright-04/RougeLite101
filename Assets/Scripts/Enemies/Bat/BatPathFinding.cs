using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatPathFinding : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f; 
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleDetectionDistance = 1.0f; // Tăng lên 1.0 để phát hiện tường sớm hơn ở tốc độ cao
    [SerializeField] private float avoidanceForce = 5f; 
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int rayCount = 8; // Tăng số tia để quét kỹ hơn các góc nhọn
    
    [Header("Flying Behavior")]
    [SerializeField] private float hoverAmplitude = 0.15f; // How much it bobs up and down
    [SerializeField] private float hoverFrequency = 2f; // How fast it bobs
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = false;
    [SerializeField] private float navigationProbeRadius = 0.16f;

    private Rigidbody2D rb;
    private Collider2D selfCollider;
    private Knockback knockback;
    private DungeonNavigationProvider navigationProvider;
    private Vector2 currentTargetPosition;
    private bool hasTarget = false;
    private Vector2 avoidanceDirection = Vector2.zero;
    private float hoverOffset = 0f;
    private float baseMoveSpeed;
    private float currentMoveSpeed;
    private Collider2D[] ownColliders;
    
    // Charge mode
    private bool isCharging = false;
    private float chargeSpeedOverride = 0f;

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

        ownColliders = GetComponentsInChildren<Collider2D>();
        baseMoveSpeed = moveSpeed;
        currentMoveSpeed = baseMoveSpeed;

        // ENFORCE STABLE PHYSICS: Đảm bảo quái vật là object vật lý thực thụ
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f; // Top-down game
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Ngăn quái bị xoay tít khi va chạm
        }
        
        // Auto-detect obstacle layers
        if (obstacleLayer == 0)
        {
            obstacleLayer = LayerMask.GetMask("InvisibleWall", "Environment", "Obstacle");
        }
        else
        {
            obstacleLayer |= LayerMask.GetMask("InvisibleWall", "Environment", "Obstacle");
        }
        
        hoverOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void FixedUpdate()
    {
        if (rb == null || (knockback != null && knockback.gettingKnockedBack) || !hasTarget)
            return;

        if (navigationProvider == null)
        {
            navigationProvider = GetComponent<DungeonNavigationProvider>();
            if (navigationProvider == null)
            {
                navigationProvider = FindFirstObjectByType<DungeonNavigationProvider>();
            }
        }

        // Calculate desired direction toward target
        Vector2 desiredDirection = (currentTargetPosition - rb.position).normalized;
        
        // Check if we're currently stuck/overlapping an obstacle
        Vector2 unstuckDirection = CheckIfStuck();
        
        // Check for obstacles and calculate avoidance
        Vector2 avoidance = CalculateObstacleAvoidance(desiredDirection);
        
        // Add hovering motion (vertical bobbing)
        Vector2 hoverMotion = GetHoverMotion();
        
        // Combine all directions
        Vector2 finalDirection;
        float currentSpeed;
        
        if (isCharging)
        {
            // During charge: still check for walls but don't avoid them as much
            if (unstuckDirection != Vector2.zero)
            {
                // Stuck in wall, stop charge
                finalDirection = unstuckDirection;
                currentSpeed = currentMoveSpeed;
            }
            else
            {
                // Charging with minimal obstacle avoidance
                finalDirection = (desiredDirection + avoidance * 0.3f).normalized;
                currentSpeed = chargeSpeedOverride;
            }
        }
        else if (unstuckDirection != Vector2.zero)
        {
            // Priority: getting unstuck first
            finalDirection = (unstuckDirection * 2f + avoidance).normalized;
            currentSpeed = currentMoveSpeed;
        }
        else
        {
            // Normal movement with avoidance and hover
            finalDirection = (desiredDirection + avoidance * 1.8f + hoverMotion).normalized;
            currentSpeed = currentMoveSpeed;
        }
        
        Vector2 movement = finalDirection * (currentSpeed * Time.fixedDeltaTime);
        Vector2 nextPosition = GetWallBlockedPosition(movement);
        if (navigationProvider != null && !navigationProvider.IsWalkable(nextPosition, selfCollider, navigationProbeRadius))
        {
            StopMoving();
            return;
        }

        rb.MovePosition(nextPosition);
    }
    
    private Vector2 GetHoverMotion()
    {
        // Create a sine wave motion for hovering effect
        float hoverY = Mathf.Sin(Time.time * hoverFrequency + hoverOffset) * hoverAmplitude;
        return new Vector2(0f, hoverY);
    }
    
    private Vector2 CheckIfStuck()
    {
        // Kiểm tra xem ta có đang nằm đè lên tường không
        Collider2D obstacle = FindOverlappingObstacle(rb.position, 0.25f);
        
        if (obstacle != null)
        {
            // TÌM ĐIỂM THOÁT: Lấy điểm gần nhất trên bề mặt tường so với ta
            Vector2 closestPoint = obstacle.ClosestPoint(rb.position);
            
            // Nếu ta bị lún vào lòng tường, ClosestPoint sẽ trùng với rb.position
            // Ta cần đẩy ra ngoài theo hướng ngược lại với tâm của Collider đó (hoặc hướng hợp lý)
            if (Vector2.Distance(closestPoint, rb.position) < 0.01f)
            {
                // Push AWAY from the center of the obstacle if deep inside
                return (rb.position - (Vector2)obstacle.bounds.center).normalized;
            }
            
            // Hướng thoát = Từ điểm va chạm trên bề mặt đẩy ngược vào ta
            return (rb.position - closestPoint).normalized;
        }
        
        return Vector2.zero;
    }

    private Vector2 GetWallBlockedPosition(Vector2 movement)
    {
        if (movement.sqrMagnitude <= 0.000001f)
        {
            return rb.position;
        }

        const float castRadius = 0.28f;
        const float skinWidth = 0.03f;
        Vector2 direction = movement.normalized;
        float distance = movement.magnitude;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(rb.position, castRadius, direction, distance, obstacleLayer);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger || IsOwnCollider(hit.collider))
            {
                continue;
            }

            float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
            return rb.position + direction * safeDistance;
        }

        return rb.position + movement;
    }

    private Collider2D FindOverlappingObstacle(Vector2 position, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, obstacleLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit != null && !hit.isTrigger && !IsOwnCollider(hit))
            {
                return hit;
            }
        }

        return null;
    }

    private bool IsOwnCollider(Collider2D candidate)
    {
        if (candidate == null || ownColliders == null)
        {
            return false;
        }

        foreach (Collider2D ownCollider in ownColliders)
        {
            if (candidate == ownCollider)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 CalculateObstacleAvoidance(Vector2 desiredDirection)
    {
        Vector2 avoidance = Vector2.zero;
        Vector2 currentPos = rb.position;
        
        // Cast rays in a cone for obstacle detection
        float spreadAngle = 80f; // Narrower spread for agile movement
        float angleStep = spreadAngle / (rayCount - 1);
        float startAngle = -spreadAngle / 2f;
        
        // Get the angle of desired direction
        float desiredAngle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;
        
        int obstaclesDetected = 0;
        
        for (int i = 0; i < rayCount; i++)
        {
            // Calculate ray angle
            float currentAngle = desiredAngle + startAngle + (angleStep * i);
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 rayDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            
            // Cast ray
            RaycastHit2D hit = Physics2D.Raycast(currentPos, rayDirection, obstacleDetectionDistance, obstacleLayer);
            
            if (showDebugRays)
            {
                Color rayColor = hit.collider != null ? Color.red : Color.green;
                Debug.DrawRay(currentPos, rayDirection * obstacleDetectionDistance, rayColor);
            }
            
            if (hit.collider != null)
            {
                obstaclesDetected++;
                
                // Calculate avoidance direction
                Vector2 awayFromObstacle = (currentPos - hit.point).normalized;
                float distance = Mathf.Max(hit.distance, 0.01f);
                
                // Weight for distance (stronger for closer obstacles)
                float weight = Mathf.Pow(1f - (distance / obstacleDetectionDistance), 2f);
                weight = Mathf.Clamp(weight, 0.6f, 3.5f);
                
                avoidance += awayFromObstacle * weight * avoidanceForce;
                
                // Add perpendicular steering for smoother evasion
                Vector2 perpendicular = new Vector2(-rayDirection.y, rayDirection.x);
                avoidance += perpendicular * weight * avoidanceForce * 0.6f;
            }
        }
        
        // If multiple obstacles detected, increase avoidance strength
        if (obstaclesDetected > 0)
        {
            float obstacleMultiplier = 1f + (obstaclesDetected * 0.4f);
            avoidance *= obstacleMultiplier;
        }
        
        return avoidance;
    }

    public void MoveTo(Vector2 targetPosition)
    {
        currentTargetPosition = targetPosition;
        hasTarget = true;
    }

    public void SetMoveSpeed(float speed)
    {
        currentMoveSpeed = Mathf.Max(0f, speed);
    }

    public void StopMoving()
    {
        hasTarget = false;
    }
    
    public void SetChargeMode(bool charging, float chargeSpeed)
    {
        isCharging = charging;
        chargeSpeedOverride = chargeSpeed;
    }
    
    // Draw gizmos to visualize detection range
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || rb == null) return;
        
        // Draw target position
        if (hasTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTargetPosition, 0.2f);
            Gizmos.DrawLine(transform.position, currentTargetPosition);
        }
        
        // Draw detection radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, obstacleDetectionDistance);
    }
}
