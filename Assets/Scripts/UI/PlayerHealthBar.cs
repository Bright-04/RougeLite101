using UnityEngine;
using UnityEngine.UI;
using RougeLite.Events;
using RougeLite.Player;

/// <summary>
/// Simple UI health bar that listens to player health events
/// Demonstrates event-driven UI updates
/// </summary>
public class PlayerHealthBar : EventBehaviour, 
    IEventListener<PlayerDamagedEvent>,
    IEventListener<PlayerHealedEvent>,
    IEventListener<PlayerDeathEvent>
{
    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Text healthText;
    [SerializeField] private Image fillImage;
    
    [Header("Visual Settings")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;

    private PlayerStats playerStats;

    protected override void Awake()
    {
        base.Awake();
        
        // Subscribe to player health events
        SubscribeToEvent<PlayerDamagedEvent>(this);
        SubscribeToEvent<PlayerHealedEvent>(this);
        SubscribeToEvent<PlayerDeathEvent>(this);
        
        // Find player stats
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerHealthBar: No PlayerStats found in scene!", this);
        }
    }

    private void Start()
    {
        // Initialize health bar with current values
        if (playerStats != null)
        {
            UpdateHealthBar(playerStats.currentHP, playerStats.maxHP);
        }
    }

    protected override void OnDestroy()
    {
        // Unsubscribe from events
        UnsubscribeFromEvent<PlayerDamagedEvent>(this);
        UnsubscribeFromEvent<PlayerHealedEvent>(this);
        UnsubscribeFromEvent<PlayerDeathEvent>(this);
        
        base.OnDestroy();
    }

    public void OnEventReceived(PlayerDamagedEvent eventData)
    {
        UpdateHealthBar(eventData.Data.currentHealth, eventData.Data.maxHealth);
        
        // Flash red when taking damage
        if (fillImage != null)
        {
            StartCoroutine(FlashDamage());
        }
        
        // Player damaged - logging disabled for cleaner console
    }

    public void OnEventReceived(PlayerHealedEvent eventData)
    {
        UpdateHealthBar(eventData.Data.currentHealth, eventData.Data.maxHealth);
        
        // Flash green when healing
        if (fillImage != null)
        {
            StartCoroutine(FlashHeal());
        }
        
        // Player healed - logging disabled for cleaner console
    }

    public void OnEventReceived(PlayerDeathEvent eventData)
    {
        UpdateHealthBar(0f, eventData.Data.maxHealth);
        
        // Death visual effect
        if (fillImage != null)
        {
            fillImage.color = Color.black;
        }
        
        // Player died - logging disabled for cleaner console
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{Mathf.Ceil(maxHealth)}";
        }
        
        if (fillImage != null)
        {
            // Change color based on health percentage
            float healthPercentage = currentHealth / maxHealth;
            if (healthPercentage <= lowHealthThreshold)
            {
                fillImage.color = lowHealthColor;
            }
            else
            {
                fillImage.color = Color.Lerp(lowHealthColor, healthyColor, 
                    (healthPercentage - lowHealthThreshold) / (1f - lowHealthThreshold));
            }
        }
    }

    private System.Collections.IEnumerator FlashDamage()
    {
        Color originalColor = fillImage.color;
        fillImage.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        fillImage.color = originalColor;
    }

    private System.Collections.IEnumerator FlashHeal()
    {
        Color originalColor = fillImage.color;
        fillImage.color = Color.cyan;
        yield return new WaitForSeconds(0.1f);
        fillImage.color = originalColor;
    }
}
