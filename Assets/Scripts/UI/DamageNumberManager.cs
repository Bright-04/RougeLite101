using UnityEngine;
using RougeLite.Events;

/// <summary>
/// Manages the creation and display of floating damage numbers
/// Listens to damage events and spawns visual feedback
/// </summary>
public class DamageNumberManager : EventBehaviour,
    IEventListener<DamageDealtEvent>,
    IEventListener<PlayerDamagedEvent>,
    IEventListener<PlayerHealedEvent>
{
    [Header("Prefab References")]
    [SerializeField] private GameObject damageNumberPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnOffset = 1f;
    [SerializeField] private float randomOffset = 0.5f;
    
    [Header("Canvas")]
    [SerializeField] private Canvas worldCanvas;

    protected override void Awake()
    {
        base.Awake();
        
        // Subscribe to damage events
        SubscribeToEvent<DamageDealtEvent>(this);
        SubscribeToEvent<PlayerDamagedEvent>(this);
        SubscribeToEvent<PlayerHealedEvent>(this);
        
        // Find or create world canvas for damage numbers
        if (worldCanvas == null)
        {
            worldCanvas = FindFirstObjectByType<Canvas>();
            if (worldCanvas == null)
            {
                Debug.LogWarning("DamageNumberManager: No Canvas found for damage numbers!", this);
            }
        }
    }

    protected override void OnDestroy()
    {
        // Unsubscribe from events
        UnsubscribeFromEvent<DamageDealtEvent>(this);
        UnsubscribeFromEvent<PlayerDamagedEvent>(this);
        UnsubscribeFromEvent<PlayerHealedEvent>(this);
        
        base.OnDestroy();
    }

    public void OnEventReceived(DamageDealtEvent eventData)
    {
        // Show damage number at target position
        if (eventData.Data.target != null)
        {
            Vector3 spawnPosition = eventData.Data.target.transform.position + Vector3.up * spawnOffset;
            SpawnDamageNumber(spawnPosition, eventData.Data.damage, eventData.Data.isCritical, false);
        }
    }

    public void OnEventReceived(PlayerDamagedEvent eventData)
    {
        // Show damage number at player position
        if (eventData.Data.currentHealth >= 0) // Only if not dead
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null && PlayerController.Instance != null)
            {
                player = PlayerController.Instance.gameObject;
            }
            
            if (player != null)
            {
                Vector3 spawnPosition = player.transform.position + Vector3.up * spawnOffset;
                SpawnDamageNumber(spawnPosition, eventData.Data.damage, false, false);
            }
        }
    }

    public void OnEventReceived(PlayerHealedEvent eventData)
    {
        // Show heal number at player position
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null && PlayerController.Instance != null)
        {
            player = PlayerController.Instance.gameObject;
        }
        
        if (player != null)
        {
            Vector3 spawnPosition = player.transform.position + Vector3.up * spawnOffset;
            SpawnDamageNumber(spawnPosition, eventData.Data.damage, false, true);
        }
    }

    private void SpawnDamageNumber(Vector3 position, float damage, bool isCritical, bool isHeal)
    {
        if (damageNumberPrefab == null)
        {
            Debug.LogWarning("DamageNumberManager: No damage number prefab assigned!", this);
            return;
        }
        
        if (worldCanvas == null)
        {
            Debug.LogWarning("DamageNumberManager: No canvas available for damage numbers!", this);
            return;
        }
        
        // Add random offset
        Vector3 randomizedPosition = position + new Vector3(
            Random.Range(-randomOffset, randomOffset),
            Random.Range(-randomOffset * 0.5f, randomOffset * 0.5f),
            0
        );
        
        // Convert world position to canvas position
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(randomizedPosition);
        
        // Spawn damage number
        GameObject damageNumberObj = Instantiate(damageNumberPrefab, worldCanvas.transform);
        damageNumberObj.transform.position = screenPosition;
        
        // Setup the damage number
        DamageNumber damageNumber = damageNumberObj.GetComponent<DamageNumber>();
        if (damageNumber != null)
        {
            damageNumber.Setup(damage, isCritical, isHeal);
        }
    }
}