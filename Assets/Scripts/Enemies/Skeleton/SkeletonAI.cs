using System.Collections;
using UnityEngine;

public class SkeletonAI : MonoBehaviour
{
    private enum State
    {
        Idle,           // Standing guard, facing player
        Approaching,    // Moving closer to player (too far)
        Retreating,     // Moving back from player (too close)
        InRange,        // In optimal sword range, preparing to attack
        Attacking,      // Executing sword slash
        Blocking,       // Brief "block" state before attack
        Cooldown        // Recovery after attack
    }

    private State state;
    private SkeletonPathFinding skeletonPathFinding;
    private Transform playerTransform;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Detection & Range")]
    [SerializeField] private float detectionRange = 10f; // How far skeleton can see player
    [SerializeField] private float optimalMinRange = 1.2f; // Minimum sword range
    [SerializeField] private float optimalMaxRange = 2.0f; // Maximum sword range
    [SerializeField] private float attackRange = 2.2f; // Range to initiate attack

    [Header("Movement")]
    [SerializeField] private float approachSpeed = 2.5f; // Speed when moving toward player
    [SerializeField] private float retreatSpeed = 2.0f; // Speed when backing away

    [Header("Attack Settings")]
    [SerializeField] private float attackPrepareTime = 0.3f; // Time before attack starts
    [SerializeField] private float attackAnimationFrame = 0.06f; // Frame 7 = 0.06s when damage occurs
    [SerializeField] private float attackDuration = 0.5f; // How long attack animation lasts
    [SerializeField] private float attackCooldown = 1.2f; // Time between attacks
    [SerializeField] private int attackDamage = 1; // Damage per hit
    [SerializeField] private bool useComboAttack = false; // Toggle 1-hit or 2-hit combo
    [SerializeField] private float comboDelay = 0.25f; // Delay between combo hits
    
    [Header("Block/Parry Mechanic")]
    [SerializeField] private bool enableBlockMechanic = true;
    [SerializeField] private float blockWindowDuration = 0.25f; // How long block window lasts
    [SerializeField] private float blockDamageReduction = 0.5f; // 0 = full block, 1 = no reduction
    [SerializeField] private Color blockTintColor = new Color(0.3f, 0.7f, 1f, 1f); // Bright cyan when blocking

    [Header("Attack Hitbox")]
    [SerializeField] private Vector2 slashOffset = new Vector2(1.5f, 0f); // Offset from skeleton position
    [SerializeField] private float slashRadius = 1.8f; // Radius of slash attack
    [SerializeField] private LayerMask playerLayer; // Layer for player detection

    [Header("Visual Feedback")]
    [SerializeField] private GameObject blockVFXPrefab; // Optional spark/glow effect for block
    [SerializeField] private float blockFlashSpeed = 15f; // How fast to flash when blocking
    [SerializeField] private GameObject attackWindupVFX; // Visual effect during attack preparation
    [SerializeField] private GameObject attackTrailVFX; // Trail effect during slash
    [SerializeField] private Color attackWindupColor = new Color(1f, 0.2f, 0f, 1f); // Bright red-orange for attack warning
    [SerializeField] private float attackWindupIntensity = 0.9f; // Very obvious telegraph
    [SerializeField] private bool useFlashingWarning = true; // Flash rapidly during attack prep
    [SerializeField] private float warningFlashSpeed = 25f; // How fast to flash
    
    [Header("AI Intelligence")]
    [SerializeField] private bool enableSmartPositioning = true; // Tries to position for better attacks
    [SerializeField] private bool avoidPlayerAttacks = true; // Tries to dodge player sword
    [SerializeField] private float reactionTime = 0.15f; // How fast skeleton reacts to player actions (reduced for smarter AI)
    [SerializeField] private bool patrolWhenIdle = true; // Patrol small area when player is far
    [SerializeField] private float patrolRadius = 3f; // How far to patrol

    private State previousState;
    private float stateTimer = 0f;
    private bool isBlocking = false;
    private bool hasAttacked = false; // Track if attack has dealt damage this cycle
    private int comboHitCount = 0; // Track combo hits
    private Color originalColor;
    private Vector3 originalScale; // Store original scale for animations
    private float originalBlockDamageReduction;
    private GameObject currentWindupVFX; // Reference to spawned VFX
    private Vector2 patrolCenter; // Center point for patrol
    private float patrolAngle = 0f; // Current patrol angle
    private bool playerIsAttacking = false; // Track if player is attacking
    private float lastPlayerAttackTime = 0f;
    
    // Animation parameter names (matching your animator controller)
    private int attackTriggerParam;
    private int isMovingParam;

    private void Awake()
    {
        skeletonPathFinding = GetComponent<SkeletonPathFinding>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        state = State.Idle;
        
        // Prevent player from pushing skeleton - make it immovable during combat
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.mass = 10f; // Heavy mass
            rb.linearDamping = 5f; // High drag to resist pushes
        }
        
        // Initialize animator parameters immediately in Awake
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // Try to find the correct parameter names in the animator
            bool hasAttackParam = false;
            bool hasIsMovingParam = false;
            
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"SkeletonAI: Found animator parameter: {param.name} (type: {param.type})");
                
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    if (param.name.ToLower().Contains("attack"))
                    {
                        attackTriggerParam = Animator.StringToHash(param.name);
                        hasAttackParam = true;
                        Debug.Log($"SkeletonAI: Using '{param.name}' for attack trigger");
                    }
                }
                
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    if (param.name.ToLower().Contains("moving") || param.name.ToLower().Contains("move"))
                    {
                        isMovingParam = Animator.StringToHash(param.name);
                        hasIsMovingParam = true;
                        Debug.Log($"SkeletonAI: Using '{param.name}' for isMoving bool");
                    }
                }
            }
            
            if (!hasAttackParam)
            {
                Debug.LogError("SkeletonAI: No 'attack' trigger parameter found in animator! Please add it.", this);
            }
            if (!hasIsMovingParam)
            {
                Debug.LogError("SkeletonAI: No 'isMoving' bool parameter found in animator! Please add it.", this);
            }
        }
        else
        {
            Debug.LogError("SkeletonAI: No Animator or AnimatorController found!", this);
        }
        
        // Auto-detect player layer if not set
        if (playerLayer == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        originalScale = transform.localScale; // Store original scale
        originalBlockDamageReduction = blockDamageReduction;

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

            // Always face the player when detected
            if (distance < detectionRange)
            {
                FacePlayer();
            }

            switch (state)
            {
                case State.Idle:
                    HandleIdle(distance);
                    break;

                case State.Approaching:
                    HandleApproaching(distance);
                    break;

                case State.Retreating:
                    HandleRetreating(distance);
                    break;

                case State.InRange:
                    HandleInRange(distance);
                    break;

                case State.Blocking:
                    HandleBlocking();
                    break;

                case State.Attacking:
                    HandleAttacking();
                    break;

                case State.Cooldown:
                    HandleCooldown(distance);
                    break;
            }

            yield return new WaitForSeconds(0.05f); // Update at 20 FPS for responsive AI
        }
    }

    // Called when skeleton dies to stop AI behavior
    public void StopAI()
    {
        StopAllCoroutines();
        enabled = false;
        
        // Reset visuals immediately
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        transform.localScale = originalScale;
        
        // Clean up VFX
        if (currentWindupVFX != null)
        {
            Destroy(currentWindupVFX);
            currentWindupVFX = null;
        }
    }

    private void HandleIdle(float distance)
    {
        // Reset visuals
        if (spriteRenderer != null && spriteRenderer.color != originalColor)
        {
            spriteRenderer.color = originalColor;
        }

        // Player detected, decide action
        if (distance < detectionRange)
        {
            skeletonPathFinding.StopMoving();
            UpdateAnimator(false, 0f);
            
            if (distance > optimalMaxRange)
            {
                state = State.Approaching;
            }
            else if (distance < optimalMinRange)
            {
                state = State.Retreating;
            }
            else
            {
                state = State.InRange;
            }
        }
        else if (patrolWhenIdle)
        {
            // Patrol in small circle when player is far
            if (patrolCenter == Vector2.zero)
            {
                patrolCenter = transform.position;
            }
            
            patrolAngle += 30f * 0.1f; // Slow patrol rotation
            if (patrolAngle > 360f) patrolAngle -= 360f;
            
            float rad = patrolAngle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * patrolRadius;
            Vector2 targetPos = patrolCenter + offset;
            
            skeletonPathFinding.SetMoveSpeed(approachSpeed * 0.4f); // Slow patrol
            skeletonPathFinding.MoveTo(targetPos);
            UpdateAnimator(true, approachSpeed * 0.4f);
        }
        else
        {
            skeletonPathFinding.StopMoving();
            UpdateAnimator(false, 0f);
        }
    }

    private void HandleApproaching(float distance)
    {
        // Player out of detection range, return to idle
        if (distance > detectionRange)
        {
            state = State.Idle;
            patrolCenter = transform.position; // Reset patrol center
            skeletonPathFinding.StopMoving();
            UpdateAnimator(false, 0f);
            return;
        }

        // Smart positioning: try to approach from an angle if enabled
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector2 targetPosition;
        
        if (enableSmartPositioning)
        {
            // Circle around player slightly instead of direct approach
            Vector2 perpendicular = new Vector2(-directionToPlayer.y, directionToPlayer.x);
            float circleOffset = Mathf.Sin(Time.time * 2f) * 0.3f; // Subtle weaving
            targetPosition = (Vector2)transform.position + (directionToPlayer + perpendicular * circleOffset) * approachSpeed * 0.1f;
        }
        else
        {
            targetPosition = (Vector2)transform.position + directionToPlayer * approachSpeed * 0.1f;
        }
        
        skeletonPathFinding.SetMoveSpeed(approachSpeed);
        skeletonPathFinding.MoveTo(targetPosition);
        UpdateAnimator(true, approachSpeed);

        // Check if we've reached optimal range
        if (distance <= optimalMaxRange)
        {
            state = State.InRange;
            skeletonPathFinding.StopMoving();
            UpdateAnimator(false, 0f);
        }
    }

    private void HandleRetreating(float distance)
    {
        // Player out of detection range, return to idle
        if (distance > detectionRange)
        {
            state = State.Idle;
            skeletonPathFinding.StopMoving();
            UpdateAnimator(false, 0f);
            return;
        }

        // Move away from player
        Vector2 directionAwayFromPlayer = (transform.position - playerTransform.position).normalized;
        Vector2 targetPosition = (Vector2)transform.position + directionAwayFromPlayer * retreatSpeed * 0.1f;
        
        skeletonPathFinding.SetMoveSpeed(retreatSpeed);
        skeletonPathFinding.MoveTo(targetPosition);
        UpdateAnimator(true, retreatSpeed);

        // Check if we've reached optimal range
        if (distance >= optimalMinRange)
        {
            state = State.InRange;
            skeletonPathFinding.StopMoving();
        }
    }

    private void HandleInRange(float distance)
    {
        skeletonPathFinding.StopMoving();
        UpdateAnimator(false, 0f);

        // Player moved out of optimal range
        if (distance > optimalMaxRange)
        {
            state = State.Approaching;
            return;
        }
        else if (distance < optimalMinRange)
        {
            state = State.Retreating;
            return;
        }

        // Check if player is attacking (simple detection based on distance change)
        if (avoidPlayerAttacks)
        {
            DetectPlayerAttack();
        }

        // Player is in attack range, prepare to attack
        if (distance <= attackRange)
        {
            // If player just attacked very recently, brief hesitation (more aggressive AI)
            if (Time.time - lastPlayerAttackTime < reactionTime * 0.5f)
            {
                // Very brief hesitation
                return;
            }
            
            // Enter blocking state first if mechanic is enabled
            if (enableBlockMechanic)
            {
                state = State.Blocking;
                stateTimer = blockWindowDuration;
                isBlocking = true;
            }
            else
            {
                state = State.Attacking;
                stateTimer = attackPrepareTime;
                hasAttacked = false;
                comboHitCount = 0;
            }
        }
    }

    private void HandleBlocking()
    {
        skeletonPathFinding.StopMoving();
        UpdateAnimator(false, 0f);

        // Enhanced visual feedback for blocking - VERY OBVIOUS
        if (spriteRenderer != null)
        {
            // Strong pulsing cyan color
            float flashValue = Mathf.PingPong(Time.time * blockFlashSpeed, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, blockTintColor, 0.7f + flashValue * 0.3f);
            
            // Stronger scale pulse for visibility
            float scalePulse = 1f + Mathf.Sin(Time.time * 20f) * 0.08f;
            transform.localScale = originalScale * scalePulse;
        }

        // Spawn block VFX if available (only once at start)
        if (blockVFXPrefab != null && stateTimer >= blockWindowDuration - 0.05f)
        {
            GameObject vfx = Instantiate(blockVFXPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            vfx.transform.SetParent(transform);
            Destroy(vfx, 0.3f);
        }

        stateTimer -= 0.05f;

        if (stateTimer <= 0f)
        {
            // Block window ended, now attack
            isBlocking = false;
            state = State.Attacking;
            stateTimer = attackPrepareTime;
            hasAttacked = false;
            comboHitCount = 0;
            
            // Reset visuals
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            transform.localScale = originalScale;
        }
    }

    private void HandleAttacking()
    {
        skeletonPathFinding.StopMoving();

        stateTimer -= 0.05f;

        // Attack preparation phase - VERY OBVIOUS VISUAL TELEGRAPH
        if (stateTimer >= attackPrepareTime - 0.1f)
        {
            // Trigger attack animation
            if (animator != null && attackTriggerParam != 0)
            {
                animator.SetTrigger(attackTriggerParam);
            }
            
            // Spawn wind-up VFX (only once)
            if (currentWindupVFX == null && attackWindupVFX != null)
            {
                Vector2 attackDir = (playerTransform.position - transform.position).normalized;
                Vector3 vfxPos = transform.position + (Vector3)(attackDir * 1f);
                currentWindupVFX = Instantiate(attackWindupVFX, vfxPos, Quaternion.identity);
                currentWindupVFX.transform.SetParent(transform);
            }
            
            // INTENSE visual telegraph: Bright flashing red warning
            if (spriteRenderer != null)
            {
                float windupProgress = 1f - (stateTimer / attackPrepareTime);
                
                if (useFlashingWarning)
                {
                    // Rapid flashing for maximum visibility
                    float flashValue = Mathf.PingPong(Time.time * warningFlashSpeed, 1f);
                    Color warningColor = Color.Lerp(attackWindupColor, Color.red, flashValue);
                    spriteRenderer.color = Color.Lerp(originalColor, warningColor, windupProgress * attackWindupIntensity);
                }
                else
                {
                    spriteRenderer.color = Color.Lerp(originalColor, attackWindupColor, windupProgress * attackWindupIntensity);
                }
                
                // Lean back more obviously (anticipation)
                transform.localScale = originalScale * (1f - windupProgress * 0.15f);
            }
        }
        // Attack execution phase - TIMED TO FRAME 7
        else if (stateTimer <= attackPrepareTime - attackAnimationFrame && !hasAttacked)
        {
            // Destroy windup VFX
            if (currentWindupVFX != null)
            {
                Destroy(currentWindupVFX);
                currentWindupVFX = null;
            }
            
            // Spawn attack trail VFX
            if (attackTrailVFX != null)
            {
                Vector2 attackDir = (playerTransform.position - transform.position).normalized;
                GameObject trail = Instantiate(attackTrailVFX, transform.position, Quaternion.LookRotation(Vector3.forward, attackDir));
                Destroy(trail, 0.5f);
            }
            
            // Brief bright flash on impact
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white; // Bright white flash
            }
            transform.localScale = originalScale * 1.1f; // Slight grow on impact
            
            // Perform the actual attack
            PerformSlashAttack();
            
            // Schedule return to normal (next frame)
            StartCoroutine(ResetVisualAfterDelay(0.05f));
            hasAttacked = true;
            comboHitCount++;

            // If combo enabled and this is first hit, schedule second hit
            if (useComboAttack && comboHitCount == 1)
            {
                stateTimer = comboDelay; // Reset timer for second hit
                hasAttacked = false; // Allow second hit
            }
        }

        // Attack complete, enter cooldown
        if (stateTimer <= -attackDuration)
        {
            // Cleanup
            if (currentWindupVFX != null)
            {
                Destroy(currentWindupVFX);
                currentWindupVFX = null;
            }
            
            state = State.Cooldown;
            stateTimer = attackCooldown;
        }
    }

    private void HandleCooldown(float distance)
    {
        skeletonPathFinding.StopMoving();
        UpdateAnimator(false, 0f);

        // Reset visuals
        if (spriteRenderer != null && spriteRenderer.color != originalColor)
        {
            spriteRenderer.color = originalColor;
        }

        stateTimer -= 0.05f;

        if (stateTimer <= 0f)
        {
            // Cooldown complete, return to idle
            state = State.Idle;
        }
    }

    private void PerformSlashAttack()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("SkeletonAI: Cannot attack, player transform is null!");
            return;
        }

        // Calculate slash position based on facing direction
        Vector2 slashDirection = (playerTransform.position - transform.position).normalized;
        Vector2 slashPosition = (Vector2)transform.position + slashDirection * slashOffset.x + Vector2.up * slashOffset.y;

        // Detect all colliders in slash radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(slashPosition, slashRadius, playerLayer);
        
        Debug.Log($"SkeletonAI: Slash attack! Found {hits.Length} colliders at position {slashPosition}");

        foreach (Collider2D hit in hits)
        {
            Debug.Log($"SkeletonAI: Hit collider: {hit.name} with tag: {hit.tag}");
            
            // Check if player is hit
            if (hit.CompareTag("Player"))
            {
                // Deal damage using PlayerStats (your project's health system)
                PlayerStats playerStats = hit.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(attackDamage);
                    Debug.Log($"Skeleton hit player for {attackDamage} damage!");
                }
                else
                {
                    Debug.LogWarning($"SkeletonAI: Player has no PlayerStats component!");
                }

                // Apply knockback to player if available
                Knockback playerKnockback = hit.GetComponent<Knockback>();
                if (playerKnockback != null)
                {
                    playerKnockback.GetKnockedBack(transform, 10f);
                }
            }
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null || spriteRenderer == null) return;

        // Flip sprite to face player
        bool shouldFlipLeft = playerTransform.position.x < transform.position.x;
        spriteRenderer.flipX = shouldFlipLeft;
    }

    private void UpdateAnimator(bool isMoving, float speed)
    {
        if (animator == null) return;

        animator.SetBool(isMovingParam, isMoving);
        // Note: MoveSpeed parameter not used in your animator, relying on isMoving only
    }
    
    private System.Collections.IEnumerator ResetVisualAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        transform.localScale = originalScale;
    }
    
    private void DetectPlayerAttack()
    {
        // Simple detection: check if player is in attack animation or has weapon active
        if (playerTransform == null) return;
        
        // Check for sword collider or damage source near player (indicates attacking)
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(playerTransform.position, 2f);
        foreach (Collider2D col in nearbyColliders)
        {
            // Check by name (safer than tag) or component
            bool isSword = col.name.ToLower().Contains("sword") || 
                          col.name.ToLower().Contains("weapon") ||
                          col.name.ToLower().Contains("slash");
            
            // Or check if it has a damage source component
            bool hasDamageSource = col.GetComponent<DamageSource>() != null;
            
            if (isSword || hasDamageSource)
            {
                playerIsAttacking = true;
                lastPlayerAttackTime = Time.time;
                
                // React: try to dodge or block
                if (avoidPlayerAttacks && state == State.InRange)
                {
                    float dodgeRoll = Random.Range(0f, 1f);
                    if (dodgeRoll > 0.5f) // 50% chance to dodge
                    {
                        state = State.Retreating; // Quick retreat
                    }
                }
                break;
            }
        }
    }

    // Called by SkeletonHealth when taking damage during block window
    public float GetBlockDamageReduction()
    {
        return isBlocking ? blockDamageReduction : 1f; // 1f = full damage, <1f = reduced
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    // Cancel attack if hit during attack animation (optional skill-based mechanic)
    public void CancelAttack()
    {
        if (state == State.Attacking)
        {
            state = State.Cooldown;
            stateTimer = attackCooldown * 0.5f; // Shorter cooldown when interrupted
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Optimal range zone
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, optimalMinRange);
        Gizmos.DrawWireSphere(transform.position, optimalMaxRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Slash hitbox (if in range and player exists)
        if (Application.isPlaying && playerTransform != null)
        {
            Vector2 slashDirection = (playerTransform.position - transform.position).normalized;
            Vector2 slashPosition = (Vector2)transform.position + slashDirection * slashOffset.x + Vector2.up * slashOffset.y;
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(slashPosition, slashRadius);
        }
    }
}
