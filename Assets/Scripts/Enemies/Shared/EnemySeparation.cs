using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Prevents enemies from hard-overlapping by applying a lightweight separation force
/// when other enemies are within a minimum radius. Works alongside normal pathfinding.
/// Attach this to enemy prefabs that have a Rigidbody2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemySeparation : MonoBehaviour
{
    [Header("Separation Settings")]
    [SerializeField] private float separationRadius = 0.5f; // Distance to start pushing apart
    [SerializeField] private float pushForce = 3f;           // Force applied per close neighbor
    [SerializeField] private float maxTotalForce = 6f;       // Clamp total separation force
    [SerializeField] private LayerMask enemyLayer;           // Layer enemies reside on
    [SerializeField] private float sampleInterval = 0.15f;   // How often to recalc (seconds)
    [SerializeField] private bool usePhysicsForce = false;   // If true, AddForce instead of position interpolation

    [Header("Performance / Debug")]
    [SerializeField] private bool showGizmos = false;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.25f);

    private Rigidbody2D rb;
    private float nextSampleTime;
    private Vector2 cachedSeparation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (enemyLayer == 0)
        {
            // Attempt auto-detect: look for a layer named "Enemy" else default to everything
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            enemyLayer = enemyLayerIndex >= 0 ? (1 << enemyLayerIndex) : ~0; // fallback all layers
        }
    }

    private void FixedUpdate()
    {
        if (Time.time >= nextSampleTime)
        {
            cachedSeparation = ComputeSeparationVector();
            nextSampleTime = Time.time + sampleInterval;
        }

        if (cachedSeparation == Vector2.zero)
            return;

        if (usePhysicsForce)
        {
            rb.AddForce(cachedSeparation, ForceMode2D.Force);
        }
        else
        {
            // Position-based mild adjustment (keeps existing pathfinding direction intact)
            rb.MovePosition(rb.position + cachedSeparation * Time.fixedDeltaTime);
        }
    }

    private Vector2 ComputeSeparationVector()
    {
        // Collect nearby colliders in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, separationRadius, enemyLayer);
        if (hits == null || hits.Length <= 1) // Only self or none
            return Vector2.zero;

        Vector2 force = Vector2.zero;
        int count = 0;
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody == rb) continue; // skip self

            Vector2 toSelf = rb.position - (Vector2)hit.transform.position;
            float dist = toSelf.magnitude;
            if (dist < 0.0001f) continue;

            // Weight stronger when closer; inverse distance scaled
            float weight = 1f - Mathf.Clamp01(dist / separationRadius);
            force += toSelf.normalized * (pushForce * weight);
            count++;
        }

        if (count == 0) return Vector2.zero;
        force = Vector2.ClampMagnitude(force, maxTotalForce);
        return force;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}