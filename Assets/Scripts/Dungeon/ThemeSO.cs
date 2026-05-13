using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Theme")]
public class ThemeSO : ScriptableObject
{
    public string themeName = "Forest";

    [Header("Fixed Rooms")]
    public GameObject spawnRoomPrefab;
    public GameObject exitRoomPrefab;

    [Header("Enemy Rooms")]
    public GameObject[] enemyRoomPrefabs;

    [Header("Special Rooms")]
    public GameObject[] specialRoomPrefabs;

    [Header("Buff Rooms")]
    public GameObject[] buffRoomPrefabs;

    [Header("Boss Rooms")]
    public GameObject[] bossRoomPrefabs;

    [Header("Optional map color")]
    public Color mapColor = Color.green;

    [Header("Connections")]
    public GameObject corridorPrefab;

    [Header("Spawn packs used by rooms of this theme")]
    public SpawnPackSO[] spawnPacks;
}