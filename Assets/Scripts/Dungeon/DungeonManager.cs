using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonManager : MonoBehaviour
{
    [Header("Run Settings")]
    [Min(1)] public int totalRooms = 10;
    [Min(1)] public int roomsPerTheme = 5;
    public int seed = 0;

    [Header("Test Mode")]
    [Tooltip("Enable to use specific room sequence instead of random")]
    public bool useTestRooms = false;
    [Tooltip("Drag specific room prefabs here for testing (ignores themes)")]
    public GameObject[] testRoomSequence;
    [Tooltip("Start at this room index (0-based). Useful for testing later rooms")]
    public int startAtRoomIndex = 0;

    [Header("Themes (order by block)")]
    public ThemeSO[] themes;  // still used to pick room prefabs

    [Header("Scene References")]
    public Transform roomsParent;
    public GameObject exitDoorPrefab;   // optional if your room already has a door

    private System.Random _rng;
    private List<GameObject> _planPrefabs;
    private int _index = -1;
    private GameObject _activeRoom;

    private ExitDoor _exitDoor;
    private int _aliveEnemies;
    private bool _transitioning;

    void Start()
    {
        int finalSeed = seed != 0 ? seed : Random.Range(int.MinValue, int.MaxValue);
        _rng = new System.Random(finalSeed);
        Debug.Log($"DungeonManager using seed: {finalSeed}");
        BuildPlan();
        
        // Skip to specified room for testing
        if (startAtRoomIndex > 0)
        {
            _index = Mathf.Clamp(startAtRoomIndex - 1, -1, _planPrefabs.Count - 1);
            Debug.Log($"<color=cyan>Skipping to room index {startAtRoomIndex}</color>");
        }
        
        LoadNextRoomInternal();
    }

    void BuildPlan()
    {
        _planPrefabs = new List<GameObject>(totalRooms);

        // TEST MODE: Use specific room sequence
        if (useTestRooms && testRoomSequence != null && testRoomSequence.Length > 0)
        {
            Debug.Log($"<color=yellow>TEST MODE: Using {testRoomSequence.Length} specified rooms</color>");
            for (int i = 0; i < totalRooms; i++)
            {
                // Loop through test rooms if we run out
                int testIndex = i % testRoomSequence.Length;
                var testRoom = testRoomSequence[testIndex];
                
                if (testRoom != null)
                {
                    _planPrefabs.Add(testRoom);
                    Debug.Log($"  Room {i}: {testRoom.name}");
                }
                else
                {
                    Debug.LogWarning($"Test room at index {testIndex} is null!");
                }
            }
            return;
        }

        // NORMAL MODE: Random selection from themes
        for (int i = 0; i < totalRooms; i++)
        {
            int block = i / roomsPerTheme;
            var theme = themes[Mathf.Clamp(block, 0, themes.Length - 1)];
            var rooms = theme.roomPrefabs;
            if (rooms == null || rooms.Length == 0)
            {
                Debug.LogError($"Theme '{theme.themeName}' has no roomPrefabs assigned!");
                continue;
            }

            var choice = rooms[_rng.Next(0, rooms.Length)];
            
            // Validate the chosen room prefab
            ValidateRoomPrefab(choice, theme.themeName);
            
            _planPrefabs.Add(choice);
        }
    }

    /// <summary>
    /// Validates a room prefab and logs warnings about potential issues
    /// </summary>
    private void ValidateRoomPrefab(GameObject roomPrefab, string themeName)
    {
        if (roomPrefab == null)
        {
            Debug.LogError($"Null room prefab found in theme '{themeName}'!");
            return;
        }

        var roomTemplate = roomPrefab.GetComponent<RoomTemplate>();
        if (roomTemplate == null)
        {
            Debug.LogWarning($"Room prefab '{roomPrefab.name}' in theme '{themeName}' is missing RoomTemplate component. It will be added automatically at runtime.");
            return;
        }

        // Validate room template configuration
        if (roomTemplate.playerSpawn == null)
        {
            Debug.LogWarning($"Room '{roomPrefab.name}' has no player spawn point set.");
        }

        if (roomTemplate.exitAnchor == null)
        {
            Debug.LogWarning($"Room '{roomPrefab.name}' has no exit anchor set.");
        }

        if (roomTemplate.enemySpawns == null || roomTemplate.enemySpawns.Length == 0)
        {
            Debug.LogWarning($"Room '{roomPrefab.name}' has no enemy spawn points set.");
        }
    }

    public void TryLoadNextRoom()
    {
        if (_transitioning) return;
        StartCoroutine(GuardedTransition());
    }

    private IEnumerator GuardedTransition()
    {
        _transitioning = true;
        yield return null; // wait one frame to swallow duplicate triggers
        LoadNextRoomInternal();
        _transitioning = false;
    }

    private void LoadNextRoomInternal()
    {
        if (_activeRoom) Destroy(_activeRoom);
        _exitDoor = null;
        _aliveEnemies = 0;

        _index++;
        if (_index >= _planPrefabs.Count)
        {
            Debug.Log("Run complete!");
            SceneManager.LoadScene("GameHome");
            return;
        }

        // Instantiate room
        var prefab = _planPrefabs[_index];
        _activeRoom = Instantiate(prefab, Vector3.zero, Quaternion.identity, roomsParent);
        var rt = _activeRoom.GetComponent<RoomTemplate>();

        // Handle missing RoomTemplate component
        if (rt == null)
        {
            Debug.LogWarning($"RoomTemplate missing on room '{prefab.name}'. This can cause issues with spawn points and door positioning. Adding component automatically and creating basic configuration.");
            rt = _activeRoom.AddComponent<RoomTemplate>();
            
            // Try to find basic spawn points automatically
            TryAutoConfigureRoomTemplate(rt);
        }

        // Ensure ExitDoor exists, is initialized, and starts locked
        _exitDoor = FindOrCreateExitDoor(rt);
        if (_exitDoor != null) { _exitDoor.Init(this); _exitDoor.Lock(); }

        // Move player
        if (rt && rt.playerSpawn)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) player.transform.position = rt.playerSpawn.position;
        }
        else
        {
            // If no player spawn is set, try to find a reasonable position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) 
            {
                player.transform.position = _activeRoom.transform.position;
                Debug.LogWarning($"No player spawn set for room '{prefab.name}'. Placing player at room center.");
            }
        }

        // Spawn using per-room profile
        if (rt == null)
        {
            Debug.LogError("Failed to create RoomTemplate component. Unlocking door immediately.");  
            _exitDoor?.Unlock();
            return;
        }

        if (rt.spawnProfile == null)
        {
            Debug.Log("No spawn profile on this room � unlocking door immediately.");
            _exitDoor?.Unlock();                    // <<� will actually enable collider
            return;
        }

        // If there are no spawn points, also unlock
        if (rt.enemySpawns == null || rt.enemySpawns.Length == 0)
        {
            Debug.Log("No enemy spawns in this room � unlocking door immediately.");
            _exitDoor?.Unlock();
            return;
        }

        StartCoroutine(SpawnFromProfile(rt, rt.spawnProfile));
    }

    private ExitDoor FindOrCreateExitDoor(RoomTemplate rt)
    {
        // 1) If we have a prefab assigned, ALWAYS spawn it (ignoring any old doors in the room)
        if (exitDoorPrefab && rt && rt.exitAnchor)
        {
            // Remove any old ExitDoor from the room first
            var oldDoor = _activeRoom.GetComponentInChildren<ExitDoor>(true);
            if (oldDoor != null)
            {
                Debug.Log($"Removing old ExitDoor from room and spawning new one from prefab.");
                Destroy(oldDoor.gameObject);
            }
            
            var doorGO = Instantiate(exitDoorPrefab, rt.exitAnchor.position, Quaternion.identity, _activeRoom.transform);
            return doorGO.GetComponent<ExitDoor>();
        }

        // 2) Fallback: Use an ExitDoor already inside the room prefab (only if no prefab assigned)
        var existing = _activeRoom.GetComponentInChildren<ExitDoor>(true);
        if (existing) return existing;

        Debug.LogWarning("No ExitDoor found or created (missing prefab or exitAnchor). Room will have no exit.");
        return null;
    }

    private IEnumerator SpawnFromProfile(RoomTemplate rt, RoomSpawnProfileSO profile)
    {
        if (profile.spawnGradually && profile.initialDelay > 0)
            yield return new WaitForSeconds(profile.initialDelay);

        // Decide total count per entry up front
        var toSpawn = new List<GameObject>();
        foreach (var e in profile.entries)
        {
            if (e.prefab == null) continue;
            int count = Mathf.Max(0, _rng.Next(e.minCount, e.maxCount + 1));
            for (int i = 0; i < count; i++) toSpawn.Add(e.prefab);
        }

        if (toSpawn.Count == 0)
        {
            Debug.Log("Spawn profile produced 0 enemies � unlocking door.");
            _exitDoor?.Unlock();
            yield break;
        }

        _aliveEnemies = 0;

        if (!profile.spawnGradually)
        {
            // spawn all instantly
            foreach (var prefab in toSpawn) SpawnOne(rt, prefab);
        }
        else
        {
            // spawn over time
            foreach (var prefab in toSpawn)
            {
                SpawnOne(rt, prefab);
                float delay = Random.Range(profile.perSpawnDelayRange.x, profile.perSpawnDelayRange.y);
                yield return new WaitForSeconds(Mathf.Max(0f, delay));
            }
        }

        // wait for clear
        while (_aliveEnemies > 0) yield return null;

        _exitDoor?.Unlock();
    }

    private void SpawnOne(RoomTemplate rt, GameObject prefab)
    {
        var sp = rt.enemySpawns[_rng.Next(0, rt.enemySpawns.Length)];
        var enemy = Instantiate(prefab, sp.position, Quaternion.identity, _activeRoom.transform);

        var notifier = enemy.GetComponent<EnemyDeathNotifier>();
        if (!notifier) notifier = enemy.AddComponent<EnemyDeathNotifier>();
        notifier.Died += OnEnemyDied;

        _aliveEnemies++;
    }

    private void OnEnemyDied(EnemyDeathNotifier n)
    {
        if (n) n.Died -= OnEnemyDied;
        _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
    }

    /// <summary>
    /// Attempts to automatically configure a RoomTemplate component with basic settings
    /// </summary>
    private void TryAutoConfigureRoomTemplate(RoomTemplate rt)
    {
        if (rt == null) return;

        // Try to find or create player spawn point
        var playerSpawnTransform = _activeRoom.transform.Find("PlayerSpawn");
        if (playerSpawnTransform == null)
        {
            // Create a basic player spawn at room center
            var playerSpawnGO = new GameObject("PlayerSpawn");
            playerSpawnGO.transform.SetParent(_activeRoom.transform);
            playerSpawnGO.transform.localPosition = Vector3.zero;
            playerSpawnTransform = playerSpawnGO.transform;
        }
        rt.playerSpawn = playerSpawnTransform;

        // Try to find or create exit anchor
        var exitAnchorTransform = _activeRoom.transform.Find("ExitAnchor");
        if (exitAnchorTransform == null)
        {
            // Create a basic exit anchor
            var exitAnchorGO = new GameObject("ExitAnchor");
            exitAnchorGO.transform.SetParent(_activeRoom.transform);
            exitAnchorGO.transform.localPosition = new Vector3(5f, 0f, 0f); // Offset to the right
            exitAnchorTransform = exitAnchorGO.transform;
        }
        rt.exitAnchor = exitAnchorTransform;

        // Try to find enemy spawn points
        var enemySpawnsList = new List<Transform>();
        for (int i = 0; i < _activeRoom.transform.childCount; i++)
        {
            var child = _activeRoom.transform.GetChild(i);
            if (child.name.ToLower().Contains("spawn") && child.name.ToLower().Contains("enemy"))
            {
                enemySpawnsList.Add(child);
            }
        }

        // If no enemy spawns found, create a few basic ones
        if (enemySpawnsList.Count == 0)
        {
            for (int i = 0; i < 3; i++)
            {
                var spawnGO = new GameObject($"EnemySpawn_{i}");
                spawnGO.transform.SetParent(_activeRoom.transform);
                // Spread spawns around the room
                float angle = (i * 120f) * Mathf.Deg2Rad;
                spawnGO.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 3f,
                    Mathf.Sin(angle) * 3f,
                    0f
                );
                enemySpawnsList.Add(spawnGO.transform);
            }
        }

        rt.enemySpawns = enemySpawnsList.ToArray();

        Debug.LogWarning($"Auto-configured RoomTemplate for '{_activeRoom.name}' with {rt.enemySpawns.Length} enemy spawns.");
    }
}
