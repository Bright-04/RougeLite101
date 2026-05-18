using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class WeaponSlotUI : MonoBehaviour, IPointerClickHandler
{
    public EquipmentManager.WeaponSlot SlotType;

    [SerializeField] private Image icon;

    public Image Icon => icon;

    public event Action<WeaponSlotUI> OnRightClickWeaponSlot;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button ==
            PointerEventData.InputButton.Right)
        {
            OnRightClickWeaponSlot?.Invoke(this);
        }
    }
}
