using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class EquippableItemSO : ItemSO, IDestroyableItem, IItemAction
{
    public string ActionName => "Equip";

    //[field: SerializeField]
    //public AudioClip actionSFX { get; private set; }

    public bool PerformAction(GameObject character)
    {
        EquipmentManager weaponSystem = character.GetComponent<EquipmentManager>();
        if (weaponSystem != null)
        {
            
            return true;
        }
        return false;
    }
}
