using UnityEngine;

public enum ArmorType
{
    Shield,
    Helmet,
    Greaves,
    Boots
}

[CreateAssetMenu(fileName = "NewArmor", menuName = "Equipment/Armor Definition")]
public class ArmorDefinitionSO : EquipmentDefinitionSO
{
    [Header("Armor")]
    [SerializeField] private ArmorType armorType;
    [SerializeField] private int defense;
    [SerializeField] private float blockCooldown = 0.5f;
    [SerializeField] private float blockDuration = 0.25f;

    public ArmorType ArmorType => armorType;
    public int Defense => defense;
    public float BlockCooldown => blockCooldown;
    public float BlockDuration => blockDuration;

    public override bool PerformAction(GameObject character)
    {
        return false;
    }
}
