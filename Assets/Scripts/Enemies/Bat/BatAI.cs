using System.Collections;
using UnityEngine;

public class BatAI : MonoBehaviour
{
    private enum State
    {
        IdleRoaming,      // Circle around current position
        PrepareCharge,    // Detected player, standing still before charge
        Charging,         // Fast charge toward player
        Cooldown          // Standing still after charge
    }

    private State state;
    private BatPathFinding batPathFinding;
    private Transform playerTransform;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f; // Detection range

    [Header("Charge Attack")]
    [SerializeField] private float prepareTime = 0.6f; // Time to stand still before charging
    [SerializeField] private float chargeSpeed = 12f; // Speed during charge (much faster than normal)
    [SerializeField] private float chargeDuration = 0.5f; // How long the charge lasts
    [SerializeField] private float cooldownTime = 1.0f; // Time to wait after charge before next one
    
    [Header("Visual Effects")]
    [SerializeField] private float prepareFlashSpeed = 10f; // How fast to flash when preparing
    [SerializeField] private Color prepareColor = new Color(1f, 0.3f, 0.3f, 1f); // Red tint when preparing
    [SerializeField] private Color chargeColor = new Color(1f, 0f, 0f, 1f); // Bright red when charging

    [Header("Roaming")]
    [SerializeField] private float roamRadius = 2f; // Radius to circle around
    [SerializeField] private float roamSpeed = 1.5f; // Speed while idly roaming

    private Vector2 roamCenter; // Center point for circling
    private float roamAngle = 0f; // Current angle in circle
    private Vector2 chargeDirection; // Direction to charge
    private Vector2 chargeTargetPosition; // Exact position to charge to
    private float stateTimer = 0f; // Timer for current state
    private float chargeDistance = 0f; // How far to charge
    
    // Visual components
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;

    private void Awake()
    {
        batPathFinding = GetComponent<BatPathFinding>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        state = State.IdleRoaming;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // Set initial roam center to starting position
        roamCenter = transform.position;
        roamAngle = Random.Range(0f, 360f);
        
        // Store original visuals
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        originalScale = transform.localScale;

        StartCoroutine(AIBehaviour());
    }

    private IEnumerator AIBehaviour()
    {
        while (true)
        {
            if (playerTransform == null)
            {
                // Search for player if lost
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            float distance = Vector2.Distance(transform.position, playerTransform.position);

            switch (state)
            {
                case State.IdleRoaming:
                    HandleIdleRoaming(distance);
                    break;

                case State.PrepareCharge:
                    HandlePrepareCharge();
                    break;

                case State.Charging:
                    HandleCharging();
                    break;

                case State.Cooldown:
                    HandleCooldown(distance);
                    break;
            }

            yield return new WaitForSeconds(0.05f); // Faster update for smooth charging
        }
    }

    private void HandleIdleRoaming(float distanceToPlayer)
    {
        // Ensure normal visuals during roaming
        if (spriteRenderer != null && spriteRenderer.color != originalColor)
        {
            spriteRenderer.color = originalColor;
        }
        if (transform.localScale != originalScale)
        {
            transform.localScale = originalScale;
        }
        
        // Check if player is in range
        if (distanceToPlayer < detectionRange)
        {
            // Player detected! Prepare to charge
            state = State.PrepareCharge;
            stateTimer = prepareTime;
            batPathFinding.StopMoving();
            
            // Calculate charge direction
            chargeDirection = (playerTransform.position - transform.position).normalized;
            return;
        }

        // Circle around the roam center
        roamAngle += roamSpeed * 0.05f * 50f; // 50 degrees per second (0.05 = update interval)
        if (roamAngle > 360f) roamAngle -= 360f;

        float rad = roamAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * roamRadius;
        Vector2 targetPos = roamCenter + offset;

        batPathFinding.MoveTo(targetPos);
    }

    private void HandlePrepareCharge()
    {
        // Stand completely still, preparing to charge
        batPathFinding.StopMoving();
        
        // Keep updating direction to player during prepare phase
        if (playerTransform != null)
        {
            chargeDirection = (playerTransform.position - transform.position).normalized;
        }
        
        // Visual feedback: Flash red and shake
        if (spriteRenderer != null)
        {
            float flashValue = Mathf.PingPong(Time.time * prepareFlashSpeed, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, prepareColor, flashValue);
        }
        
        // Shake effect during prepare
        float shakeAmount = 0.05f;
        transform.localScale = originalScale + new Vector3(
            Mathf.Sin(Time.time * 30f) * shakeAmount,
            Mathf.Sin(Time.time * 25f) * shakeAmount,
            0f
        );
        
        // Decrement timer (using fixed 0.05 from coroutine wait time)
        stateTimer -= 0.05f;

        if (stateTimer <= 0f)
        {
            // Time to charge! Lock in the target position
            state = State.Charging;
            
            // Calculate fixed charge distance and target
            chargeDistance = chargeSpeed * chargeDuration;
            chargeTargetPosition = (Vector2)transform.position + chargeDirection * chargeDistance;
            
            // Use distance-based tracking instead of time
            stateTimer = chargeDuration * 1.5f; // Safety timeout
            
            // Set charging visuals
            if (spriteRenderer != null)
            {
                spriteRenderer.color = chargeColor;
            }
            transform.localScale = originalScale * 1.2f; // Slightly bigger when charging
        }
    }

    private void HandleCharging()
    {
        // Check if we've reached the target or hit something
        float distanceToTarget = Vector2.Distance(transform.position, chargeTargetPosition);
        
        // Add motion trail effect (stretch sprite in direction of movement)
        Vector2 velocity = ((Vector2)transform.position - chargeTargetPosition).normalized;
        transform.localScale = new Vector3(
            originalScale.x * 1.2f,
            originalScale.y * 0.9f, // Squish vertically
            originalScale.z
        );
        
        // Stop if we're close to target or timeout
        stateTimer -= 0.05f;
        if (distanceToTarget < 0.3f || stateTimer <= 0f)
        {
            // Charge complete, enter cooldown
            state = State.Cooldown;
            stateTimer = cooldownTime;
            batPathFinding.SetChargeMode(false, 0f);
            batPathFinding.StopMoving();
            
            // Reset visuals
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            transform.localScale = originalScale;
            return;
        }
        
        // Continue charging toward target
        batPathFinding.SetChargeMode(true, chargeSpeed);
        batPathFinding.MoveTo(chargeTargetPosition);
    }

    private void HandleCooldown(float distanceToPlayer)
    {
        // Stand still after charge
        batPathFinding.StopMoving();
        
        // Ensure visuals are reset
        if (spriteRenderer != null && spriteRenderer.color != originalColor)
        {
            spriteRenderer.color = originalColor;
        }
        if (transform.localScale != originalScale)
        {
            transform.localScale = originalScale;
        }
        
        stateTimer -= 0.05f;

        if (stateTimer <= 0f)
        {
            // Cooldown complete, decide next action
            if (distanceToPlayer < detectionRange)
            {
                // Still in range, prepare another charge
                state = State.PrepareCharge;
                stateTimer = prepareTime;
                chargeDirection = (playerTransform.position - transform.position).normalized;
            }
            else
            {
                // Player out of range, return to roaming
                state = State.IdleRoaming;
                roamCenter = transform.position; // New roam center
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Roam radius
        if (state == State.IdleRoaming && Application.isPlaying)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(roamCenter, roamRadius);
        }

        // Charge direction
        if (state == State.PrepareCharge || state == State.Charging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, chargeDirection * 3f);
        }
    }
}
