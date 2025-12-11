using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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

    [Header("Screen Fade")]
    [Tooltip("Optional CanvasGroup used for screen fade. If null and autoCreateFadeCanvas is true, a full-screen black panel will be created at runtime.")]
    public CanvasGroup fadeCanvasGroup;
    [Tooltip("Duration of fade in/out in seconds")]
    public float fadeDuration = 0.25f;
    [Tooltip("Automatically create a fullscreen black CanvasGroup if none is assigned")] 
    public bool autoCreateFadeCanvas = true;

    [Header("Room Complete UI")]
    [Tooltip("Optional: Assign a UI GameObject (Text or Panel) to show when the room is completed. Can be a child of your HUD Canvas.")]
    public GameObject roomCompleteUI;
    [Tooltip("How long to show the 'Room Completed' message (seconds)")]
    public float roomCompleteDisplayTime = 2f;
    [Tooltip("When true, a simple default Room Completed UI will be created at runtime if none is assigned.")]
    public bool autoCreateDefaultRoomCompleteUI = true;
    [Tooltip("Optional Resources path (without extension) to a Room Complete prefab. Example: 'UI/RoomComplete' -> Assets/Resources/UI/RoomComplete.prefab")]
    public string roomCompletePrefabResourcePath = "UI/RoomComplete";

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

        // NORMAL MODE: 4 normal + 1 boss per theme block
        for (int i = 0; i < totalRooms; i++)
        {
            int block = i / roomsPerTheme;          // which theme block (0,1,2,…)
            int localIndex = i % roomsPerTheme;          // index inside block (0..roomsPerTheme-1)

            ThemeSO theme = themes[Mathf.Clamp(block, 0, themes.Length - 1)];

            bool isBossSlot = (localIndex == roomsPerTheme - 1);   // last room in this block

            GameObject choice = null;

            if (isBossSlot && theme.bossRoomPrefabs != null && theme.bossRoomPrefabs.Length > 0)
            {
                // Pick one boss room at random
                int bossIdx = _rng.Next(0, theme.bossRoomPrefabs.Length);
                choice = theme.bossRoomPrefabs[bossIdx];
                Debug.Log($"Plan room {i}: <color=red>BOSS</color> from theme '{theme.themeName}' -> {choice.name}");
            }
            else
            {
                // Pick a normal room
                if (theme.normalRoomPrefabs == null || theme.normalRoomPrefabs.Length == 0)
                {
                    Debug.LogError($"Theme '{theme.themeName}' has no normalRoomPrefabs assigned!");
                    continue;
                }

                int normalIdx = _rng.Next(0, theme.normalRoomPrefabs.Length);
                choice = theme.normalRoomPrefabs[normalIdx];
                Debug.Log($"Plan room {i}: normal from theme '{theme.themeName}' -> {choice.name}");
            }

            // Safety
            if (choice == null)
            {
                Debug.LogError($"Null room choice at index {i} for theme '{theme.themeName}'");
                continue;
            }

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
        // Fade out, load, then fade in
        // Wait one frame to swallow duplicate triggers
        yield return null;
        yield return StartCoroutine(TransitionAndLoad());
        _transitioning = false;
    }

    private IEnumerator TransitionAndLoad()
    {
        // Disable player movement while transitioning
        DisablePlayerControl();

        // Ensure fade canvas exists
        EnsureFadeCanvas();

        // Fade to black
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // Perform the room load while screen is hidden
        LoadNextRoomInternal();

        // Give one frame for objects to initialize
        yield return null;

        // Fade back in
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // Re-enable player movement after transition
        EnablePlayerControl();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.alpha = from;
        fadeCanvasGroup.gameObject.SetActive(true);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.0001f, duration)));
            fadeCanvasGroup.alpha = v;
            yield return null;
        }

        fadeCanvasGroup.alpha = to;

        // If fully transparent, deactivate to avoid blocking raycasts
        if (Mathf.Approximately(to, 0f))
            fadeCanvasGroup.gameObject.SetActive(false);
    }

    private void EnsureFadeCanvas()
    {
        if (fadeCanvasGroup != null) return;
        if (!autoCreateFadeCanvas) return;

        // Try to find an existing CanvasGroup named 'ScreenFade' in scene
        var existing = GameObject.Find("ScreenFade");
        if (existing != null)
        {
            fadeCanvasGroup = existing.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup != null) return;
        }

        // Create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Auto_Canvas_Fade");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create full-screen panel
        var panelGO = new GameObject("ScreenFade");
        panelGO.transform.SetParent(canvas.transform, false);
        var rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = panelGO.AddComponent<Image>();
        img.color = Color.black;

        fadeCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = true;
        panelGO.SetActive(false);
    }

    private bool _playerMovementWasEnabled = false;

    private void DisablePlayerControl()
    {
        var pm = PlayerMovement.Instance;
        if (pm != null)
        {
            _playerMovementWasEnabled = pm.enabled;
            pm.enabled = false;
        }
        else
        {
            // fallback: try to find player by tag and disable PlayerMovement component
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var comp = player.GetComponent<PlayerMovement>();
                if (comp != null)
                {
                    _playerMovementWasEnabled = comp.enabled;
                    comp.enabled = false;
                }
            }
        }
    }

    private void EnablePlayerControl()
    {
        var pm = PlayerMovement.Instance;
        if (pm != null)
        {
            pm.enabled = _playerMovementWasEnabled;
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var comp = player.GetComponent<PlayerMovement>();
            if (comp != null)
            {
                comp.enabled = _playerMovementWasEnabled;
            }
        }
    }

    private void LoadNextRoomInternal()
    {
        if (_activeRoom) Destroy(_activeRoom);
        _exitDoor = null;
        _aliveEnemies = 0;

        _index++;
        if (_index >= _planPrefabs.Count)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                Debug.Log("Run complete!");
                player.transform.position = new Vector3(0f, 9f, 0f);
                SceneManager.LoadScene("GameHome");
            }
            //SceneManager.LoadScene("GameHome");
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

        // Show Room Completed UI message
        ShowRoomCompleted();
    }

    private void ShowRoomCompleted()
    {
        // Ensure we have a UI object: try to resolve existing scene object, then Resources prefab, then create default
        if (roomCompleteUI == null)
        {
            roomCompleteUI = ResolveRoomCompleteUI();
            if (roomCompleteUI == null && autoCreateDefaultRoomCompleteUI)
                roomCompleteUI = CreateDefaultRoomCompleteUI();
        }

        if (roomCompleteUI == null)
        {
            Debug.Log("Room Completed!");
            return;
        }

        // Prefer TextMeshPro if present
        var tmp = roomCompleteUI.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = "Room Completed!";
            tmp.gameObject.SetActive(true);
        }
        else
        {
            // fallback to legacy UI Text
            var txt = roomCompleteUI.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = "Room Completed!";
                txt.gameObject.SetActive(true);
            }
        }

        roomCompleteUI.SetActive(true);
        // Hide after delay
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideRoomCompleteAfterDelay(roomCompleteDisplayTime));
    }

    private GameObject ResolveRoomCompleteUI()
    {
        // 1) Find by exact name
        var byName = GameObject.Find("RoomCompleteUI");
        if (byName != null) return byName;

        // 2) Search for a TMP/Text in scene whose name suggests it's a room-complete message
        var tmps = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var t in tmps)
        {
            var n = t.gameObject.name.ToLower();
            if (n.Contains("room") || n.Contains("complete") || n.Contains("completed") || n.Contains("RoomCompleteUI"))
            {
                return t.gameObject;
            }
        }

        var texts = FindObjectsOfType<Text>(true);
        foreach (var t in texts)
        {
            var n = t.gameObject.name.ToLower();
            if (n.Contains("room") || n.Contains("complete") || n.Contains("completed"))
            {
                if (t.transform.parent != null) return t.transform.parent.gameObject;
                return t.gameObject;
            }
        }

        // 3) Optionally search for a GameObject tagged specifically (guard against missing tag)
        try
        {
            var tagged = GameObject.FindWithTag("RoomCompleteUI");
            if (tagged != null) return tagged;
        }
        catch { }

        return null;
    }

    private IEnumerator HideRoomCompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, delay));
        if (roomCompleteUI != null)
            roomCompleteUI.SetActive(false);
        _hideCoroutine = null;
    }

    private Coroutine _hideCoroutine;

    /// <summary>
    /// Immediately hide any active Room Completed UI and cancel pending hide coroutine.
    /// Can be called externally (e.g. when player enters exit) to clear the message.
    /// </summary>
    public void HideRoomCompleted()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        if (roomCompleteUI != null)
        {
            // hide TMP if present
            var tmp = roomCompleteUI.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.gameObject.SetActive(false);

            var txt = roomCompleteUI.GetComponentInChildren<Text>(true);
            if (txt != null) txt.gameObject.SetActive(false);

            roomCompleteUI.SetActive(false);
        }
    }

    private GameObject CreateDefaultRoomCompleteUI()
    {
        // Before creating a default, attempt to load a prefab from Resources if configured
        if (!string.IsNullOrEmpty(roomCompletePrefabResourcePath))
        {
            var prefab = Resources.Load<GameObject>(roomCompletePrefabResourcePath);
            if (prefab != null)
            {
                // Find or create a Canvas
                Canvas canva = FindObjectOfType<Canvas>();
                if (canva == null)
                {
                    var canvasGO = new GameObject("Auto_Canvas_RoomComplete");
                    canva = canvasGO.AddComponent<Canvas>();
                    canva.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }

                var inst = Instantiate(prefab, canva.transform, false);
                inst.SetActive(false);
                return inst;
            }
        }

        // Find or create a Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Auto_Canvas_RoomComplete");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create root panel
        var panelGO = new GameObject("RoomCompleteUI");
        panelGO.transform.SetParent(canvas.transform, false);
        var rect = panelGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 100);
        rect.anchorMin = new Vector2(0.5f, 0.9f);
        rect.anchorMax = new Vector2(0.5f, 0.9f);
        rect.anchoredPosition = Vector2.zero;

        // Optional background image
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);

        // Try to add TextMeshPro first
        TextMeshProUGUI tmp = null;
        try
        {
            tmp = panelGO.AddComponent<TextMeshProUGUI>();
        }
        catch { tmp = null; }

        if (tmp != null)
        {
            tmp.text = "Room Completed!";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36;
            tmp.color = Color.white;
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            // Fallback to legacy Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(panelGO.transform, false);
            var txt = textGO.AddComponent<Text>();
            txt.text = "Room Completed!";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var tr = txt.rectTransform;
            tr.anchorMin = new Vector2(0, 0);
            tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
        }

        panelGO.SetActive(false);
        return panelGO;
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
