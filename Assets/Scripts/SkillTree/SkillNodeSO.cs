using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSkillNode", menuName = "SkillTree/Skill Node")]
public class SkillNodeSO : ScriptableObject
{
    [Header("Skill Information")]
    public string skillID;
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;
    
    [Header("Requirements")]
    public int cost = 1;
    public List<SkillNodeSO> prerequisites = new List<SkillNodeSO>();
    
    [Header("Stat Boosts")]
    public List<StatModifier> statModifiers = new List<StatModifier>();
    
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(skillID))
        {
            skillID = System.Guid.NewGuid().ToString();
        }
    }
}
