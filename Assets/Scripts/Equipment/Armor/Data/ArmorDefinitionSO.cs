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
    //[SerializeField] private int defense;
    //[SerializeField] private float maxHealthBonus;
    //[SerializeField] private float moveSpeedBonus;
    //[SerializeField] private float blockCooldown = 0.5f;
    //[SerializeField] private float blockDuration = 0.25f;

    public ArmorType ArmorType => armorType;
    //public int Defense => defense;
    //public float MaxHealthBonus => maxHealthBonus;
    //public float MoveSpeedBonus => moveSpeedBonus;
    //public float BlockCooldown => blockCooldown;
    //public float BlockDuration => blockDuration;

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
