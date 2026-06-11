using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class WeaponSlotUI : MonoBehaviour, IPointerClickHandler
{
    public EquipmentManager.WeaponSlot SlotType;

    [SerializeField] private Image icon;

    public Image Icon => icon;

    public event Action<WeaponSlotUI> OnRightClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Clicked {name} - {eventData.button}");
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick?.Invoke(this);
        }
    }
}
