using System.Collections;
using UnityEngine;

public class BatAI : MonoBehaviour
{
    private enum State
    {
        Roaming,
        Chasing,
        Retreating,
        Attacking
    }

    private State state;
    private BatPathFinding batPathFinding;
    private Transform playerTransform;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 7f; // Bats can detect from farther away
    [SerializeField] private float attackRange = 2f; // Range to start attacking

    [Header("Behavior")]
    [SerializeField] private float attackCooldown = 1.5f; // Time between attacks
    [SerializeField] private float retreatDistance = 4f; // How far to retreat after attack
    [SerializeField] private float retreatDuration = 1f; // How long to retreat

    private float lastAttackTime;
    private Vector2 retreatTarget;

    private void Awake()
    {
        batPathFinding = GetComponent<BatPathFinding>();
        state = State.Roaming;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        StartCoroutine(AIBehaviour());
    }

    private IEnumerator AIBehaviour()
    {
        while (true)
        {
            if (playerTransform != null)
            {
                float distance = Vector2.Distance(transform.position, playerTransform.position);

                switch (state)
                {
                    case State.Roaming:
                        if (distance < detectionRange)
                        {
                            state = State.Chasing;
                        }
                        else
                        {
                            Vector2 roamPosition = GetRoamingPosition();
                            batPathFinding.MoveTo(roamPosition);
                        }
                        break;

                    case State.Chasing:
                        if (distance > detectionRange)
                        {
                            state = State.Roaming;
                        }
                        else if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
                        {
                            state = State.Attacking;
                            lastAttackTime = Time.time;
                            StartCoroutine(AttackAndRetreat());
                        }
                        else
                        {
                            batPathFinding.MoveTo(playerTransform.position);
                        }
                        break;

                    case State.Attacking:
                        // Handled by coroutine
                        break;

                    case State.Retreating:
                        batPathFinding.MoveTo(retreatTarget);
                        break;
                }
            }
            else
            {
                // Search for player if lost
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator AttackAndRetreat()
    {
        // Quick dash toward player for attack
        batPathFinding.MoveTo(playerTransform.position);
        yield return new WaitForSeconds(0.3f);

        // Calculate retreat position (away from player)
        Vector2 directionAwayFromPlayer = (transform.position - playerTransform.position).normalized;
        retreatTarget = (Vector2)transform.position + directionAwayFromPlayer * retreatDistance;

        state = State.Retreating;
        yield return new WaitForSeconds(retreatDuration);

        // Return to chasing
        state = State.Chasing;
    }

    private Vector2 GetRoamingPosition()
    {
        return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
