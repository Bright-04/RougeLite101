using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTemplate : MonoBehaviour
{
    private bool hasSpawnedEnemies = false;

    [Header("Layout")]
    public Transform center;

    [Header("Anchors")]
    public Transform playerSpawn;
    public Transform exitAnchor;

    [Header("Spawning")]
    public Transform[] enemySpawns;
    public RoomSpawnProfileSO spawnProfile;

    [Header("Connection Sockets")]
    public Transform northSocket;
    public Transform southSocket;
    public Transform eastSocket;
    public Transform westSocket;

    [Header("Connection Prefabs")]
    public GameObject doorPrefab;
    public GameObject wallPrefab;

    private readonly List<GameObject> _spawnedConnections = new List<GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _spawnedSideObjects = new Dictionary<Vector2Int, GameObject>();

    public void ApplyConnections(bool openNorth, bool openSouth, bool openEast, bool openWest)
    {
        ClearConnections();

        BuildSide(Vector2Int.up, northSocket, openNorth, 0f);
        BuildSide(Vector2Int.right, eastSocket, openEast, -90f);
        BuildSide(Vector2Int.down, southSocket, openSouth, 180f);
        BuildSide(Vector2Int.left, westSocket, openWest, 90f);
    }

    private void BuildSide(Vector2Int dir, Transform socket, bool open, float zRotation)
    {
        if (socket == null) return;

        GameObject prefab = open ? doorPrefab : wallPrefab;
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, 0f, zRotation), socket);

        Transform centerChild = obj.transform.Find("Center");
        if (centerChild != null)
        {
            Vector3 offset = obj.transform.position - centerChild.position;
            obj.transform.position = socket.position + offset;
        }
        else
        {
            obj.transform.position = socket.position;
        }

        _spawnedConnections.Add(obj);
        _spawnedSideObjects[dir] = obj;
    }

    private void ClearConnections()
    {
        for (int i = _spawnedConnections.Count - 1; i >= 0; i--)
        {
            if (_spawnedConnections[i] != null)
                Destroy(_spawnedConnections[i]);
        }

        _spawnedConnections.Clear();
        _spawnedSideObjects.Clear();
    }

    public GameObject GetSpawnedSideObject(Vector2Int dir)
    {
        if (_spawnedSideObjects.TryGetValue(dir, out GameObject obj))
            return obj;

        return null;
    }

    public Transform GetSocket(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return northSocket;
        if (dir == Vector2Int.down) return southSocket;
        if (dir == Vector2Int.right) return eastSocket;
        if (dir == Vector2Int.left) return westSocket;
        return null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!hasSpawnedEnemies)
            {
                Debug.Log($"[RoomTemplate] Người chơi đã bước vào phòng {gameObject.name}. Bắt đầu kiểm tra điều kiện spawn quái... ");
                hasSpawnedEnemies = true;
                StartCoroutine(SpawnEnemiesRoutine());
            }
            else
            {
                // Đã spawn quái từ trước đó rồi
            }
        }
    }

    private IEnumerator SpawnEnemiesRoutine()
    {
        if (spawnProfile == null)
        {
            Debug.LogWarning($"[RoomTemplate] THẤT BẠI: Phòng {gameObject.name} CHƯA được gán Spawn Profile (spawnProfile). Hãy gán Box (RoomSpawnProfileSO) vào Inspector!");
            yield break;
        }
        
        if (spawnProfile.entries == null || spawnProfile.entries.Length == 0)
        {
            Debug.LogWarning($"[RoomTemplate] THẤT BẠI: Spawn Profile của phòng {gameObject.name} hiện đang TRỐNG (không có dữ liệu Entry nào).");
            yield break;
        }

        if (enemySpawns == null || enemySpawns.Length == 0)
        {
            Debug.LogWarning($"[RoomTemplate] THẤT BẠI: Phòng {gameObject.name} KHÔNG CÓ điểm spawn tọa độ nào được cấu hình trong mảng 'enemySpawns'. Phải kéo các Transform vào đây!");
            yield break;
        }

        Debug.Log($"[RoomTemplate] Điều kiện hợp lệ! Chờ delay {spawnProfile.initialDelay}s và sắp sửa sinh quái ra tại phòng {gameObject.name}...");
        yield return new WaitForSeconds(spawnProfile.initialDelay);

        List<GameObject> enemiesToSpawn = new List<GameObject>();
        foreach (var entry in spawnProfile.entries)
        {
            int count = Random.Range(entry.minCount, entry.maxCount + 1);
            for (int i = 0; i < count; i++)
            {
                enemiesToSpawn.Add(entry.prefab);
            }
        }

        // Shuffle the list for random distribution
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            GameObject temp = enemiesToSpawn[i];
            int randomIndex = Random.Range(i, enemiesToSpawn.Count);
            enemiesToSpawn[i] = enemiesToSpawn[randomIndex];
            enemiesToSpawn[randomIndex] = temp;
        }

        int spawnIndex = 0;
        foreach (GameObject enemyPrefab in enemiesToSpawn)
        {
            if (spawnIndex >= enemySpawns.Length)
            {
                // If there are more enemies than spawn points, just wrap around or pick a random point
                spawnIndex = 0;
            }

            Transform spawnPoint = enemySpawns[spawnIndex];
            Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            spawnIndex++;

            if (spawnProfile.spawnGradually)
            {
                float delay = Random.Range(spawnProfile.perSpawnDelayRange.x, spawnProfile.perSpawnDelayRange.y);
                yield return new WaitForSeconds(delay);
            }
        }
    }
}