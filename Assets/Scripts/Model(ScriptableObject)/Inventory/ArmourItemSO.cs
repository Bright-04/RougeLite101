using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum ArmourType
{
    Helmet,
    Chestplate,
    Leggings,
    Boots
}

public enum ArmourSet
{
    Leather,
    Chainmail,
    Golden,
    Diamond
}


[CreateAssetMenu(fileName = "NewArmourItem", menuName = "Inventory/ArmourItemSO")]
public class ArmourItemSO : ItemSO, IDestroyableItem, IItemAction
{
    public string ActionName => "Equip";

    [Header("Armour Type")]
    [SerializeField] private ArmourType armourType;

    public ArmourType ArmourType => armourType;

    [Header("Armour Set")]
    [SerializeField] private ArmourSet armourSet;

    public ArmourSet ArmourSet => armourSet;

    //[field: SerializeField]
    //public AudioClip actionSFX { get; private set; }

    public bool PerformAction(GameObject character)
    {
        ArmourController armourController = character.GetComponent<ArmourController>();
        if(armourController != null )
        {
            armourController.Equip(this);
            foreach (ModifierData data in modifiersData)
            {
                data.statModifier.AffectCharacter(character, data.value);
            }
        }
        return true;
    }
}
