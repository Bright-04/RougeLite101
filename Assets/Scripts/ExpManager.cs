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
            playerStats.currentExp += amount;
            while(playerStats.currentExp >= playerStats.levelUpExp)
            {
                LevelUp();
            }
        }
    }

    private void LevelUp()
    {
        playerStats.level++;
        playerStats.currentExp -=playerStats.levelUpExp;
        playerStats.levelUpExp = Mathf.RoundToInt(playerStats.levelUpExp * expMultiplier);

        // Tăng stats
        IncreaseStats();

        Debug.Log("Level up");
    }

    private void IncreaseStats()
    {
        // Tăng max stats
        playerStats.maxHP += hpGrowth;
        playerStats.maxMana += manaGrowth;
        playerStats.maxStamina += staminaGrowth;

        // Tăng combat stats
        playerStats.attackDamage += attackDamageGrowth;
        playerStats.abilityPower += abilityPowerGrowth;
        playerStats.defense += defenseGrowth;

        // Tăng regen
        playerStats.hpRegen += hpRegenGrowth;
        playerStats.manaRegen += manaRegenGrowth;
        playerStats.staminaRegen += staminaRegenGrowth;

        Debug.Log($"Stats increased! HP: {playerStats.maxHP}, ATK: {playerStats.attackDamage}");
    }
}
