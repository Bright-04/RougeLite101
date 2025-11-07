using System.Collections;
using UnityEngine;

/// <summary>
/// Bat AI: Flying enemy that patrols in patterns and swoops at player
/// Can fly over obstacles
/// </summary>
public class BatAI : BaseEnemy
{
    private enum State
    {
        Patrolling,
        Swooping,
        Returning
    }

    private State currentState;
    
    [Header("Bat Settings")]
    [SerializeField] private float patrolRadius = 8f;
    [SerializeField] private float swoopSpeed = 6f;
    [SerializeField] private float swoopCooldown = 2.5f;
    [SerializeField] private float circleSpeed = 2f;
    
    private Vector2 patrolCenter;
    private float patrolAngle;
    private float lastSwoopTime;
    private Vector2 swoopStartPosition;

#if UNITY_EDITOR
    // Expose state for debugging in Editor
    public string GetCurrentState() => currentState.ToString();
#endif

    protected override void Start()
    {
        base.Start();
        patrolCenter = transform.position;
        patrolAngle = Random.Range(0f, 360f);
        currentState = State.Patrolling;
        StartCoroutine(AIBehaviour());
    }

    private IEnumerator AIBehaviour()
    {
        while (!dead)
        {
            if (playerTransform == null)
            {
                currentState = State.Patrolling;
                Patrol();
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            float distanceToPlayer = GetDistanceToPlayer();

            switch (currentState)
            {
                case State.Patrolling:
                    Patrol();
                    
                    // Check if player is in range and cooldown is ready
                    if (distanceToPlayer < detectionRange && Time.time > lastSwoopTime + swoopCooldown)
                    {
                        swoopStartPosition = transform.position;
                        currentState = State.Swooping;
                        lastSwoopTime = Time.time;
                    }
                    break;

                case State.Swooping:
                    Swoop();
                    
                    // Check if we've passed the player or gotten far from start
                    if (Vector2.Distance(transform.position, swoopStartPosition) > patrolRadius * 1.5f)
                    {
                        currentState = State.Returning;
                    }
                    break;

                case State.Returning:
                    ReturnToPatrol();
                    
                    // Return to patrol when close to patrol center
                    if (Vector2.Distance(transform.position, patrolCenter) < 1f)
                    {
                        currentState = State.Patrolling;
                    }
                    break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Patrol()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        // Circle around patrol center
        patrolAngle += circleSpeed * Time.deltaTime * 50f;
        if (patrolAngle > 360f) patrolAngle -= 360f;

        float radians = patrolAngle * Mathf.Deg2Rad;
        Vector2 targetPosition = patrolCenter + new Vector2(
            Mathf.Cos(radians) * patrolRadius,
            Mathf.Sin(radians) * patrolRadius
        );

        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void Swoop()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        // Swoop towards player
        Vector2 direction = GetDirectionToPlayer();
        rb.linearVelocity = direction * swoopSpeed;
    }

    private void ReturnToPatrol()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        Vector2 direction = (patrolCenter - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * (moveSpeed * 1.5f);
    }

    private void FixedUpdate()
    {
        if (dead || (knockback != null && knockback.gettingKnockedBack))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Visualize patrol area in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? patrolCenter : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, detectionRange);
    }
}
