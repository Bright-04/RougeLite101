using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RougeLite.Events;
using RougeLite.Player;

public class PlayerUIManager : EventBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Mana UI")]
    [SerializeField] private Slider manaBar;
    [SerializeField] private TextMeshProUGUI manaText;
    
    [Header("Spell UI")]
    [SerializeField] private SpellSlotUI[] spellSlots; // Array for 3 spell slots
    
    private PlayerStats playerStats;
    private SpellCaster spellCaster;
    
    [System.Serializable]
    public class SpellSlotUI
    {
        public TextMeshProUGUI keyText;        // Shows "1", "2", "3"
        public TextMeshProUGUI spellNameText;  // Shows spell name
        public TextMeshProUGUI cooldownText;   // Shows cooldown timer
        public Image cooldownOverlay;          // Visual cooldown indicator
        public Image spellIcon;                // Optional spell icon
    }
    
    protected override void Awake()
    {
        base.Awake();
        
        // Find player components
        playerStats = FindFirstObjectByType<PlayerStats>();
        spellCaster = FindFirstObjectByType<SpellCaster>();
        
        if (playerStats == null)
        {
            Debug.LogError("PlayerUIManager: PlayerStats not found in scene!");
        }
        
        if (spellCaster == null)
        {
            Debug.LogError("PlayerUIManager: SpellCaster not found in scene!");
        }
    }
    
    private void Start()
    {
        // Subscribe to events using Action delegates
        RegisterForEvent<PlayerDamagedEvent>(OnPlayerDamaged);
        RegisterForEvent<PlayerHealedEvent>(OnPlayerHealed);
        RegisterForEvent<PlayerManaUsedEvent>(OnPlayerManaUsed);
        RegisterForEvent<PlayerManaRestoredEvent>(OnPlayerManaRestored);
        
        // Initialize UI
        UpdateHealthUI();
        UpdateManaUI();
        InitializeSpellUI();
    }
    
    private void Update()
    {
        // Update UI every frame (could be optimized to only update when values change)
        UpdateHealthUI();
        UpdateManaUI();
        UpdateSpellCooldowns();
    }
    
    private void UpdateHealthUI()
    {
        if (playerStats == null) return;
        
        if (healthBar != null)
        {
            healthBar.value = playerStats.currentHP / playerStats.maxHP;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(playerStats.currentHP)}/{Mathf.Ceil(playerStats.maxHP)}";
        }
    }
    
    private void UpdateManaUI()
    {
        if (playerStats == null) return;
        
        if (manaBar != null)
        {
            manaBar.value = playerStats.currentMana / playerStats.maxMana;
        }
        
        if (manaText != null)
        {
            manaText.text = $"{Mathf.Ceil(playerStats.currentMana)}/{Mathf.Ceil(playerStats.maxMana)}";
        }
    }
    
    private void InitializeSpellUI()
    {
        if (spellCaster == null || spellSlots == null) return;
        
        for (int i = 0; i < spellSlots.Length && i < spellCaster.spellSlots.Length; i++)
        {
            var spellSlotUI = spellSlots[i];
            var spell = spellCaster.spellSlots[i];
            
            // Set key text
            if (spellSlotUI.keyText != null)
            {
                spellSlotUI.keyText.text = (i + 1).ToString(); // "1", "2", "3"
            }
            
            // Set spell name
            if (spellSlotUI.spellNameText != null)
            {
                spellSlotUI.spellNameText.text = spell != null ? spell.spellName : "Empty";
            }
            
            // Initialize cooldown overlay
            if (spellSlotUI.cooldownOverlay != null)
            {
                spellSlotUI.cooldownOverlay.fillAmount = 0f;
            }
        }
    }
    
    private void UpdateSpellCooldowns()
    {
        if (spellCaster == null || spellSlots == null) return;
        
        // Access cooldown timers through reflection or make them public
        var cooldownTimers = GetCooldownTimers();
        if (cooldownTimers == null) return;
        
        for (int i = 0; i < spellSlots.Length && i < cooldownTimers.Length; i++)
        {
            var spellSlotUI = spellSlots[i];
            var cooldownTime = cooldownTimers[i];
            var spell = i < spellCaster.spellSlots.Length ? spellCaster.spellSlots[i] : null;
            
            if (cooldownTime > 0 && spell != null)
            {
                // Show cooldown timer
                if (spellSlotUI.cooldownText != null)
                {
                    spellSlotUI.cooldownText.text = cooldownTime.ToString("F1") + "s";
                }
                
                // Update cooldown overlay
                if (spellSlotUI.cooldownOverlay != null)
                {
                    spellSlotUI.cooldownOverlay.fillAmount = cooldownTime / spell.cooldown;
                }
            }
            else
            {
                // Clear cooldown display
                if (spellSlotUI.cooldownText != null)
                {
                    spellSlotUI.cooldownText.text = "";
                }
                
                if (spellSlotUI.cooldownOverlay != null)
                {
                    spellSlotUI.cooldownOverlay.fillAmount = 0f;
                }
            }
        }
    }
    
    // Helper method to get cooldown timers
    private float[] GetCooldownTimers()
    {
        if (spellCaster == null) return null;
        return spellCaster.CooldownTimers;
    }
    
    protected override void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        UnregisterFromEvent<PlayerDamagedEvent>(OnPlayerDamaged);
        UnregisterFromEvent<PlayerHealedEvent>(OnPlayerHealed);
        UnregisterFromEvent<PlayerManaUsedEvent>(OnPlayerManaUsed);
        UnregisterFromEvent<PlayerManaRestoredEvent>(OnPlayerManaRestored);
        
        base.OnDestroy();
    }
    
    // Event handlers
    private void OnPlayerDamaged(PlayerDamagedEvent eventData)
    {
        UpdateHealthUI();
    }
    
    private void OnPlayerHealed(PlayerHealedEvent eventData)
    {
        UpdateHealthUI();
    }
    
    private void OnPlayerManaUsed(PlayerManaUsedEvent eventData)
    {
        UpdateManaUI();
    }
    
    private void OnPlayerManaRestored(PlayerManaRestoredEvent eventData)
    {
        UpdateManaUI();
    }
}
