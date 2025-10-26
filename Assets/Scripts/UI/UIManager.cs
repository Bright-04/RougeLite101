using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    //public Image hpFill;
    //public Image manaFill;
    //public Image staminaFill;

    private PlayerStats playerStats;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text manaText;

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
    }


    private void Update()
    {
        UpdateHealthUI();
        UpdateManaUI();

        //hpFill.fillAmount = playerStats.currentHP / playerStats.maxHP;
        //manaFill.fillAmount = playerStats.currentMana / playerStats.maxMana;
        //staminaFill.fillAmount = playerStats.currentStamina / playerStats.maxStamina;
    }

    private void UpdateHealthUI()
    {
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
