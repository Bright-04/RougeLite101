using UnityEngine;

/// <summary>
/// This data persists across all runs. It is saved and loaded via JSON/PlayerPrefs.
/// </summary>
[System.Serializable]
public class MetaProgressionData
{
    public float baseMaxHP = 100f;
    public float baseMaxMana = 100f;
    public float baseAttackDamage = 2f;

    // Currencies
    public int souls = 0;
    public int coins = 0;

    // Permanent Unlocks could go here (e.g. HashSet of unlocked weapons)

    public void AddSouls(int amount)
    {
        souls += amount;
    }
}
