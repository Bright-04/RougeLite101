using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "NewArmourItem", menuName = "Inventory/ArmourItemSO")]
public class ArmourItemSO : ItemSO, IDestroyableItem, IItemAction
{
    public string ActionName => "Equip";

    //[field: SerializeField]
    //public AudioClip actionSFX { get; private set; }

    public bool PerformAction(GameObject character)
    {
        foreach (ModifierData data in modifiersData)
        {
            data.statModifier.AffectCharacter(character, data.value);
        }
        return true;
    }
}
