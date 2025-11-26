using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Theme")]
public class ThemeSO : ScriptableObject
{
    public string themeName = "Forest";

    [Header("Normal Rooms (used for the first roomsPerTheme-1 rooms)")]
    [Tooltip("All the non-boss layouts for this theme.")]
    public GameObject[] normalRoomPrefabs;

    [Header("Boss Rooms (used for the last room in each theme block)")]
    [Tooltip("One of these will be picked as the boss room for this theme block.")]
    public GameObject[] bossRoomPrefabs;

    [Header("Optional map color")]
    public Color mapColor = Color.green;

    [Header("Spawn packs used by rooms of this theme")]
    public SpawnPackSO[] spawnPacks;  // same as before
}
