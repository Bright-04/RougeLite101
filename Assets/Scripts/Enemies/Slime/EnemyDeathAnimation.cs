using System.Collections;
using UnityEngine;

/// <summary>
/// Handles death animation for enemies: knockback + fade out
/// Attach this to any enemy that needs death animation
/// </summary>
public class EnemyDeathAnimation : MonoBehaviour
{
    [Header("Death Animation Settings")]
    [SerializeField] private float deathKnockbackForce = 20f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float deathDelay = 0.1f; // Small delay before starting fade
    
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private bool isDying = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();
    }

    /// <summary>
    /// Plays the death animation: knockback + fade out, then destroys the game object
    /// </summary>
    /// <param name="damageSource">The transform that dealt the killing blow (for knockback direction)</param>
    public void PlayDeathAnimation(Transform damageSource)
    {
        if (isDying) return; // Prevent multiple death animations
        isDying = true;

        // Disable AI and movement
        DisableEnemyBehavior();

        // Disable colliders so enemy doesn't block or interact anymore
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Start the death sequence
        StartCoroutine(DeathSequence(damageSource));
    }

    private IEnumerator DeathSequence(Transform damageSource)
    {
        // Apply death knockback
        if (rb != null && damageSource != null)
        {
            Vector2 knockbackDirection = (transform.position - damageSource.position).normalized;
            rb.linearVelocity = Vector2.zero; // Clear any existing velocity
            rb.AddForce(knockbackDirection * deathKnockbackForce, ForceMode2D.Impulse);
        }

        // Small delay before fading
        yield return new WaitForSeconds(deathDelay);

        // Fade out
        float elapsedTime = 0f;
        Color startColor = spriteRenderer.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
            
            // Optionally slow down the enemy as it fades
            if (rb != null)
            {
                rb.linearVelocity *= 0.95f; // Gradually reduce velocity
            }
            
            yield return null;
        }

        // Ensure fully transparent
        spriteRenderer.color = targetColor;

        // Destroy the game object
        Destroy(gameObject);
    }

    private void DisableEnemyBehavior()
    {
        // Disable common enemy components
        var slimeAI = GetComponent<SlimeAI>();
        if (slimeAI) slimeAI.enabled = false;

        var slimePathFinding = GetComponent<SlimePathFinding>();
        if (slimePathFinding) slimePathFinding.enabled = false;

        // Disable skeleton components
        var skeletonAI = GetComponent<SkeletonAI>();
        if (skeletonAI)
        {
            skeletonAI.StopAI(); // Use StopAI to clean up visuals
        }

        var skeletonPathFinding = GetComponent<SkeletonPathFinding>();
        if (skeletonPathFinding) skeletonPathFinding.enabled = false;

        var enemyDamageSource = GetComponent<EnemyDamageSource>();
        if (enemyDamageSource) enemyDamageSource.enabled = false;

        // Disable animator to freeze the sprite
        var animator = GetComponent<Animator>();
        if (animator) animator.enabled = false;
    }
}
