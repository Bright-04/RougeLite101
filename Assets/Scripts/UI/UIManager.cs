using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Image hpFill;
    public Image manaFill;
    public Image staminaFill;

    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
    }


    private void Update()
    {
        hpFill.fillAmount = playerStats.currentHP / playerStats.maxHP;
        manaFill.fillAmount = playerStats.currentMana / playerStats.maxMana;
        staminaFill.fillAmount = playerStats.currentStamina / playerStats.maxStamina;
    }
}
