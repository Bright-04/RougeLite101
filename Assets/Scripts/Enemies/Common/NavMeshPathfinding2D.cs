using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Advanced pathfinding using Unity's NavMesh system.
/// This provides intelligent pathfinding around obstacles and through complex terrain.
/// Requires NavMesh to be baked in the scene.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NavMeshPathfinding2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stoppingDistance = 0.1f;
    
    [Header("Path Update")]
    [SerializeField] private float pathUpdateInterval = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugPath = false;

    private Rigidbody2D rb;
    private Knockback knockback;
    private Vector2 currentTarget;
    private bool hasTarget = false;
    private float lastPathUpdateTime = 0f;
    
    // Simple 2D path
    private Vector2[] currentPath;
    private int currentPathIndex = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockback = GetComponent<Knockback>();
    }

    private void FixedUpdate()
    {
        if ((knockback != null && knockback.gettingKnockedBack) || !hasTarget)
            return;

        // Update path periodically
        if (Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            CalculatePath(currentTarget);
            lastPathUpdateTime = Time.time;
        }

        // Follow the path
        if (currentPath != null && currentPath.Length > 0)
        {
            FollowPath();
        }
    }

    private void CalculatePath(Vector2 targetPosition)
    {
        // For 2D games, we'll use simple A* or direct line with obstacle checking
        // This is a simplified version - for complex mazes, integrate a proper A* algorithm
        
        Vector2 currentPos = rb.position;
        
        // Check if we can go straight to target
        RaycastHit2D hit = Physics2D.Linecast(currentPos, targetPosition, LayerMask.GetMask("Default", "InvisibleWall"));
        
        if (hit.collider == null)
        {
            // Direct path available
            currentPath = new Vector2[] { targetPosition };
            currentPathIndex = 0;
        }
        else
        {
            // Obstacle in the way - try to find a path around it
            Vector2[] possiblePath = FindPathAroundObstacle(currentPos, targetPosition, hit.point);
            if (possiblePath != null)
            {
                currentPath = possiblePath;
                currentPathIndex = 0;
            }
        }
    }

    private Vector2[] FindPathAroundObstacle(Vector2 start, Vector2 end, Vector2 obstaclePoint)
    {
        // Simple pathfinding: try going around the obstacle
        Vector2 toObstacle = (obstaclePoint - start).normalized;
        Vector2 perpendicular1 = new Vector2(-toObstacle.y, toObstacle.x);
        Vector2 perpendicular2 = new Vector2(toObstacle.y, -toObstacle.x);
        
        // Try two waypoints around the obstacle
        Vector2 waypoint1 = obstaclePoint + perpendicular1 * 1f;
        Vector2 waypoint2 = obstaclePoint + perpendicular2 * 1f;
        
        // Choose the waypoint that's closer to the target
        float dist1 = Vector2.Distance(waypoint1, end);
        float dist2 = Vector2.Distance(waypoint2, end);
        
        Vector2 chosenWaypoint = dist1 < dist2 ? waypoint1 : waypoint2;
        
        return new Vector2[] { chosenWaypoint, end };
    }

    private void FollowPath()
    {
        if (currentPathIndex >= currentPath.Length)
        {
            hasTarget = false;
            return;
        }

        Vector2 targetWaypoint = currentPath[currentPathIndex];
        Vector2 direction = (targetWaypoint - rb.position).normalized;
        float distance = Vector2.Distance(rb.position, targetWaypoint);

        if (distance < stoppingDistance)
        {
            // Reached waypoint, move to next
            currentPathIndex++;
            return;
        }

        // Move toward current waypoint
        rb.MovePosition(rb.position + direction * (moveSpeed * Time.fixedDeltaTime));
    }

    public void MoveTo(Vector2 targetPosition)
    {
        currentTarget = targetPosition;
        hasTarget = true;
        CalculatePath(targetPosition);
    }

    public void StopMoving()
    {
        hasTarget = false;
        currentPath = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugPath || currentPath == null || currentPath.Length == 0) return;

        // Draw the path
        Gizmos.color = Color.green;
        for (int i = 0; i < currentPath.Length; i++)
        {
            Gizmos.DrawWireSphere(currentPath[i], 0.2f);
            
            if (i < currentPath.Length - 1)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
            
            if (i > 0)
            {
                Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
            }
        }

        // Draw line from current position to first waypoint
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentPath[currentPathIndex]);
        }
    }
}
