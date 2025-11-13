using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    [SerializeField] private int baseDamage = 1;
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color gizmoColor = Color.red;

    private void OnEnable()
    {
        // Damage source activated
    }

    private void OnDisable()
    {
        // Damage source deactivated
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent(out SlimeHealth slimeHealth))
        {
            float finalDamage = baseDamage;

            // Get the player's stats for AD and Crit
            PlayerStats stats = PlayerController.Instance.GetComponent<PlayerStats>();
            if (stats != null)
            {
                finalDamage += stats.attackDamage;

                if (stats.TryCrit())
                {
                    finalDamage *= stats.GetCritMultiplier();
                }
            }

            slimeHealth.TakeDamage(Mathf.RoundToInt(finalDamage));
        }
    }

    // Visualize the collider in Scene view
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        // Different color when enabled vs disabled
        Gizmos.color = enabled && gameObject.activeInHierarchy ? gizmoColor : new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        
        if (col is PolygonCollider2D polyCol)
        {
            // Draw the polygon
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector2[] points = polyCol.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[(i + 1) % points.Length];
                Gizmos.DrawLine(start, end);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (col is BoxCollider2D boxCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCol.offset, boxCol.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (col is CircleCollider2D circleCol)
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
        }

        // Draw center point
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
}
