using UnityEngine;
using System;

public class ExpManager : MonoBehaviour
{
    public static ExpManager Instance;
    
    [Header("References")]
    public PlayerStats playerStats;

    [Header("Level Settings")]
    public float expMultiplier = 1.5f;

    public event Action OnLevelUpEvent;

    private void Awake()
    {
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
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("ExpManager: PlayerStats not found! Please assign it in the inspector.");
            }
        }
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
        playerStats.currentExp -= playerStats.levelUpExp;
        playerStats.levelUpExp = Mathf.RoundToInt(playerStats.levelUpExp * expMultiplier);

        Debug.Log($"Level up to {playerStats.level}");

        OnLevelUpEvent?.Invoke();
    }
}
