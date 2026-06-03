using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmorRegistry", menuName = "Equipment/Armor Registry")]
public class ArmorRegistry : ScriptableObject
{
    [SerializeField] private List<ArmorDefinitionSO> allArmor = new List<ArmorDefinitionSO>();

    private Dictionary<string, ArmorDefinitionSO> lookupTable;

    private void OnEnable()
    {
        lookupTable = null;
    }

    public void Initialize()
    {
        if (lookupTable != null)
        {
            return;
        }

        lookupTable = new Dictionary<string, ArmorDefinitionSO>();

        foreach (ArmorDefinitionSO armor in allArmor)
        {
            if (armor == null || string.IsNullOrWhiteSpace(armor.EquipmentId))
            {
                continue;
            }

            if (lookupTable.ContainsKey(armor.EquipmentId))
            {
                Debug.LogWarning($"ArmorRegistry: Duplicate armor id '{armor.EquipmentId}' detected. Keeping first entry.", this);
                continue;
            }

            lookupTable.Add(armor.EquipmentId, armor);
        }
    }

    public ArmorDefinitionSO GetById(string armorId)
    {
        if (string.IsNullOrWhiteSpace(armorId))
        {
            return null;
        }

        Initialize();

        if (lookupTable.TryGetValue(armorId, out ArmorDefinitionSO armor))
        {
            return armor;
        }

        Debug.LogWarning($"ArmorRegistry: Could not find armor with id '{armorId}'.", this);
        return null;
    }
}
