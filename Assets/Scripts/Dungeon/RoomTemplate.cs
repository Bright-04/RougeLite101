using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTemplate : MonoBehaviour
{
    private bool hasSpawnedEnemies = false;
    private bool roomLocked = false;
    private bool isSpawningFinished = false;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private List<GameObject> invisibleWalls = new List<GameObject>();

    [Header("Layout")]
    public Transform center;

    [Header("Anchors")]
    public Transform playerSpawn;
    public Transform exitAnchor;

    [Header("Spawning")]
    public RoomSpawnProfileSO spawnProfile;

    [Header("Connection Sockets")]
    public Transform northSocket;
    public Transform southSocket;
    public Transform eastSocket;
    public Transform westSocket;

    [Header("PlaceHolder")]
    public Transform placeHolder;

    [Header("Connection Prefabs")]
    public GameObject doorPrefab;
    public GameObject wallPrefab;

    [Header("Room Structure")]
    public StructureRandomizer structureRandomizer;

    private GameObject spawnedStructure;

    private readonly List<GameObject> _spawnedConnections = new List<GameObject>();
    private readonly List<GameObject> _spawnedDoors = new List<GameObject>();
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
        
        if (open)
        {
            _spawnedDoors.Add(obj);
        }
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
        _spawnedDoors.Clear();
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

    private void Update()
    {
        // Chờ toàn bộ quái đẻ xong xuôi thì mới bắt đầu kiểm tra "hết quái chưa"
        if (roomLocked && isSpawningFinished)
        {
            CheckEnemiesStatus();
        }
    }

    private void CheckEnemiesStatus()
    {
        // Quái vật khi chết sẽ bị hàm Destroy(gameObject) xóa khỏi bộ nhớ
        // Ta chỉ cần lọc các GameObject null ra khỏi danh sách
        activeEnemies.RemoveAll(e => e == null);

        // Nếu danh sách trống trơn -> Combat kết thúc
        if (activeEnemies.Count == 0)
        {
            UnlockDoors();
        }
    }

    private void LockDoors()
    {
        if (roomLocked) return;
        roomLocked = true;

        foreach (GameObject door in _spawnedDoors)
        {
            if (door == null) continue;
            
            GameObject blocker = new GameObject("InvisibleWallBlocker");
            int obstacleLayerIdx = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayerIdx != -1)
            {
                blocker.layer = obstacleLayerIdx;
            }
            
            blocker.transform.position = door.transform.position;
            blocker.transform.rotation = door.transform.rotation;
            blocker.transform.SetParent(door.transform);

            // Gắn BoxCollider2D đủ to để chặn hoàn toàn cửa
            BoxCollider2D boxCol = blocker.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = false;
            // Cho kích thước to ra một chút để đảm bảo chặn kín cổng
            boxCol.size = new Vector2(3f, 3f); 
            
            invisibleWalls.Add(blocker);
        }
    }

    private void UnlockDoors()
    {
        roomLocked = false;
        
        // Huỷ bỏ toàn bộ các bức tường vô hình khi đã clear quái
        foreach (GameObject wall in invisibleWalls)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }
        invisibleWalls.Clear();

        Debug.Log($"[RoomTemplate] Phòng {gameObject.name} đã được dọn sạch! Vô hiệu hoá tường tàng hình.");
    }

    private IEnumerator SpawnEnemiesRoutine()
    {
        if (spawnProfile == null || spawnProfile.entries == null || spawnProfile.entries.Length == 0)
        {
            // If it's a safe room (no profile), or profile has no entries, just stop quietly.
            yield break;
        }

        BoxCollider2D roomCollider = GetComponent<BoxCollider2D>();
        if (roomCollider == null)
        {
            Debug.LogWarning($"[RoomTemplate] Phòng {gameObject.name} thiếu BoxCollider2D! Không thể tính toán vị trí spawn quái ngẫu nhiên.");
            yield break;
        }

        // --- MỚI: CHỜ NGƯỜI CHƠI ĐI VÀO SÂU TRONG PHÒNG RỒI MỚI KHÓA ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Bounds b = roomCollider.bounds;
            // Ép vào 1.5 đơn vị từ mép cửa để đảm bảo lọt hẳn vào phòng
            float minX = b.min.x + 1.5f;
            float maxX = b.max.x - 1.5f;
            float minY = b.min.y + 1.5f;
            float maxY = b.max.y - 1.5f;

            while (true)
            {
                Vector3 p = player.transform.position;
                if (p.x > minX && p.x < maxX && p.y > minY && p.y < maxY)
                {
                    break;
                }
                yield return null;
            }
        }

        List<GameObject> enemiesToSpawn = new List<GameObject>();
        foreach (var entry in spawnProfile.entries)
        {
            int count = Random.Range(entry.minCount, entry.maxCount + 1);
            for (int i = 0; i < count; i++)
            {
                enemiesToSpawn.Add(entry.prefab);
            }
        }

        if (enemiesToSpawn.Count == 0)
        {
            yield break;
        }

        // KHÓA CỬA NGAY LẬP TỨC TRƯỚC KHI DELAY CHỜ QUÁI ĐẺ
        LockDoors();

        yield return new WaitForSeconds(spawnProfile.initialDelay);

        // Shuffle the list for random distribution
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            GameObject temp = enemiesToSpawn[i];
            int randomIndex = Random.Range(i, enemiesToSpawn.Count);
            enemiesToSpawn[i] = enemiesToSpawn[randomIndex];
            enemiesToSpawn[randomIndex] = temp;
        }

        foreach (GameObject enemyPrefab in enemiesToSpawn)
        {
            Bounds b = roomCollider.bounds;
            Vector3 randomPosition = new Vector3(
                Random.Range(b.min.x + 0.5f, b.max.x - 0.5f), // Padding so they don't spawn exactly ON the walls
                Random.Range(b.min.y + 0.5f, b.max.y - 0.5f), 
                0f
            );

            GameObject spawnedEn = Instantiate(enemyPrefab, randomPosition, Quaternion.identity);
            
            // Ghi nhận trực tiếp GameObject vào danh sách. Khi nó chết (bị Destroy), list sẽ tự cập nhật thành null.
            activeEnemies.Add(spawnedEn);

            if (spawnProfile.spawnGradually)
            {
                float delay = Random.Range(spawnProfile.perSpawnDelayRange.x, spawnProfile.perSpawnDelayRange.y);
                yield return new WaitForSeconds(delay);
            }
        }

        // Đánh dấu là đã spawn đủ số lượng
        isSpawningFinished = true;
        Debug.Log($"[RoomTemplate] Đã spawn và nạp khoá cửa hoàn tất! Tổng số quái phải giết: {activeEnemies.Count}");
    }

    private void Start()
    {
        if (structureRandomizer == null)
        {
            structureRandomizer = GetComponent<StructureRandomizer>();
        }

        SpawnStructure();
    }


    private void SpawnStructure()
    {
        // Structure spawning is optional per-room.
        // If both references are missing, skip quietly.
        if (placeHolder == null && structureRandomizer == null)
        {
            return;
        }

        if (placeHolder == null || structureRandomizer == null)
        {
            Debug.LogWarning($"{gameObject.name}: Missing placeHolder or structureRandomizer.");
            return;
        }

        GameObject prefab = structureRandomizer.GetRandomStructure();

        if (prefab == null)
        {
            Debug.LogWarning($"{gameObject.name}: StructureRandomizer returned null.");
            return;
        }

        spawnedStructure = Instantiate(
            prefab,
            placeHolder.position,
            placeHolder.rotation,
            placeHolder
        );

        Transform centerChild = spawnedStructure.transform.Find("Center");
        if (centerChild != null)
        {
            Vector3 offset = spawnedStructure.transform.position - centerChild.position;
            spawnedStructure.transform.position = placeHolder.position + offset;
        }
    }
}