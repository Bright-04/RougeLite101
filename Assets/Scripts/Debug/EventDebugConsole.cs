using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RougeLite.Events;

/// <summary>
/// Debug console that displays recent game events
/// Useful for monitoring event system activity during development
/// </summary>
public class EventDebugConsole : EventBehaviour,
    IEventListener<PlayerDamagedEvent>,
    IEventListener<PlayerHealedEvent>,
    IEventListener<PlayerDeathEvent>,
    IEventListener<EnemyDeathEvent>,
    IEventListener<AttackPerformedEvent>,
    IEventListener<SpellCastEvent>,
    IEventListener<DamageDealtEvent>
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Text logText;
    [SerializeField] private Button clearButton;
    [SerializeField] private Toggle enableToggle;
    
    [Header("Debug Settings")]
    [SerializeField] private int maxLogEntries = 50;
    [SerializeField] private bool startEnabled = true;
    [SerializeField] private bool showTimestamps = true;
    
    private Queue<string> logEntries = new Queue<string>();
    private bool isEnabled;

    protected override void Awake()
    {
        base.Awake();
        
        isEnabled = startEnabled;
        
        // Subscribe to all events we want to monitor
        SubscribeToEvent<PlayerDamagedEvent>(this);
        SubscribeToEvent<PlayerHealedEvent>(this);
        SubscribeToEvent<PlayerDeathEvent>(this);
        SubscribeToEvent<EnemyDeathEvent>(this);
        SubscribeToEvent<AttackPerformedEvent>(this);
        SubscribeToEvent<SpellCastEvent>(this);
        SubscribeToEvent<DamageDealtEvent>(this);
        
        // Setup UI
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearLog);
        }
        
        if (enableToggle != null)
        {
            enableToggle.isOn = isEnabled;
            enableToggle.onValueChanged.AddListener(SetEnabled);
        }
        
        // Initial state
        if (gameObject != null)
        {
            gameObject.SetActive(isEnabled);
        }
    }

    protected override void OnDestroy()
    {
        // Unsubscribe from events
        UnsubscribeFromEvent<PlayerDamagedEvent>(this);
        UnsubscribeFromEvent<PlayerHealedEvent>(this);
        UnsubscribeFromEvent<PlayerDeathEvent>(this);
        UnsubscribeFromEvent<EnemyDeathEvent>(this);
        UnsubscribeFromEvent<AttackPerformedEvent>(this);
        UnsubscribeFromEvent<SpellCastEvent>(this);
        UnsubscribeFromEvent<DamageDealtEvent>(this);
        
        base.OnDestroy();
    }

    public void OnEventReceived(PlayerDamagedEvent eventData)
    {
        LogEvent($"Player damaged: -{eventData.Data.damage:F1} HP (Health: {eventData.Data.currentHealth:F1}/{eventData.Data.maxHealth:F1})");
    }

    public void OnEventReceived(PlayerHealedEvent eventData)
    {
        LogEvent($"Player healed: +{eventData.Data.damage:F1} HP (Health: {eventData.Data.currentHealth:F1}/{eventData.Data.maxHealth:F1})");
    }

    public void OnEventReceived(PlayerDeathEvent eventData)
    {
        LogEvent("<color=red><b>PLAYER DIED</b></color>");
    }

    public void OnEventReceived(EnemyDeathEvent eventData)
    {
        LogEvent($"<color=green>{eventData.Data.enemyType} defeated!</color>");
    }

    public void OnEventReceived(AttackPerformedEvent eventData)
    {
        string attackerName = eventData.Data.attacker != null ? eventData.Data.attacker.name : "Unknown";
        LogEvent($"{attackerName} performed {eventData.Data.attackType} attack");
    }

    public void OnEventReceived(SpellCastEvent eventData)
    {
        string casterName = eventData.Data.caster != null ? eventData.Data.caster.name : "Unknown";
        LogEvent($"<color=cyan>{casterName} cast {eventData.Data.spellName}</color>");
    }

    public void OnEventReceived(DamageDealtEvent eventData)
    {
        string attackerName = eventData.Data.attacker != null ? eventData.Data.attacker.name : "Unknown";
        string targetName = eventData.Data.target != null ? eventData.Data.target.name : "Unknown";
        string critText = eventData.Data.isCritical ? " <color=yellow><b>CRIT!</b></color>" : "";
        
        LogEvent($"{attackerName} → {targetName}: {eventData.Data.damage:F1} {eventData.Data.attackType} damage{critText}");
    }

    private void LogEvent(string message)
    {
        if (!isEnabled) return;
        
        string timestamp = showTimestamps ? $"[{Time.time:F1}s] " : "";
        string fullMessage = timestamp + message;
        
        logEntries.Enqueue(fullMessage);
        
        // Keep only the most recent entries
        while (logEntries.Count > maxLogEntries)
        {
            logEntries.Dequeue();
        }
        
        UpdateLogDisplay();
    }

    private void UpdateLogDisplay()
    {
        if (logText != null)
        {
            logText.text = string.Join("\n", logEntries.ToArray());
            
            // Auto-scroll to bottom
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    private void ClearLog()
    {
        logEntries.Clear();
        UpdateLogDisplay();
        LogEvent("<i>Log cleared</i>");
    }

    private void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (gameObject != null)
        {
            gameObject.SetActive(enabled);
        }
    }

    /// <summary>
    /// Toggle the debug console visibility
    /// </summary>
    public void ToggleConsole()
    {
        SetEnabled(!isEnabled);
        if (enableToggle != null)
        {
            enableToggle.isOn = isEnabled;
        }
    }
}