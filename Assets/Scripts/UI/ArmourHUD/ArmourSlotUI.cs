using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;


public class ArmourSlotUI : MonoBehaviour, IPointerClickHandler
{
    [field: SerializeField]
    public Image ItemImage { get; set; }

    [Header("Armour Type")]
    [SerializeField] private ArmourType armourType;

    public ArmourType ArmourType => armourType;

    public event Action<ArmourSlotUI> OnRightMouseBtnClickArmourSlot;


    public void OnPointerClick(PointerEventData pointerData)
    {

        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            OnRightMouseBtnClickArmourSlot?.Invoke(this);
        }       
    }
}
