using System.Collections;
using UnityEngine;

/// <summary>
/// Goblin AI: Aggressive chaser - faster and more persistent than slimes
/// Always chases player when in range, no roaming behavior
/// </summary>
public class GoblinAI : BaseEnemy
{
    protected override void Start()
    {
        base.Start();
        StartCoroutine(AIBehaviour());
    }

    private IEnumerator AIBehaviour()
    {
        while (!dead)
        {
            if (playerTransform == null)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            float distanceToPlayer = GetDistanceToPlayer();

            if (distanceToPlayer < detectionRange)
            {
                // Chase player aggressively
                ChasePlayer();
            }
            else
            {
                // Out of range - stop
                rb.linearVelocity = Vector2.zero;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void ChasePlayer()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        Vector2 direction = GetDirectionToPlayer();
        rb.linearVelocity = direction * moveSpeed;
    }

    private void FixedUpdate()
    {
        if (dead || knockback != null && knockback.gettingKnockedBack)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
