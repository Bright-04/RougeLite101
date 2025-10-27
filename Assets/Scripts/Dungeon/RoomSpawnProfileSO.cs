using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Room Spawn Profile")]
public class RoomSpawnProfileSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;
        public int minCount = 1;
        public int maxCount = 3;
    }

    [Header("What to spawn")]
    public Entry[] entries;

    [Header("How to spawn")]
    public bool spawnGradually = true;
    public float initialDelay = 0.5f;
    public Vector2 perSpawnDelayRange = new Vector2(0.4f, 1.0f); // ignored if spawnGradually=false
}
