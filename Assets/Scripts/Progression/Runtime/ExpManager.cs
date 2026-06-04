using UnityEngine;

public class ExpManager : MonoBehaviour
{
    public static ExpManager Instance; // Singleton để dễ access
    [Header("References")]
    public PlayerStats playerStats;

    [Header("Level Settings")]
    public float expMultiplier = 1.5f; // Mỗi level tăng thêm 50% EXP cần

    [Header("Stat Growth Per Level")]
    public float hpGrowth = 10f;
    public float manaGrowth = 20f;
    public float staminaGrowth = 5f;
    public float attackDamageGrowth = 2f;
    public float abilityPowerGrowth = 1f;
    public float defenseGrowth = 1f;
    public float hpRegenGrowth = 0.1f;
    public float manaRegenGrowth = 0.2f;
    public float staminaRegenGrowth = 0.1f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Tự động tìm PlayerStats nếu chưa được gán
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("ExpManager: PlayerStats not found! Please assign it in the inspector.");
            }
        }
        //GainExperience(100);
    }

    public void GainExperience(float amount)
    {
        if (playerStats != null)
        {
            playerStats.IncreaseExp(amount);
            while(playerStats.GetCurrentExp() >= playerStats.GetLevelUpExp())
            {
                LevelUp();
            }
        }
    }

    private void LevelUp()
    {
        playerStats.IncreaseLevel(expMultiplier);

        // Tăng stats
        IncreaseStats();

        Debug.Log("Level up");
    }

    private void IncreaseStats()
    {
        // Tăng max stats
        playerStats.LevelUpHP(hpGrowth);
        playerStats.LevelUpMana(manaGrowth);
        playerStats.LevelUpStamina(staminaGrowth);

        // Tăng combat stats
        playerStats.LevelUpAttackDamage(attackDamageGrowth);
        playerStats.LevelUpAbilityPower(abilityPowerGrowth);
        playerStats.LevelUpDefense(defenseGrowth);

        // Tăng regen
        playerStats.LevelUpHPRegen(hpRegenGrowth);
        playerStats.LevelUpManaRegen(manaRegenGrowth);
        playerStats.LevelUpStaminaRegen(staminaRegenGrowth);

        Debug.Log($"Stats increased!");
    }
}
