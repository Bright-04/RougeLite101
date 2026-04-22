using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillNodeUI : MonoBehaviour
{
    [Header("Node Data")]
    public SkillNodeSO nodeData;

    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public Button unlockButton;
    
    [Header("Managers")]
    public SkillManager skillManager;

    private void Start()
    {
        if (skillManager == null)
        {
            skillManager = FindAnyObjectByType<SkillManager>();
        }

        SetupUI();

        if (unlockButton != null)
        {
            unlockButton.onClick.AddListener(OnUnlockClicked);
        }
    }

    private void SetupUI()
    {
        if (nodeData == null) return;

        if (iconImage != null) iconImage.sprite = nodeData.icon;
        if (nameText != null) nameText.text = nodeData.skillName;

        RefreshState();
    }

    public void RefreshState()
    {
        if (nodeData == null || skillManager == null) return;

        bool isUnlocked = skillManager.unlockedSkillIDs.Contains(nodeData.skillID);
        bool canUnlock = skillManager.CanUnlock(nodeData);

        if (isUnlocked)
        {
            // Visuals for unlocked state (e.g. green color)
            unlockButton.interactable = false;
        }
        else if (canUnlock)
        {
            // Visuals for available state
            unlockButton.interactable = true;
        }
        else
        {
            // Visuals for locked state (e.g. grey color)
            unlockButton.interactable = false;
        }
    }

    private void OnUnlockClicked()
    {
        if (skillManager != null && nodeData != null)
        {
            skillManager.UnlockSkill(nodeData);
            
            // Refresh this node
            RefreshState();
            
            // Note: Ideally, you'd raise an event so ALL nodes refresh their states,
            // revealing newly unlockable child nodes.
            var allNodes = FindObjectsByType<SkillNodeUI>(FindObjectsSortMode.None);
            foreach (var node in allNodes)
            {
                node.RefreshState();
            }
        }
    }
}
