using UnityEngine;
using RougeLite.Events;
using System.Collections.Generic;
using System.Linq;
using RougeLite.Enemies;
using RougeLite.Player;

public class LightningSpell : EventBehaviour
{
    [SerializeField] private float damage = 20f;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float duration = 2.0f;
    [SerializeField] private float animationSpeed = 12f; // Frames per second

    private SpriteRenderer spriteRenderer;
    private float fadeTimer;
    private Sprite[] lightningSprites;
    private int currentFrame = 0;
    private float frameTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("LightningSpell: No SpriteRenderer found!");
        }
        
        fadeTimer = duration;
        LoadLightningSprites();
    }

    private void LoadLightningSprites()
    {
        // Try to load all lightning sprites from Resources at runtime
        lightningSprites = Resources.LoadAll<Sprite>("Lightning");
        
        if (lightningSprites != null && lightningSprites.Length > 0)
        {
            // Sort sprites by name to ensure correct frame order (Lightning_0, Lightning_1, etc.)
            lightningSprites = lightningSprites.OrderBy(s => ExtractFrameNumber(s.name)).ToArray();
            Debug.Log($"LightningSpell: Loaded {lightningSprites.Length} lightning sprite frames");
        }
        else
        {
            // Fallback: use the current sprite if available
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                lightningSprites = new Sprite[] { spriteRenderer.sprite };
                Debug.Log("LightningSpell: Using single sprite as fallback");
            }
            else
            {
                Debug.LogWarning("LightningSpell: No lightning sprites available");
            }
        }
    }

    private int ExtractFrameNumber(string spriteName)
    {
        // Extract number from "Lightning_X" format
        string[] parts = spriteName.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[1], out int frameNum))
        {
            return frameNum;
        }
        return 0;
    }

    private void Start()
    {
        Debug.Log($"Lightning spell spawned at {transform.position} with {damage} damage and {radius} radius");
        
        // Make sure the sprite is visible
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        // Find enemies in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out SlimeHealth slimeHealth))
            {
                Debug.Log($"Lightning hit {hit.name} for {damage} damage!");
                
                // Broadcast spell damage event for each enemy hit
                var attackData = new AttackData(
                    attacker: PlayerController.Instance != null ? PlayerController.Instance.gameObject : gameObject,
                    target: hit.gameObject,
                    damage: damage,
                    position: transform.position,
                    type: "Lightning",
                    critical: false
                );
                
                var damageEvent = new DamageDealtEvent(attackData, gameObject);
                BroadcastEvent(damageEvent);
                
                slimeHealth.TakeDamage((int)damage);
            }
        }

        Destroy(gameObject, duration);
    }

    private void Update()
    {
        // Update animation frames
        UpdateAnimation();
        
        // Fade out effect over time
        if (spriteRenderer != null && fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, fadeTimer / duration);
            Color currentColor = spriteRenderer.color;
            currentColor.a = alpha;
            spriteRenderer.color = currentColor;
        }
    }

    private void UpdateAnimation()
    {
        if (spriteRenderer == null)
            return;

        frameTimer += Time.deltaTime;
        
        // If we have multiple sprites, cycle through them
        if (lightningSprites != null && lightningSprites.Length > 1)
        {
            if (frameTimer >= 1f / animationSpeed)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % lightningSprites.Length;
                spriteRenderer.sprite = lightningSprites[currentFrame];
            }
        }
        else
        {
            // Create a dynamic lightning effect with color and scale changes
            if (frameTimer >= 1f / animationSpeed)
            {
                frameTimer = 0f;
                
                // Lightning flicker effect with random intensity
                float intensity = Random.Range(0.7f, 1.3f);
                Color lightningColor = Color.Lerp(Color.white, Color.cyan, Mathf.PingPong(Time.time * 8f, 1f));
                
                // Keep the alpha from the fade effect
                lightningColor.a = spriteRenderer.color.a;
                lightningColor *= intensity;
                spriteRenderer.color = lightningColor;
                
                // Scale variation for lightning bolt effect
                float scaleVariation = Random.Range(0.9f, 1.1f);
                float baseScale = 3f;
                transform.localScale = Vector3.one * (baseScale * scaleVariation);
                
                // Small random rotation for dynamic effect
                transform.rotation = Quaternion.Euler(0, 0, Random.Range(-5f, 5f));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
