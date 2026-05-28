using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsMenuUI : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerStats playerStats;

    [Header("UI Text Elements")]
    public TMP_Text levelText;
    public TMP_Text expText;

    [Header("Core Stats")]
    public TMP_Text maxHpText;
    public TMP_Text maxManaText;
    public TMP_Text maxStaminaText;

    [Header("Combat Stats")]
    public TMP_Text attackDamageText;
    public TMP_Text abilityPowerText;
    public TMP_Text defenseText;
    public TMP_Text critChanceText;
    public TMP_Text critDamageText;
    public TMP_Text luckText;

    [Header("Regeneration Stats")]
    public TMP_Text hpRegenText;
    public TMP_Text manaRegenText;
    public TMP_Text staminaRegenText;

    [Header("Progress Bars")]
    public Slider expBar;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        // Tự động tìm PlayerStats nếu chưa được gán
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("StatsMenuUI: PlayerStats not found! Please assign it in the inspector.");
            }
        }
    }

    private void Update()
    {
        // Cập nhật real-time khi menu đang mở
        if (gameObject.activeInHierarchy)
        {
            UpdateStatsDisplay();
        }
    }

    private void UpdateStatsDisplay()
    {
        if (playerStats == null) return;

        // Level & Experience
        if (levelText != null)
            levelText.text = $"Level: {playerStats.GetLevel()}";

        if (expText != null)
            expText.text = $"EXP: {playerStats.GetCurrentExp():F0} / {playerStats.GetLevelUpExp():F0}";

        // Core Stats
        if (maxHpText != null)
            maxHpText.text = $"Max HP: {playerStats.GetMaxHP():F0}";

        if (maxManaText != null)
            maxManaText.text = $"Max Mana: {playerStats.GetMaxMana():F0}";

        if (maxStaminaText != null)
            maxStaminaText.text = $"Max Stamina: {playerStats.GetMaxStamina():F0}";

        // Combat Stats
        if (attackDamageText != null)
            attackDamageText.text = $"Attack Damage: {playerStats.GetAttackDamage():F0}";

        if (abilityPowerText != null)
            abilityPowerText.text = $"Ability Power: {playerStats.GetAbilityPower():F0}";

        if (defenseText != null)
            defenseText.text = $"Defense: {playerStats.GetDefense():F0}";

        if (critChanceText != null)
            critChanceText.text = $"Crit Chance: {(playerStats.GetCritChance() * 100f):F1}%";

        if (critDamageText != null)
            critDamageText.text = $"Crit Damage: {(playerStats.GetCritDamage() * 100f):F0}%";

        if (luckText != null)
            luckText.text = $"Luck: {playerStats.GetLuck():F0}";

        // Regeneration Stats
        if (hpRegenText != null)
            hpRegenText.text = $"HP Regen: {playerStats.GetRegenHP():F1}/s";

        if (manaRegenText != null)
            manaRegenText.text = $"Mana Regen: {playerStats.GetRegenMana():F1}/s";

        if (staminaRegenText != null)
            staminaRegenText.text = $"Stamina Regen: {playerStats.GetRegenStamina():F1}/s";

        // EXP Bar
        if (expBar != null)
        {
            expBar.maxValue = playerStats.GetLevelUpExp();
            expBar.value = playerStats.GetCurrentExp();
        }
    }
}
