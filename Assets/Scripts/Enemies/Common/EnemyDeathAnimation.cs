using System.Collections;
using UnityEngine;

/// <summary>
/// Handles death animation for enemies: knockback + fade out.
/// </summary>
public class EnemyDeathAnimation : MonoBehaviour
{
    [Header("Death Animation Settings")]
    [SerializeField] private float deathKnockbackForce = 20f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float deathDelay = 0.1f;
    [SerializeField] private bool disableAnimatorOnDeath = true;
    [SerializeField] private SpriteRenderer[] fadeRenderers;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer[] resolvedFadeRenderers;
    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private bool isDying;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        resolvedFadeRenderers = fadeRenderers != null && fadeRenderers.Length > 0
            ? fadeRenderers
            : (spriteRenderer != null ? new[] { spriteRenderer } : GetComponentsInChildren<SpriteRenderer>(true));
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();
    }

    public void PlayDeathAnimation(Transform damageSource)
    {
        if (isDying)
        {
            return;
        }

        isDying = true;
        DisableEnemyBehavior();

        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        StartCoroutine(DeathSequence(damageSource));
    }

    private IEnumerator DeathSequence(Transform damageSource)
    {
        if (rb != null && damageSource != null)
        {
            Vector2 knockbackDirection = (transform.position - damageSource.position).normalized;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDirection * deathKnockbackForce, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(deathDelay);

        float elapsedTime = 0f;
        Color[] startColors = new Color[resolvedFadeRenderers.Length];
        Color[] targetColors = new Color[resolvedFadeRenderers.Length];
        for (int i = 0; i < resolvedFadeRenderers.Length; i++)
        {
            SpriteRenderer fadeRenderer = resolvedFadeRenderers[i];
            if (fadeRenderer == null)
            {
                continue;
            }

            startColors[i] = fadeRenderer.color;
            targetColors[i] = new Color(startColors[i].r, startColors[i].g, startColors[i].b, 0f);
        }

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            for (int i = 0; i < resolvedFadeRenderers.Length; i++)
            {
                SpriteRenderer fadeRenderer = resolvedFadeRenderers[i];
                if (fadeRenderer == null)
                {
                    continue;
                }

                fadeRenderer.color = Color.Lerp(startColors[i], targetColors[i], t);
            }

            if (rb != null)
            {
                rb.linearVelocity *= 0.95f;
            }

            yield return null;
        }

        for (int i = 0; i < resolvedFadeRenderers.Length; i++)
        {
            SpriteRenderer fadeRenderer = resolvedFadeRenderers[i];
            if (fadeRenderer == null)
            {
                continue;
            }

            fadeRenderer.color = targetColors[i];
        }

        Destroy(gameObject);
    }

    private void DisableEnemyBehavior()
    {
        SlimeAI slimeAI = GetComponent<SlimeAI>();
        if (slimeAI)
        {
            slimeAI.enabled = false;
        }

        SlimePathFinding slimePathFinding = GetComponent<SlimePathFinding>();
        if (slimePathFinding)
        {
            slimePathFinding.enabled = false;
        }

        EnemyDamageSource enemyDamageSource = GetComponent<EnemyDamageSource>();
        if (enemyDamageSource)
        {
            enemyDamageSource.enabled = false;
        }

        Animator animator = GetComponent<Animator>();
        if (animator && disableAnimatorOnDeath)
        {
            animator.enabled = false;
        }
    }
}
