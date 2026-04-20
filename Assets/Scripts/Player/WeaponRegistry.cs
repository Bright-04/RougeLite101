using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponRegistry", menuName = "Weapons/Weapon Registry")]
public class WeaponRegistry : ScriptableObject
{
    [SerializeField] private List<WeaponDefinitionSO> allWeapons = new List<WeaponDefinitionSO>();

    private Dictionary<string, WeaponDefinitionSO> lookupTable;

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

        lookupTable = new Dictionary<string, WeaponDefinitionSO>();

        foreach (WeaponDefinitionSO weapon in allWeapons)
        {
            if (weapon == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(weapon.WeaponId))
            {
                Debug.LogWarning($"WeaponRegistry: Weapon '{weapon.name}' has an empty weaponId.", this);
                continue;
            }

            if (lookupTable.ContainsKey(weapon.WeaponId))
            {
                Debug.LogWarning($"WeaponRegistry: Duplicate weaponId '{weapon.WeaponId}' detected. Keeping first entry.", this);
                continue;
            }

            lookupTable.Add(weapon.WeaponId, weapon);
        }
    }

    public WeaponDefinitionSO GetById(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return null;
        }

        Initialize();

        if (lookupTable.TryGetValue(weaponId, out WeaponDefinitionSO weapon))
        {
            return weapon;
        }

        Debug.LogWarning($"WeaponRegistry: Could not find weapon with id '{weaponId}'.", this);
        return null;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Populate Weapons")]
    public void AutoPopulateWeapons()
    {
        allWeapons.Clear();

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WeaponDefinitionSO");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            WeaponDefinitionSO weapon = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(path);
            if (weapon != null)
            {
                allWeapons.Add(weapon);
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        lookupTable = null;
        Debug.Log($"WeaponRegistry: Added {allWeapons.Count} weapons.", this);
    }
#endif
}
