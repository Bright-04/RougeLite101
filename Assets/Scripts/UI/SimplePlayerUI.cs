using UnityEngine;
using RougeLite.Player;
using UnityEngine.UI;
using RougeLite.Events;
using TMPro;

public class SimplePlayerUI : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    
    [Header("Mana UI")]
    public Slider manaSlider;
    public TextMeshProUGUI manaText;
    
    [Header("Spell UI")]
    public TextMeshProUGUI[] spellTexts = new TextMeshProUGUI[3]; // For 3 spells
    
    private PlayerStats playerStats;
    private SpellCaster spellCaster;
    
    void Start()
    {
        // Find player components using the new API
        playerStats = FindFirstObjectByType<PlayerStats>();
        spellCaster = FindFirstObjectByType<SpellCaster>();
        
        if (playerStats == null)
        {
            Debug.LogError("SimplePlayerUI: PlayerStats not found!");
        }
        
        if (spellCaster == null)
        {
            Debug.LogError("SimplePlayerUI: SpellCaster not found!");
        }
        
        // Setup UI elements if they don't exist
        SetupUI();

        // Subscribe to player events (event-driven health/mana UI)
        var eventManager = FindFirstObjectByType<EventManager>();
        if (eventManager != null)
        {
            eventManager.RegisterAction<PlayerDamagedEvent>(OnPlayerDamaged);
            eventManager.RegisterAction<PlayerHealedEvent>(OnPlayerHealed);
            eventManager.RegisterAction<PlayerManaUsedEvent>(OnPlayerManaUsed);
            eventManager.RegisterAction<PlayerManaRestoredEvent>(OnPlayerManaRestored);
        }
    }
    
    void Update()
    {
        // Only update spell cooldown visuals per frame; health/mana are event-driven
        if (spellCaster != null)
            UpdateSpellUI();
    }
    
    void SetupUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            // Create canvas if it doesn't exist
            GameObject canvasGO = new GameObject("PlayerUI Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create a background panel for the UI
        CreateUIBackground(canvas.transform);
        
        // Create health UI if not assigned
        if (healthSlider == null)
        {
            healthSlider = CreateSlider(canvas.transform, "Health Bar", new Vector2(20, 120), new Vector2(200, 25), Color.red);
        }
        
        if (healthText == null)
        {
            healthText = CreateText(canvas.transform, "Health Text", new Vector2(230, 120), "HP: 100/100\n(100%)", 14, Color.white);
        }
        
        // Create mana UI if not assigned
        if (manaSlider == null)
        {
            manaSlider = CreateSlider(canvas.transform, "Mana Bar", new Vector2(20, 85), new Vector2(200, 25), Color.cyan);
        }
        
        if (manaText == null)
        {
            manaText = CreateText(canvas.transform, "Mana Text", new Vector2(230, 85), "MP: 50/50\n(100%)", 14, Color.white);
        }
        
        // Create spell UI with better spacing
        for (int i = 0; i < spellTexts.Length; i++)
        {
            if (spellTexts[i] == null)
            {
                spellTexts[i] = CreateText(canvas.transform, $"Spell {i + 1}", 
                    new Vector2(20 + (i * 120), 40), $"[{i + 1}] Empty", 12, Color.white);
            }
        }
    }
    
    Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size, Color fillColor)
    {
        GameObject sliderGO = new GameObject(name);
        sliderGO.transform.SetParent(parent);
        
        RectTransform rect = sliderGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0); // Bottom left anchor
        rect.anchorMax = new Vector2(0, 0);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Slider slider = sliderGO.AddComponent<Slider>();
        
        // Create background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark semi-transparent background
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        
        // Set slider references
        slider.fillRect = fillRect;
        slider.value = 1f;
        
        return slider;
    }
    
    TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, string text, float fontSize = 14, Color? textColor = null)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent);
        
        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0); // Bottom left anchor
        rect.anchorMax = new Vector2(0, 0);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(110, 40); // Fixed size for consistent layout
        
        TextMeshProUGUI textComp = textGO.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.color = textColor ?? Color.white;
        textComp.fontStyle = FontStyles.Bold;
        textComp.alignment = TextAlignmentOptions.Left; // Left align for better readability
        textComp.textWrappingMode = TextWrappingModes.Normal; // Updated from enableWordWrapping
        textComp.overflowMode = TextOverflowModes.Overflow;
        
        // Add outline for better readability
        textComp.outlineWidth = 0.2f;
        textComp.outlineColor = Color.black;
        
        return textComp;
    }
    
    void CreateUIBackground(Transform parent)
    {
        GameObject bgPanel = new GameObject("UI Background Panel");
        bgPanel.transform.SetParent(parent);
        
        RectTransform rect = bgPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0); // Bottom left anchor
        rect.anchorMax = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(10, 10); // Small padding from bottom-left corner
        rect.sizeDelta = new Vector2(380, 150); // Size to encompass all UI elements
        
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.4f); // Semi-transparent black background
        
        // Ensure background is behind everything else
        bgPanel.transform.SetAsFirstSibling();
    }
    
    void UpdateHealthUI()
    {
        if (playerStats == null) return;
        
        if (healthSlider != null)
        {
            healthSlider.value = playerStats.currentHP / playerStats.maxHP;
        }
        
        if (healthText != null)
        {
            int currentHP = Mathf.CeilToInt(playerStats.currentHP);
            int maxHP = Mathf.CeilToInt(playerStats.maxHP);
            float percentage = (playerStats.currentHP / playerStats.maxHP) * 100f;
            healthText.text = $"HP: {currentHP}/{maxHP}\n({percentage:F0}%)";
            
            // Color health text based on health percentage
            if (percentage > 75f)
                healthText.color = Color.green;
            else if (percentage > 50f)
                healthText.color = Color.yellow;
            else if (percentage > 25f)
                healthText.color = new Color(1f, 0.5f, 0f); // Orange
            else
                healthText.color = Color.red;
        }
    }
    
    void UpdateManaUI()
    {
        if (playerStats == null) return;
        
        if (manaSlider != null)
        {
            manaSlider.value = playerStats.currentMana / playerStats.maxMana;
        }
        
        if (manaText != null)
        {
            int currentMana = Mathf.CeilToInt(playerStats.currentMana);
            int maxMana = Mathf.CeilToInt(playerStats.maxMana);
            float percentage = (playerStats.currentMana / playerStats.maxMana) * 100f;
            manaText.text = $"MP: {currentMana}/{maxMana}\n({percentage:F0}%)";
            
            // Color mana text based on mana percentage
            if (percentage > 50f)
                manaText.color = Color.cyan;
            else if (percentage > 25f)
                manaText.color = Color.blue;
            else
                manaText.color = new Color(0.5f, 0.5f, 1f); // Light blue when low
        }
    }

    // Event handlers for event-driven updates
    void OnPlayerDamaged(PlayerDamagedEvent e) { UpdateHealthUI(); }
    void OnPlayerHealed(PlayerHealedEvent e) { UpdateHealthUI(); }
    void OnPlayerManaUsed(PlayerManaUsedEvent e) { UpdateManaUI(); }
    void OnPlayerManaRestored(PlayerManaRestoredEvent e) { UpdateManaUI(); }

    void OnDestroy()
    {
        var eventManager = FindFirstObjectByType<EventManager>();
        if (eventManager != null)
        {
            eventManager.UnregisterAction<PlayerDamagedEvent>(OnPlayerDamaged);
            eventManager.UnregisterAction<PlayerHealedEvent>(OnPlayerHealed);
            eventManager.UnregisterAction<PlayerManaUsedEvent>(OnPlayerManaUsed);
            eventManager.UnregisterAction<PlayerManaRestoredEvent>(OnPlayerManaRestored);
        }
    }
    
    void UpdateSpellUI()
    {
        if (spellCaster == null || spellTexts == null) return;
        
        var cooldowns = spellCaster.CooldownTimers;
        
        for (int i = 0; i < spellTexts.Length && i < spellCaster.spellSlots.Length; i++)
        {
            if (spellTexts[i] == null) continue;
            
            var spell = spellCaster.spellSlots[i];
            string keyText = (i + 1).ToString();
            
            if (spell != null)
            {
                float cooldownTime = cooldowns != null && i < cooldowns.Length ? cooldowns[i] : 0f;
                
                if (cooldownTime > 0)
                {
                    // Show cooldown
                    spellTexts[i].text = $"[{keyText}] {spell.spellName}\n{cooldownTime:F1}s";
                    spellTexts[i].color = new Color(0.7f, 0.7f, 0.7f, 1f); // Grayed out during cooldown
                }
                else
                {
                    // Show available spell
                    spellTexts[i].text = $"[{keyText}] {spell.spellName}\n{spell.manaCost} MP";
                    
                    // Color based on mana availability
                    if (playerStats != null && playerStats.currentMana >= spell.manaCost)
                    {
                        spellTexts[i].color = Color.green; // Available
                    }
                    else
                    {
                        spellTexts[i].color = Color.red; // Not enough mana
                    }
                }
            }
            else
            {
                spellTexts[i].text = $"[{keyText}] Empty";
                spellTexts[i].color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
    }
}
