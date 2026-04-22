using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SkillTreeRegistry", menuName = "SkillTree/Skill Tree Registry")]
public class SkillTreeSO : ScriptableObject
{
    public List<SkillNodeSO> allNodes = new List<SkillNodeSO>();

    public SkillNodeSO GetNodeByID(string id)
    {
        return allNodes.Find(node => node.skillID == id);
    }
}
