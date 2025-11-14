using UnityEngine;

public class SkeletonPathFinding : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f; // Moderate speed - between Slime and Bat
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleDetectionDistance = 0.8f;
    [SerializeField] private float avoidanceForce = 3.5f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int rayCount = 7; // Number of rays to cast
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = false;

    private Rigidbody2D rb;
    private Knockback knockback;
    private Vector2 currentTargetPosition;
    private bool hasTarget = false;
    private Vector2 avoidanceDirection = Vector2.zero;
    private float currentMoveSpeed;

    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = moveSpeed;
        
        // Auto-detect obstacle layers if not set
        if (obstacleLayer == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default", "InvisibleWall");
        }
    }

    private void FixedUpdate()
    {
        if ((knockback != null && knockback.gettingKnockedBack) || !hasTarget)
            return;

        // Calculate desired direction toward target
        Vector2 desiredDirection = (currentTargetPosition - rb.position).normalized;
        
        // Check if we're currently stuck/overlapping an obstacle
        Vector2 unstuckDirection = CheckIfStuck();
        
        // Check for obstacles and calculate avoidance
        Vector2 avoidance = CalculateObstacleAvoidance(desiredDirection);
        
        // Combine all directions
        Vector2 finalDirection;
        if (unstuckDirection != Vector2.zero)
        {
            // Priority: getting unstuck first
            finalDirection = (unstuckDirection * 2f + avoidance).normalized;
        }
        else
        {
            // Normal movement with avoidance
            finalDirection = (desiredDirection + avoidance * 1.5f).normalized;
        }
        
        // Move in the final direction using current move speed
        Vector2 newPosition = rb.position + finalDirection * (currentMoveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }
    
    private Vector2 CheckIfStuck()
    {
        // Check if we're currently overlapping or very close to an obstacle
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(rb.position, 0.3f, obstacleLayer);
        
        if (overlaps.Length > 0)
        {
            // We're stuck! Calculate escape direction
            Vector2 escapeDirection = Vector2.zero;
            
            foreach (Collider2D obstacle in overlaps)
            {
                Vector2 awayFromObstacle = (rb.position - (Vector2)obstacle.transform.position).normalized;
                escapeDirection += awayFromObstacle;
            }
            
            return escapeDirection.normalized;
        }
        
        return Vector2.zero;
    }

    private Vector2 CalculateObstacleAvoidance(Vector2 desiredDirection)
    {
        Vector2 avoidance = Vector2.zero;
        Vector2 currentPos = rb.position;
        
        // Cast rays in a wider cone for better detection
        float spreadAngle = 90f; // Moderate spread angle
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
                
                // Calculate avoidance direction (perpendicular to obstacle)
                Vector2 awayFromObstacle = (currentPos - hit.point).normalized;
                float distance = Mathf.Max(hit.distance, 0.01f); // Prevent division by zero
                
                // Weight for distance (stronger for closer obstacles)
                float weight = Mathf.Pow(1f - (distance / obstacleDetectionDistance), 2f);
                weight = Mathf.Clamp(weight, 0.5f, 3f);
                
                avoidance += awayFromObstacle * weight * avoidanceForce;
                
                // Add perpendicular steering to go around obstacle
                Vector2 perpendicular = new Vector2(-rayDirection.y, rayDirection.x);
                avoidance += perpendicular * weight * avoidanceForce * 0.5f;
            }
        }
        
        // If multiple obstacles detected, increase avoidance strength
        if (obstaclesDetected > 0)
        {
            float obstacleMultiplier = 1f + (obstaclesDetected * 0.3f);
            avoidance *= obstacleMultiplier;
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
    }

    public void SetMoveSpeed(float speed)
    {
        currentMoveSpeed = speed;
    }

    public void ResetMoveSpeed()
    {
        currentMoveSpeed = moveSpeed;
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
