using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class ArmourUI : MonoBehaviour
{
    [SerializeField] private ArmorController armorController;

    [Header("Icon Displays")]
    [SerializeField] private ArmourSlotUI helmetIcon;
    [SerializeField] private ArmourSlotUI chestplateIcon;
    [SerializeField] private ArmourSlotUI leggingIcon;
    [SerializeField] private ArmourSlotUI bootsIcon;

    [SerializeField] private Sprite emptyIcon;

    private void Start()
    {
        if (armorController == null)
        {
            armorController = FindFirstObjectByType<ArmorController>(FindObjectsInactive.Include);
        }
        if (armorController == null)
        {
            Debug.LogError("ArmourController not found.");
            return;
        }
        armorController.OnArmourChanged += RefreshUI;
        RefreshUI();

        helmetIcon.OnRightMouseBtnClickArmourSlot += HandleRightClick;
        chestplateIcon.OnRightMouseBtnClickArmourSlot += HandleRightClick;
        leggingIcon.OnRightMouseBtnClickArmourSlot += HandleRightClick;
        bootsIcon.OnRightMouseBtnClickArmourSlot += HandleRightClick;
    }

    private void OnDestroy()
    {
        if (armorController != null)
        {
            armorController.OnArmourChanged -= RefreshUI;
        }
        helmetIcon.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
        chestplateIcon.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
        leggingIcon.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
        bootsIcon.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
    }

    private void RefreshUI()
    {
        UpdateSlot(helmetIcon.ItemImage, armorController.Helmet);
        UpdateSlot(chestplateIcon.ItemImage, armorController.Chestplate);
        UpdateSlot(leggingIcon.ItemImage, armorController.Leggings);
        UpdateSlot(bootsIcon.ItemImage, armorController.Boots);
    }

    private void UpdateSlot(Image slot, ArmorDefinitionSO armour)
    {
        if (armour == null)
        {
            slot.sprite = emptyIcon;
            return;
        }

        slot.sprite = armour.ItemImage;
    }

    private void HandleRightClick(ArmourSlotUI slot)
    {
        if (armorController != null)
        {
            armorController.Unequip(slot.ArmorType);
        }

    }
}
