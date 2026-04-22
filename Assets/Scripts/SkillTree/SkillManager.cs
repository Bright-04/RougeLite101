using UnityEngine;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerStats playerStats;
    public SkillTreeSO skillRegistry;

    public List<string> unlockedSkillIDs = new List<string>();

    private void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }
    }

    public bool CanUnlock(SkillNodeSO node)
    {
        if (node == null) return false;

        // Already unlocked
        if (unlockedSkillIDs.Contains(node.skillID)) return false;

        // Check Points
        if (playerStats.skillPoints < node.cost) return false;

        // Check prerequisites
        foreach (var prereq in node.prerequisites)
        {
            if (prereq != null && !unlockedSkillIDs.Contains(prereq.skillID))
            {
                return false;
            }
        }

        return true;
    }

    public void UnlockSkill(SkillNodeSO node)
    {
        if (!CanUnlock(node)) 
        {
            Debug.LogWarning($"Cannot unlock skill {node.skillName}");
            return;
        }

        playerStats.skillPoints -= node.cost;
        unlockedSkillIDs.Add(node.skillID);

        // Apply modifiers dynamically
        foreach (var mod in node.statModifiers)
        {
            playerStats.AddModifier(mod);
        }

        Debug.Log($"Skill Unlocked: {node.skillName}!");
    }

    public void LoadUnlockedSkills(List<string> savedSkillIDs)
    {
        unlockedSkillIDs.Clear();
        if (savedSkillIDs != null)
        {
            unlockedSkillIDs.AddRange(savedSkillIDs);
        }

        if (skillRegistry == null)
        {
            Debug.LogError("SkillManager: SkillRegistry is missing! Cannot restore skill stats.");
            return;
        }

        // Re-apply all permanent modifiers
        foreach (string id in unlockedSkillIDs)
        {
            SkillNodeSO node = skillRegistry.GetNodeByID(id);
            if (node != null)
            {
                foreach (var mod in node.statModifiers)
                {
                    playerStats.AddModifier(mod);
                }
            }
            else
            {
                Debug.LogWarning($"SkillNode Data not found for ID: {id}");
            }
        }
    }
}
