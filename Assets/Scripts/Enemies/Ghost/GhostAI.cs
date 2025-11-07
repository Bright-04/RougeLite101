using System.Collections;
using UnityEngine;

/// <summary>
/// Ghost AI: Teleporting enemy that can phase through obstacles
/// Teleports near the player periodically
/// </summary>
public class GhostAI : BaseEnemy
{
    private enum State
    {
        Floating,
        Teleporting,
        Chasing
    }

    private State currentState;
    [SerializeField] private float teleportCooldown = 4f;
    [SerializeField] private float teleportDistance = 3f;
    [SerializeField] private float fadeTime = 0.3f;
    
    private float lastTeleportTime;
    private SpriteRenderer spriteRenderer;
    private Vector2 floatDirection;
    private float floatChangeTimer;

#if UNITY_EDITOR
    // Expose state for debugging in Editor
    public string GetCurrentState() => currentState.ToString();
#endif

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void Start()
    {
        base.Start();
        currentState = State.Floating;
        floatDirection = Random.insideUnitCircle.normalized;
        StartCoroutine(AIBehaviour());
    }

    private IEnumerator AIBehaviour()
    {
        while (!dead)
        {
            if (playerTransform == null)
            {
                FloatAround();
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            float distanceToPlayer = GetDistanceToPlayer();

            // Try to teleport if player is detected and cooldown is ready
            if (distanceToPlayer < detectionRange && Time.time > lastTeleportTime + teleportCooldown)
            {
                yield return StartCoroutine(TeleportNearPlayer());
            }
            else if (distanceToPlayer < detectionRange / 2)
            {
                // Chase player if close
                currentState = State.Chasing;
                ChasePlayer();
            }
            else
            {
                // Float around
                currentState = State.Floating;
                FloatAround();
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void FloatAround()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        floatChangeTimer += Time.deltaTime;
        if (floatChangeTimer > 2f)
        {
            floatDirection = Random.insideUnitCircle.normalized;
            floatChangeTimer = 0f;
        }

        rb.linearVelocity = floatDirection * (moveSpeed * 0.5f);
    }

    private void ChasePlayer()
    {
        if (knockback != null && knockback.gettingKnockedBack) return;

        Vector2 direction = GetDirectionToPlayer();
        rb.linearVelocity = direction * (moveSpeed * 0.7f);
    }

    private IEnumerator TeleportNearPlayer()
    {
        currentState = State.Teleporting;
        lastTeleportTime = Time.time;

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Teleport to a position near the player
        Vector2 randomOffset = Random.insideUnitCircle * teleportDistance;
        Vector2 newPosition = (Vector2)playerTransform.position + randomOffset;
        transform.position = newPosition;

        // Fade in
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        Color color = spriteRenderer.color;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            spriteRenderer.color = color;
            yield return null;
        }

        color.a = 0f;
        spriteRenderer.color = color;
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Color color = spriteRenderer.color;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeTime);
            spriteRenderer.color = color;
            yield return null;
        }

        color.a = 1f;
        spriteRenderer.color = color;
    }

    private void FixedUpdate()
    {
        if (dead || (knockback != null && knockback.gettingKnockedBack))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
