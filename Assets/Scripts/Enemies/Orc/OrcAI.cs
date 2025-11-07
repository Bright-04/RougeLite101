using System.Collections;
using UnityEngine;

/// <summary>
/// Orc AI: Slow but tanky enemy with charge attack
/// High health, slow movement, charges when close to player
/// </summary>
public class OrcAI : BaseEnemy
{
    private enum State
    {
        Idle,
        Walking,
        Charging,
        Stunned
    }

    private State currentState;
    
    [Header("Orc Settings")]
    [SerializeField] private float chargeRange = 5f;
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDuration = 1f;
    [SerializeField] private float chargeCooldown = 3f;
    [SerializeField] private float stunDuration = 0.5f;
    
    private float lastChargeTime;
    private Vector2 chargeDirection;
    private bool isCharging;

#if UNITY_EDITOR
    // Expose state for debugging in Editor
    public string GetCurrentState() => currentState.ToString();
#endif

    protected override void Start()
    {
        base.Start();
        moveSpeed = 1.5f; // Slower base speed
        currentState = State.Idle;
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

            // Don't process AI during charge or stun
            if (currentState == State.Charging || currentState == State.Stunned)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            float distanceToPlayer = GetDistanceToPlayer();

            if (distanceToPlayer < chargeRange && Time.time > lastChargeTime + chargeCooldown)
            {
                // Start charge
                yield return StartCoroutine(ChargeAttack());
            }
            else if (distanceToPlayer < detectionRange)
            {
                // Walk towards player
                currentState = State.Walking;
                WalkToPlayer();
            }
            else
            {
                // Out of range
                currentState = State.Idle;
                rb.linearVelocity = Vector2.zero;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void WalkToPlayer()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        Vector2 direction = GetDirectionToPlayer();
        rb.linearVelocity = direction * moveSpeed;
    }

    private IEnumerator ChargeAttack()
    {
        currentState = State.Charging;
        lastChargeTime = Time.time;
        isCharging = true;

        // Store charge direction
        chargeDirection = GetDirectionToPlayer();

        // Charge forward
        float chargeTimer = 0f;
        while (chargeTimer < chargeDuration && !dead)
        {
            if (knockback == null || !knockback.gettingKnockedBack)
            {
                rb.linearVelocity = chargeDirection * chargeSpeed;
            }
            chargeTimer += Time.deltaTime;
            yield return null;
        }

        isCharging = false;

        // Stun after charge
        yield return StartCoroutine(Stunned());
    }

    private IEnumerator Stunned()
    {
        currentState = State.Stunned;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(stunDuration);
        currentState = State.Idle;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If charging and hit a wall, get stunned
        if (isCharging && (collision.gameObject.layer == LayerMask.NameToLayer("Wall") || 
            collision.gameObject.CompareTag("Wall")))
        {
            isCharging = false;
            StartCoroutine(Stunned());
        }
    }

    private void FixedUpdate()
    {
        if (dead || (knockback != null && knockback.gettingKnockedBack && !isCharging))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
