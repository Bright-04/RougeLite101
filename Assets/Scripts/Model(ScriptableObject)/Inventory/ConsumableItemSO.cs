using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "Inventory/ConsumableItemSO")]
public class ConsumableItemSO : ItemSO, IDestroyableItem, IItemAction
{
    public string ActionName => "Consume";

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

public interface IDestroyableItem
{

}

public interface IItemAction
{
    public string ActionName { get; }
    //public AudioClip actionSFX { get; }
    bool PerformAction(GameObject character);
}