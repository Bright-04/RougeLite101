using System;
using UnityEngine;

public class ArmourController : MonoBehaviour
{
    public ArmourItemSO Helmet { get; private set; }
    public ArmourItemSO Chestplate { get; private set; }
    public ArmourItemSO Leggings { get; private set; }
    public ArmourItemSO Boots { get; private set; }

    public event Action OnArmourChanged;

    public void Equip(ArmourItemSO armour)
    {
        switch (armour.ArmourType)
        {
            case ArmourType.Helmet:
                Helmet = armour;
                break;

            case ArmourType.Chestplate:
                Chestplate = armour;
                break;

            case ArmourType.Leggings:
                Leggings = armour;
                break;

            case ArmourType.Boots:
                Boots = armour;
                break;
        }

        OnArmourChanged?.Invoke();
    }
}
