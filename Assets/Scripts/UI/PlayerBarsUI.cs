using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private PlayerStats playerStats;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text manaText;

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerBarUI: Could not find PlayerStats!");
        }
        else
        {
            Debug.Log("PlayerBarUI: Found PlayerStats successfully");
        }
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
