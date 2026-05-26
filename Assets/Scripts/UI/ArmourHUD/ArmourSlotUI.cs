using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArmourSlotUI : MonoBehaviour, IPointerClickHandler
{
    [field: SerializeField]
    public Image ItemImage { get; private set; }

    [SerializeField]
    private EquipmentController.ArmorSlot armorSlot;

    public EquipmentController.ArmorSlot ArmorSlot => armorSlot;

    public event Action<ArmourSlotUI> OnRightMouseBtnClickArmourSlot;

    public void OnPointerClick(PointerEventData pointerData)
    {
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            OnRightMouseBtnClickArmourSlot?.Invoke(this);
        }
    }
}
