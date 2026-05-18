using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class ArmourUI : MonoBehaviour
{
    [SerializeField] private ArmourController armourController;

    [Header("Icon Displays")]
    [SerializeField] private ArmourSlotUI helmetIcon; 
    [SerializeField] private ArmourSlotUI chestplateIcon;
    [SerializeField] private ArmourSlotUI leggingIcon;
    [SerializeField] private ArmourSlotUI bootsIcon;

    [SerializeField] private Sprite emptyIcon;

    private void Start()
    {
        if (armourController == null)
        {
            armourController = FindFirstObjectByType<ArmourController>(FindObjectsInactive.Include);
        }
        if (armourController == null)
        {
            Debug.LogError("ArmourController not found.");
            return;
        }
        armourController.OnArmourChanged += RefreshUI;
        RefreshUI();

        helmetIcon.OnRightMouseBtnClickArmourSlot += HandleRightClick;
        chestplateIcon.OnRightMouseBtnClickArmourSlot += HandleRightClick;
        leggingIcon.OnRightMouseBtnClickArmourSlot += HandleRightClick;
        bootsIcon.OnRightMouseBtnClickArmourSlot += HandleRightClick;
    }

    private void OnDestroy()
    {
        if (armourController != null)
        {
            armourController.OnArmourChanged -= RefreshUI;
        }
        helmetIcon.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
        chestplateIcon.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
        leggingIcon.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
        bootsIcon.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
    }

    private void RefreshUI()
    {
        UpdateSlot(helmetIcon.ItemImage, armourController.Helmet);
        UpdateSlot(chestplateIcon.ItemImage, armourController.Chestplate);
        UpdateSlot(leggingIcon.ItemImage, armourController.Leggings);
        UpdateSlot(bootsIcon.ItemImage, armourController.Boots);
    }

    private void UpdateSlot(Image slot, ArmourItemSO armour)
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
        armourController.Unequip(slot.ArmourType);
    }

}
