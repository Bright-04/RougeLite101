using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Definition")]
public class WeaponDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId;
    [SerializeField] private string displayName;

    [Header("Presentation")]
    [SerializeField] private Sprite icon;

    [Header("Runtime")]
    [SerializeField] private GameObject weaponPrefab;

    [Header("Optional")]
    [SerializeField] private string rarity;
    [SerializeField] private string[] tags;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public GameObject WeaponPrefab => weaponPrefab;
    public string Rarity => rarity;
    public string[] Tags => tags;
}
