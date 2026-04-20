using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private PlayerStats playerStats;
    private EquipmentManager equipmentManager;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text manaText;

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        equipmentManager = FindAnyObjectByType<EquipmentManager>();

        if (playerStats == null)
        {
            Debug.LogError("PlayerBarUI: Could not find PlayerStats!");
        }

        if (equipmentManager == null)
        {
            Debug.LogWarning("PlayerBarUI: Could not find EquipmentManager!");
        }
    }

    private void OnDestroy()
    {
    }

    private void Update()
    {
        UpdateHealthUI();
        UpdateManaUI();     
    }

    private void UpdateHealthUI()
    {
        if (playerStats == null) return;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = playerStats.maxHP;
            healthSlider.value = playerStats.currentHP;
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.FloorToInt(playerStats.currentHP)} / {playerStats.maxHP}";
        }
            
    }

    private void UpdateManaUI()
    {
        if (playerStats == null) return;

        if (manaSlider != null)
        {
            manaSlider.maxValue = playerStats.maxMana;
            manaSlider.value = playerStats.currentMana;
        }

        if (manaText != null)
        {
            manaText.text = $"{Mathf.FloorToInt(playerStats.currentMana)} / {playerStats.maxMana}";
        }
            
    }
}
