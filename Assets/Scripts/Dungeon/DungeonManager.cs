using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonManager : MonoBehaviour
{
    [Header("Floor Settings")]
    public int gridWidth = 4;
    public int gridHeight = 4;
    public Vector2Int spawnGridPos = new Vector2Int(3, 2);
    [Min(1)] public int normalRoomCount = 5;

    [Header("World Settings")]
    public float roomSpacing = 40f;
    public Transform roomsParent;

    [Header("Player")]

    [Header("Boss Floor")]
    public bool isBossFloor = false;

    [Header("Floor Progression")]
    public int currentFloor = 1;
    public int bossEveryXFloor = 5;
    public int maxFloor = 15;

    [Header("Themes")]
    public ThemeSO[] themes;
    public int currentThemeIndex = 0;

    [Header("Random")]
    public int seed = 0;



    public float corridorSegmentLength = 7f;

    private System.Random _rng;
    private Dictionary<Vector2Int, RoomNode> _roomMap = new Dictionary<Vector2Int, RoomNode>();

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Start()
    {
        int finalSeed = seed != 0 ? seed : Random.Range(int.MinValue, int.MaxValue);
        _rng = new System.Random(finalSeed);
        Debug.Log($"DungeonManager using seed: {finalSeed}");

        GenerateFloor();
    }

    public void GenerateFloor()
    {
        ClearFloor();
        _roomMap.Clear();

        ThemeSO theme = GetCurrentTheme();
        if (theme == null)
        {
            Debug.LogError("No valid theme assigned.");
            return;
        }

        bool isBossFloor = currentFloor % bossEveryXFloor == 0;

        Debug.Log($"Generating floor {currentFloor}. Boss floor = {isBossFloor}");

        if (isBossFloor)
        {
            GenerateBossFloor();
        }
        else
        {
            AddSpawnRoom();
            GenerateConnectedNormalRooms();
            GenerateExitRoomLast();
            AssignSpecialAndBuffRooms();
        }

        ChoosePrefabs(theme);
        SolveConnections();
        SpawnRooms();
        SpawnCorridors(theme);
        PlacePlayerInSpawnRoom();
    }

    private ThemeSO GetCurrentTheme()
    {
        if (themes == null || themes.Length == 0) return null;
        return themes[Mathf.Clamp(currentThemeIndex, 0, themes.Length - 1)];
    }

    private void AddSpawnRoom()
    {
        RoomNode spawn = new RoomNode(spawnGridPos);
        spawn.roomType = RoomType.Spawn;
        _roomMap.Add(spawnGridPos, spawn);
    }

    private void GenerateConnectedNormalRooms()
    {
        List<Vector2Int> frontier = new List<Vector2Int>();
        AddAvailableNeighbors(spawnGridPos, frontier);

        int generated = 0;

        while (generated < normalRoomCount && frontier.Count > 0)
        {
            int index = _rng.Next(0, frontier.Count);
            Vector2Int chosen = frontier[index];
            frontier.RemoveAt(index);

            if (_roomMap.ContainsKey(chosen))
                continue;

            RoomNode node = new RoomNode(chosen);
            node.roomType = RoomType.Enemy; // default, may be changed later
            _roomMap.Add(chosen, node);
            generated++;

            AddAvailableNeighbors(chosen, frontier);
        }
    }

    private void GenerateExitRoomLast()
    {
        List<Vector2Int> deadEndCandidates = new List<Vector2Int>();
        List<Vector2Int> generalCandidates = new List<Vector2Int>();

        foreach (Vector2Int emptyCell in GetAllEmptyCells())
        {
            int neighborCount = CountExistingNeighbors(emptyCell);
            if (neighborCount < 1) continue;

            if (IsAdjacent(emptyCell, spawnGridPos)) continue;

            if (neighborCount == 1)
                deadEndCandidates.Add(emptyCell);
            else
                generalCandidates.Add(emptyCell);
        }

        Vector2Int? exitPos = null;

        if (deadEndCandidates.Count > 0)
        {
            exitPos = deadEndCandidates[_rng.Next(0, deadEndCandidates.Count)];
        }
        else if (generalCandidates.Count > 0)
        {
            exitPos = generalCandidates[_rng.Next(0, generalCandidates.Count)];
        }
        else
        {
            foreach (Vector2Int emptyCell in GetAllEmptyCells())
            {
                if (CountExistingNeighbors(emptyCell) >= 1)
                {
                    exitPos = emptyCell;
                    break;
                }
            }
        }

        if (!exitPos.HasValue)
        {
            Debug.LogWarning("Could not place exit room.");
            return;
        }

        RoomNode exitNode = new RoomNode(exitPos.Value);
        exitNode.roomType = RoomType.Exit;
        _roomMap.Add(exitPos.Value, exitNode);
    }

    private void AssignSpecialAndBuffRooms()
    {
        List<RoomNode> candidates = new List<RoomNode>();

        foreach (var kvp in _roomMap)
        {
            RoomNode node = kvp.Value;
            if (node.roomType == RoomType.Spawn || node.roomType == RoomType.Exit)
                continue;

            candidates.Add(node);
        }

        if (candidates.Count > 0)
        {
            int specialIndex = _rng.Next(0, candidates.Count);
            candidates[specialIndex].roomType = RoomType.Special;
            candidates.RemoveAt(specialIndex);
        }

        if (candidates.Count > 0)
        {
            int buffIndex = _rng.Next(0, candidates.Count);
            candidates[buffIndex].roomType = RoomType.Buff;
            candidates.RemoveAt(buffIndex);
        }

        foreach (RoomNode node in candidates)
        {
            node.roomType = RoomType.Enemy;
        }
    }

    private void ChoosePrefabs(ThemeSO theme)
    {
        foreach (var kvp in _roomMap)
        {
            RoomNode node = kvp.Value;

            switch (node.roomType)
            {
                case RoomType.Spawn:
                    node.chosenPrefab = theme.spawnRoomPrefab;
                    break;
                case RoomType.Exit:
                    node.chosenPrefab = theme.exitRoomPrefab;
                    break;
                case RoomType.Enemy:
                    node.chosenPrefab = GetRandomPrefab(theme.enemyRoomPrefabs);
                    break;
                case RoomType.Special:
                    node.chosenPrefab = GetRandomPrefab(theme.specialRoomPrefabs);
                    break;
                case RoomType.Buff:
                    node.chosenPrefab = GetRandomPrefab(theme.buffRoomPrefabs);
                    break;
                case RoomType.Boss:
                    node.chosenPrefab = GetRandomPrefab(theme.bossRoomPrefabs);
                    break;
            }

            if (node.chosenPrefab == null)
            {
                Debug.LogError($"No prefab available for {node.roomType} in theme {theme.themeName}");
            }
        }
    }

    private GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        int index = _rng.Next(0, prefabs.Length);
        return prefabs[index];
    }

    private void SolveConnections()
    {
        foreach (var kvp in _roomMap)
        {
            RoomNode node = kvp.Value;
            Vector2Int pos = node.gridPos;

            node.openNorth = _roomMap.ContainsKey(pos + Vector2Int.up);
            node.openSouth = _roomMap.ContainsKey(pos + Vector2Int.down);
            node.openEast = _roomMap.ContainsKey(pos + Vector2Int.right);
            node.openWest = _roomMap.ContainsKey(pos + Vector2Int.left);
        }
    }

    private void SpawnRooms()
    {
        foreach (var kvp in _roomMap)
        {
            RoomNode node = kvp.Value;
            if (node.chosenPrefab == null) continue;

            Transform parent = roomsParent != null ? roomsParent : transform;

            GameObject roomObj = Instantiate(node.chosenPrefab, Vector3.zero, Quaternion.identity, parent);
            roomObj.name = $"{node.roomType}_{node.gridPos.x}_{node.gridPos.y}";
            node.spawnedInstance = roomObj;

            Vector3 targetWorldPos = GridToWorld(node.gridPos);

            RoomTemplate template = roomObj.GetComponent<RoomTemplate>();
            if (template != null && template.center != null)
            {
                Vector3 delta = targetWorldPos - template.center.position;
                roomObj.transform.position += delta;

                template.ApplyConnections(node.openNorth, node.openSouth, node.openEast, node.openWest);
            }
            else
            {
                Debug.LogWarning($"{roomObj.name} is missing RoomTemplate or center. Using default grid position.");
                roomObj.transform.position = targetWorldPos;

                if (template != null)
                    template.ApplyConnections(node.openNorth, node.openSouth, node.openEast, node.openWest);
            }

            if (node.roomType == RoomType.Exit)
            {
                ExitDoor exitDoor = roomObj.GetComponentInChildren<ExitDoor>();
                if (exitDoor != null)
                {
                    exitDoor.Init(this);
                }
                else
                {
                    Debug.LogWarning($"{roomObj.name} has no ExitDoor component.");
                }
            }

            Debug.Log($"{roomObj.name} grid={node.gridPos} root={roomObj.transform.position} center={template.center.position}");

        }
    }

    private void AddAvailableNeighbors(Vector2Int pos, List<Vector2Int> frontier)
    {
        foreach (Vector2Int dir in Directions)
        {
            Vector2Int next = pos + dir;

            if (!IsInsideGrid(next)) continue;
            if (_roomMap.ContainsKey(next)) continue;
            if (!frontier.Contains(next)) frontier.Add(next);
        }
    }

    private IEnumerable<Vector2Int> GetAllEmptyCells()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!_roomMap.ContainsKey(pos))
                    yield return pos;
            }
        }
    }

    private int CountExistingNeighbors(Vector2Int pos)
    {
        int count = 0;
        foreach (Vector2Int dir in Directions)
        {
            if (_roomMap.ContainsKey(pos + dir))
                count++;
        }
        return count;
    }

    private bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    private Vector3 GridToWorld(Vector2Int pos)
    {
        return new Vector3(pos.x * roomSpacing, pos.y * roomSpacing, 0f);
    }

    private void ClearFloor()
    {
        if (roomsParent == null) return;

        for (int i = roomsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(roomsParent.GetChild(i).gameObject);
        }
    }

    private void PlacePlayerInSpawnRoom()
    {
        if (!_roomMap.TryGetValue(spawnGridPos, out RoomNode spawnNode))
        {
            Debug.LogWarning("DungeonManager: could not find spawn room node.");
            return;
        }

        if (spawnNode.spawnedInstance == null)
        {
            Debug.LogWarning("DungeonManager: spawn room instance missing.");
            return;
        }

        RoomTemplate template = spawnNode.spawnedInstance.GetComponent<RoomTemplate>();
        if (template == null)
        {
            Debug.LogWarning("DungeonManager: spawn room missing RoomTemplate.");
            return;
        }

        if (template.playerSpawn == null)
        {
            Debug.LogWarning("DungeonManager: playerSpawn not assigned on spawn room.");
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("DungeonManager: could not find player with tag 'Player'.");
            return;
        }

        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = template.playerSpawn.position;
        }
        else
        {
            playerObj.transform.position = template.playerSpawn.position;
        }

        Debug.Log("Player placed at spawn room: " + template.playerSpawn.position);
    }
    private void TrySpawnCorridor(RoomNode node, Vector2Int dir, RoomTemplate templateA, GameObject corridorPrefab)
    {
        Vector2Int neighborPos = node.gridPos + dir;

        if (!_roomMap.TryGetValue(neighborPos, out RoomNode neighbor))
            return;

        if (neighbor.spawnedInstance == null)
            return;

        RoomTemplate templateB = neighbor.spawnedInstance.GetComponent<RoomTemplate>();
        if (templateB == null)
            return;

        GameObject gateAObj = templateA.GetSpawnedSideObject(dir);
        GameObject gateBObj = templateB.GetSpawnedSideObject(-dir);

        if (gateAObj == null || gateBObj == null)
        {
            Debug.LogWarning($"Missing spawned gate object between {node.gridPos} and {neighborPos}");
            return;
        }

        ConnectionPoints gateA = gateAObj.GetComponentInChildren<ConnectionPoints>();
        ConnectionPoints gateB = gateBObj.GetComponentInChildren<ConnectionPoints>();

        if (gateA == null || gateB == null)
        {
            Debug.LogWarning($"Missing ConnectionPoints on gate between {node.gridPos} and {neighborPos}");
            return;
        }

        RepeatCorridorBetween(gateA, gateB, corridorPrefab);
    }
    private void RepeatCorridorBetween(ConnectionPoints gateA, ConnectionPoints gateB, GameObject corridorPrefab)
    {
        ConnectionPoints t = corridorPrefab.GetComponentInChildren<ConnectionPoints>();
        if (t == null || t.center == null) return;

        Vector3 start = gateA.center.position;
        start.z = 0f;
        Vector3 end = gateB.center.position;
        end.z = 0f;

        Vector3 dir = (end - start).normalized;
        float totalDistance = Vector3.Distance(start, end);
        
       

        // Rotate 90 degrees if moving horizontally (X axis)
        bool isHorizontal = Mathf.Abs(dir.x) > Mathf.Abs(dir.y);
        Quaternion rot = isHorizontal ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
        int count;
        if (isHorizontal)
            count = Mathf.Max(1, Mathf.RoundToInt(totalDistance));
        else
            count = Mathf.Max(1, Mathf.RoundToInt(totalDistance))+1;
        // Center offset rotated correctly
        Vector3 centerLocal = rot * t.center.localPosition;

        Transform parent = roomsParent != null ? roomsParent : transform;
        Vector3Int test = new Vector3Int(1, 0, 0);
        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = start + dir * i;

            // Offset 1 unit perpendicular to direction to close the gap
            if (isHorizontal)
                worldPos.x += 1f;
            else 
                worldPos.y -= 1f;

                Vector3 rootPos = worldPos - centerLocal;
            rootPos.z = 0f;

            Instantiate(corridorPrefab, rootPos, rot, parent);
        }
    }
    private void SpawnCorridors(ThemeSO theme)
    {
        Debug.Log("SpawnCorridors called");

        if (theme == null || theme.corridorPrefab == null)
        {
            Debug.LogWarning("Current theme is missing corridor prefab.");
            return;
        }

        foreach (var kvp in _roomMap)
        {
            RoomNode node = kvp.Value;
            if (node.spawnedInstance == null) continue;

            RoomTemplate templateA = node.spawnedInstance.GetComponent<RoomTemplate>();
            if (templateA == null) continue;

            TrySpawnCorridor(node, Vector2Int.right, templateA, theme.corridorPrefab);
            TrySpawnCorridor(node, Vector2Int.up, templateA, theme.corridorPrefab);
        }
    }

    private void GenerateBossFloor()
    {
        AddSpawnRoom();

        Vector2Int bossPos = spawnGridPos + Vector2Int.right;
        Vector2Int exitPos = bossPos + Vector2Int.right;

        if (!IsInsideGrid(bossPos) || !IsInsideGrid(exitPos))
        {
            bossPos = spawnGridPos + Vector2Int.left;
            exitPos = bossPos + Vector2Int.left;
        }

        if (!IsInsideGrid(bossPos) || !IsInsideGrid(exitPos))
        {
            bossPos = spawnGridPos + Vector2Int.up;
            exitPos = bossPos + Vector2Int.up;
        }

        if (!IsInsideGrid(bossPos) || !IsInsideGrid(exitPos))
        {
            bossPos = spawnGridPos + Vector2Int.down;
            exitPos = bossPos + Vector2Int.down;
        }

        RoomNode boss = new RoomNode(bossPos);
        boss.roomType = RoomType.Boss;
        _roomMap.Add(bossPos, boss);

        RoomNode exit = new RoomNode(exitPos);
        exit.roomType = RoomType.Exit;
        _roomMap.Add(exitPos, exit);
    }


    public void LoadNextFloor()
    {
        currentFloor++;

        if (currentFloor > maxFloor)
        {
            Debug.Log("Dungeon completed!");

            SceneManager.LoadScene("GameHome");
            return;
        }

        Debug.Log("Loading Floor: " + currentFloor);

        if ((currentFloor - 1) % 5 == 0)
        {
            currentThemeIndex += 1;
        }

        GenerateFloor();
    }
}