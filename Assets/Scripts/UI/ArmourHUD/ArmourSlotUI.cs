using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArmourSlotUI : MonoBehaviour, IPointerClickHandler
{
    [field: SerializeField]
    public Image ItemImage { get; private set; }

    [Header("Armour Type")]
    [SerializeField] private ArmorType armorType;

    public ArmorType ArmorType => armorType;

    public event Action<ArmourSlotUI> OnRightMouseBtnClickArmourSlot;

    public void OnPointerClick(PointerEventData pointerData)
    {
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            OnRightMouseBtnClickArmourSlot?.Invoke(this);
        }
    }
}
