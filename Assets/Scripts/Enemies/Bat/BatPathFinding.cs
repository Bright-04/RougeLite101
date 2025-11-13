using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatPathFinding : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f; // Faster than slime (slime is 2f)
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleDetectionDistance = 0.6f; // Slightly shorter since bats are more agile
    [SerializeField] private float avoidanceForce = 4f; // Higher avoidance for quick reactions
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int rayCount = 5; // Fewer rays needed for agile movement
    
    [Header("Flying Behavior")]
    [SerializeField] private float hoverAmplitude = 0.15f; // How much it bobs up and down
    [SerializeField] private float hoverFrequency = 2f; // How fast it bobs
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = false;

    private Rigidbody2D rb;
    private Knockback knockback;
    private Vector2 currentTargetPosition;
    private bool hasTarget = false;
    private Vector2 avoidanceDirection = Vector2.zero;
    private float hoverOffset = 0f;
    
    // Charge mode
    private bool isCharging = false;
    private float chargeSpeedOverride = 0f;

    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        rb = GetComponent<Rigidbody2D>();
        
        // Auto-detect obstacle layers if not set
        if (obstacleLayer == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default", "InvisibleWall");
        }
        
        // Random starting hover offset for variety
        hoverOffset = Random.Range(0f, Mathf.PI * 2f);
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
                currentSpeed = moveSpeed;
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
            currentSpeed = moveSpeed;
        }
        else
        {
            // Normal movement with avoidance and hover
            finalDirection = (desiredDirection + avoidance * 1.8f + hoverMotion).normalized;
            currentSpeed = moveSpeed;
        }
        
        // Move in the final direction
        Vector2 newPosition = rb.position + finalDirection * (currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }
    
    private Vector2 GetHoverMotion()
    {
        // Create a sine wave motion for hovering effect
        float hoverY = Mathf.Sin(Time.time * hoverFrequency + hoverOffset) * hoverAmplitude;
        return new Vector2(0f, hoverY);
    }
    
    private Vector2 CheckIfStuck()
    {
        // Check if we're currently overlapping or very close to an obstacle
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(rb.position, 0.25f, obstacleLayer);
        
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
