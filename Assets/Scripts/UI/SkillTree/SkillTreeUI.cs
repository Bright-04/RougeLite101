using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject skillTreePanel;

    private bool isPausedByUI = false;

    private void Start()
    {
        if (skillTreePanel != null)
        {
            skillTreePanel.SetActive(false);
        }
    }

    public void OpenSkillTree()
    {
        if (skillTreePanel != null)
        {
            skillTreePanel.SetActive(true);
        }

        // Pause game (handled by LevelingService already, but fallback just in case)
        if (Time.timeScale > 0)
        {
            Time.timeScale = 0f;
            isPausedByUI = true;
        }
    }

    public void CloseSkillTree()
    {
        if (skillTreePanel != null)
        {
            skillTreePanel.SetActive(false);
        }
        
        // Only unpause if we were the ones who paused it, or universally unpause
        Time.timeScale = 1f;
        isPausedByUI = false;
        
        // Make sure to refresh any HUD UI for points/stats!
    }
}
