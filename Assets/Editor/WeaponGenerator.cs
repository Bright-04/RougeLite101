using UnityEditor;
using UnityEngine;

public class WeaponGenerator : EditorWindow
{
    [MenuItem("Tools/RougeLite101/Generate Weapon Variants")]
    public static void GenerateVariants()
    {
        string baseSwordPath = "Assets/ScriptableObjects/Weapons/Definitions/sword_basic.asset";
        string baseBowPath = "Assets/ScriptableObjects/Weapons/Definitions/bow_basic.asset";
        
        WeaponDefinitionSO baseSword = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(baseSwordPath);
        WeaponDefinitionSO baseBow = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(baseBowPath);

        if (baseSword == null || baseBow == null)
        {
            Debug.LogError("Cannot find base sword or bow definitions at " + baseSwordPath + " or " + baseBowPath);
            return;
        }

        CreateVariant(baseSword, "sword_fire", "Fire Sword", "A sword that burns enemies.", "Rare");
        CreateVariant(baseSword, "sword_ice", "Ice Sword", "A sword that freezes enemies.", "Epic");
        CreateVariant(baseSword, "sword_poison", "Poison Sword", "A sword coated in lethal poison.", "Rare");

        CreateVariant(baseBow, "bow_long", "Long Bow", "A bow with longer range.", "Rare");
        CreateVariant(baseBow, "bow_lightning", "Lightning Bow", "Shoots lightning fast arrows.", "Epic");
        CreateVariant(baseBow, "bow_shadow", "Shadow Bow", "A bow made of pure darkness.", "Legendary");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Weapon variants generated successfully! Don't forget to Auto Populate Weapons in WeaponRegistry.");
    }

    private static void CreateVariant(WeaponDefinitionSO baseWeapon, string newId, string newName, string newDescription, string rarity)
    {
        string newPath = $"Assets/ScriptableObjects/Weapons/Definitions/{newId}.asset";
        
        // If it already exists, don't overwrite
        if (AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(newPath) != null)
        {
            Debug.LogWarning($"Variant {newId} already exists. Skipping.");
            return;
        }

        // Create a new instance
        WeaponDefinitionSO newWeapon = ScriptableObject.CreateInstance<WeaponDefinitionSO>();
        
        // Copy data using SerializedObject
        SerializedObject src = new SerializedObject(baseWeapon);
        SerializedObject dest = new SerializedObject(newWeapon);
        
        SerializedProperty prop = src.GetIterator();
        if (prop.NextVisible(true))
        {
            do
            {
                if (prop.name != "m_Script")
                {
                    dest.CopyFromSerializedProperty(prop);
                }
            }
            while (prop.NextVisible(false));
        }

        dest.ApplyModifiedPropertiesWithoutUndo();

        // Now modify the specific fields
        dest.Update();
        
        // m_Name of the object itself
        newWeapon.name = newId;

        SerializedProperty idProp = dest.FindProperty("weaponId");
        if (idProp != null) idProp.stringValue = newId;
        
        SerializedProperty nameProp = dest.FindProperty("<Name>k__BackingField");
        if (nameProp != null) nameProp.stringValue = newName;
        
        SerializedProperty descProp = dest.FindProperty("<Description>k__BackingField");
        if (descProp != null) descProp.stringValue = newDescription;

        SerializedProperty rarityProp = dest.FindProperty("rarity");
        if (rarityProp != null) rarityProp.stringValue = rarity;

        // Try 'displayName' if it exists
        SerializedProperty displayNameProp = dest.FindProperty("displayName");
        if (displayNameProp != null) displayNameProp.stringValue = newName;

        dest.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(newWeapon, newPath);
    }
}
