using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Theme")]
public class ThemeSO : ScriptableObject
{
    public string themeName = "Forest";
    [Tooltip("Room layout prefabs that belong to this theme")]
    public GameObject[] roomPrefabs;
    [Header("Optional map color")]
    public Color mapColor = Color.green;
}
