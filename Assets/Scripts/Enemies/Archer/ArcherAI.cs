using System.Collections;
using UnityEngine;

/// <summary>
/// Archer AI: Ranged attacker that keeps distance and shoots arrows
/// </summary>
public class ArcherAI : BaseEnemy
{
    private enum State
    {
        Idle,
        KeepingDistance,
        Shooting,
        Retreating
    }

    private State currentState;
    
    [Header("Archer Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float optimalRange = 7f;
    [SerializeField] private float tooCloseRange = 4f;
    
    private float lastShootTime;

#if UNITY_EDITOR
    // Expose state for debugging in Editor
    public string GetCurrentState() => currentState.ToString();
#endif

    protected override void Start()
    {
        base.Start();
        currentState = State.Idle;
        
        // Create fire point if it doesn't exist
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            firePoint = firePointObj.transform;
        }
        
        StartCoroutine(AIBehaviour());
    }

    private IEnumerator AIBehaviour()
    {
        while (!dead)
        {
            if (playerTransform == null)
            {
                currentState = State.Idle;
                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            float distanceToPlayer = GetDistanceToPlayer();

            if (distanceToPlayer < tooCloseRange)
            {
                // Too close, retreat
                currentState = State.Retreating;
                Retreat();
            }
            else if (distanceToPlayer > detectionRange)
            {
                // Out of range
                currentState = State.Idle;
                rb.linearVelocity = Vector2.zero;
            }
            else if (distanceToPlayer > optimalRange)
            {
                // Move to optimal range
                currentState = State.KeepingDistance;
                MoveToOptimalRange();
            }
            else
            {
                // In optimal range, try to shoot
                currentState = State.Shooting;
                rb.linearVelocity = Vector2.zero;
                
                if (Time.time > lastShootTime + shootCooldown)
                {
                    ShootArrow();
                    lastShootTime = Time.time;
                }
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    private void MoveToOptimalRange()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        Vector2 direction = GetDirectionToPlayer();
        rb.linearVelocity = direction * (moveSpeed * 0.6f); // Move slower than melee enemies
    }

    private void Retreat()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        Vector2 direction = -GetDirectionToPlayer(); // Move away from player
        rb.linearVelocity = direction * moveSpeed;
    }

    private void ShootArrow()
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("Archer: Arrow prefab not assigned!");
            return;
        }

        Vector2 direction = GetDirectionToPlayer();
        
        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
        Arrow arrowScript = arrow.GetComponent<Arrow>();
        
        if (arrowScript != null)
        {
            arrowScript.Initialize(direction);
        }
    }

    private void FixedUpdate()
    {
        if (dead || (knockback != null && knockback.gettingKnockedBack))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
