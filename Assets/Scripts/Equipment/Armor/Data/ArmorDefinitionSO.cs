using UnityEngine;

public enum ArmorType
{
    Helmet,
    Chestplate,
    Leggings,
    Boots
}

[CreateAssetMenu(fileName = "NewArmor", menuName = "Equipment/Armor Definition")]
/// <summary>
/// Armor definition data uses the inherited EquipmentId as its stable authored identity.
/// Keep that string-based ID aligned with save/load and registry lookups.
/// </summary>
public class ArmorDefinitionSO : EquipmentDefinitionSO
{
    [Header("Armor")]
    [SerializeField] private ArmorType armorType; 

    public ArmorType ArmorType => armorType;  

    //[field: SerializeField]
    //public AudioClip actionSFX { get; private set; }

    public override bool PerformAction(GameObject character)
    {
        ArmorController armorController = character.GetComponent<ArmorController>();
        if (armorController == null)
        {
            return false;
        }

        foreach (ModifierData data in modifiersData)
        {
            data.statModifier.AffectCharacter(character, data.value);
        }
        armorController.Equip(this);
        return true;
    }
}
