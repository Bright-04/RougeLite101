using UnityEngine;

public class LevelingService : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerStats playerStats;
    
    // Survival Stat Growths per Level
    [Header("Survival Growth")]
    public float hpGrowthLevel = 5f;
    public float manaGrowthLevel = 2f;

    private void Start()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (ExpManager.Instance != null)
        {
            ExpManager.Instance.OnLevelUpEvent += HandleLevelUp;
        }
        else
        {
            Debug.LogWarning("LevelingService: ExpManager instance not found. Make sure ExpManager is initialized.");
        }
    }

    private void OnDestroy()
    {
        if (ExpManager.Instance != null)
        {
            ExpManager.Instance.OnLevelUpEvent -= HandleLevelUp;
        }
    }

    private void HandleLevelUp()
    {
        // 1. Grant base survival stats
        playerStats.baseMaxHP += hpGrowthLevel;
        playerStats.baseMaxMana += manaGrowthLevel;

        // 2. Grant Skill Points
        playerStats.skillPoints++;
        
        // Ensure the stats are updated
        playerStats.RecalculateStats();

        // 3. Pause Game and Open Skill Tree UI (UI logic stub)
        Debug.Log("LevelingService: Pausing game and Opening Skill Tree UI...");
        Time.timeScale = 0f;
        
        // Find SkillTreeUI and Open it
        SkillTreeUI ui = FindAnyObjectByType<SkillTreeUI>();
        if (ui != null)
        {
            ui.OpenSkillTree();
        }
        else
        {
            Debug.LogWarning("SkillTreeUI not found in scene!");
        }
    }
}
