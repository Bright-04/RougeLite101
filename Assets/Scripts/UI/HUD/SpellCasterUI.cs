using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellCasterUI : MonoBehaviour
{
    [Header("References")]
    private SpellCaster spellCaster;

    [Header("UI Elements")]
    public SpellSlotUI[] spellSlotUIs; // 3 slots UI

    [System.Serializable]
    public class SpellSlotUI
    {
        public Image spellIcon;          // Icon của spell     
        public TextMeshProUGUI cooldownText; // Text hiển thị số giây còn lại
        public Image cooldownRadial;     // Radial fill cho cooldown (optional)
        public TextMeshProUGUI hotkeyText; // Text hiển thị phím tắt (1, 2, 3)
    }

    private void Start()
    {
        // Tự động tìm SpellCaster trong scene
        spellCaster = FindAnyObjectByType<SpellCaster>();

        if (spellCaster == null)
        {
            Debug.LogError("SpellCaster not found in scene!");
            return;
        }

        InitializeUI();
    }

    private void InitializeUI()
    {
        // Setup initial UI state
        for (int i = 0; i < spellSlotUIs.Length; i++)
        {
            if (i < spellCaster.spellSlots.Length && spellCaster.spellSlots[i] != null)
            {
                Spell spell = spellCaster.spellSlots[i];

                // Set spell icon
                if (spellSlotUIs[i].spellIcon != null && spell.icon != null)
                {
                    spellSlotUIs[i].spellIcon.sprite = spell.icon;
                    spellSlotUIs[i].spellIcon.enabled = true;
                }

                // Set hotkey text
                if (spellSlotUIs[i].hotkeyText != null)
                {
                    spellSlotUIs[i].hotkeyText.text = (i + 1).ToString();
                }
            }
            else
            {
                // Disable slot if no spell assigned
                if (spellSlotUIs[i].spellIcon != null)
                {
                    spellSlotUIs[i].hotkeyText.enabled = false;
                    spellSlotUIs[i].spellIcon.enabled = false;
                }
                    
            }

            // Hide cooldown UI initially           
            if (spellSlotUIs[i].cooldownRadial != null)
                spellSlotUIs[i].cooldownRadial.fillAmount = 0;

            if (spellSlotUIs[i].cooldownText != null)
                spellSlotUIs[i].cooldownText.enabled = false;
        }
    }

    private void Update()
    {
        UpdateCooldownUI();
    }

    private void UpdateCooldownUI()
    {
        for (int i = 0; i < spellSlotUIs.Length; i++)
        {
            if (i >= spellCaster.spellSlots.Length || spellCaster.spellSlots[i] == null)
                continue;

            float cooldownRemaining = spellCaster.GetCooldownRemaining(i);
            float cooldownTotal = spellCaster.spellSlots[i].cooldown;

            if (cooldownRemaining > 0)
            {
                // Spell is on cooldown
                float cooldownPercent = cooldownRemaining / cooldownTotal;             

                // Update radial fill
                if (spellSlotUIs[i].cooldownRadial != null)
                {
                    spellSlotUIs[i].cooldownRadial.fillAmount = cooldownPercent;
                }

                // Update cooldown text
                if (spellSlotUIs[i].cooldownText != null)
                {
                    spellSlotUIs[i].cooldownText.enabled = true;
                    spellSlotUIs[i].cooldownText.text = cooldownRemaining.ToString("0.0");
                }
            }
            else
            {
                // Spell is ready               
                if (spellSlotUIs[i].cooldownRadial != null)
                    spellSlotUIs[i].cooldownRadial.fillAmount = 0;

                if (spellSlotUIs[i].cooldownText != null)
                    spellSlotUIs[i].cooldownText.enabled = false;
            }
        }
    }
}