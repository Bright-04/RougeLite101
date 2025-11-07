using UnityEngine;

/// <summary>
/// Debug script to help visualize sword collision issues
/// Attach this to the Player to see weapon collision debugging
/// </summary>
public class SwordDebugger : MonoBehaviour
{
    [Header("Debug Visualization")]
    public bool showWeaponCollider = true;
    public bool showDebugLogs = true;
    public Color colliderColor = Color.red;
    
    private Sword sword;
    private Transform weaponCollider;
    
    void Start()
    {
        // Find the sword in children
        sword = GetComponentInChildren<Sword>();
        if (sword != null)
        {
            // Access the weapon collider through reflection or public field
            weaponCollider = FindWeaponCollider();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"SwordDebugger: Sword found = {sword != null}");
            Debug.Log($"SwordDebugger: WeaponCollider found = {weaponCollider != null}");
        }
    }
    
    void Update()
    {
        if (weaponCollider != null && showDebugLogs)
        {
            // Log weapon collider state when it changes
            if (weaponCollider.gameObject.activeInHierarchy)
            {
                Debug.Log($"SwordDebugger: Weapon collider is ACTIVE at position {weaponCollider.position}");
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showWeaponCollider || weaponCollider == null) return;
        
        // Draw the weapon collider bounds
        if (weaponCollider.gameObject.activeInHierarchy)
        {
            Gizmos.color = colliderColor;
            
            // Get the polygon collider points if available
            PolygonCollider2D polyCollider = weaponCollider.GetComponent<PolygonCollider2D>();
            if (polyCollider != null)
            {
                Vector2[] points = polyCollider.points;
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 worldPoint1 = weaponCollider.TransformPoint(points[i]);
                    Vector3 worldPoint2 = weaponCollider.TransformPoint(points[(i + 1) % points.Length]);
                    Gizmos.DrawLine(worldPoint1, worldPoint2);
                }
            }
            else
            {
                // Fallback: draw a simple sphere at the collider position
                Gizmos.DrawWireSphere(weaponCollider.position, 0.5f);
            }
        }
    }
    
    private Transform FindWeaponCollider()
    {
        // Search for "Weapon Collider" in the sword's children
        if (sword != null)
        {
            Transform[] children = sword.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == "Weapon Collider")
                {
                    return child;
                }
            }
        }
        return null;
    }
}