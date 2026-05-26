using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Spawn Pack")]
public class SpawnPackSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;
        public int minCount = 1;
        public int maxCount = 3;
    }

    [Tooltip("One room will spawn 1..n of each entry")]
    public Entry[] entries;
}
