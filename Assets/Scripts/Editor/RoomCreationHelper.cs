using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor utility to quickly set up tilemap room structures
/// </summary>
public class RoomCreationHelper : EditorWindow
{
    [MenuItem("Tools/Dungeon/Room Creation Helper")]
    public static void ShowWindow()
    {
        GetWindow<RoomCreationHelper>("Room Creation Helper");
    }

    private string roomName = "Room_Forest_New";
    private int enemySpawnCount = 3;
    private Vector2 roomSize = new Vector2(20, 15);

    private void OnGUI()
    {
        GUILayout.Label("Tilemap Room Creation Helper", EditorStyles.boldLabel);
        GUILayout.Space(10);

        roomName = EditorGUILayout.TextField("Room Name:", roomName);
        enemySpawnCount = EditorGUILayout.IntSlider("Enemy Spawn Count:", enemySpawnCount, 1, 8);
        roomSize = EditorGUILayout.Vector2Field("Room Size (tiles):", roomSize);

        GUILayout.Space(10);

        if (GUILayout.Button("Create Room Structure", GUILayout.Height(30)))
        {
            CreateRoomStructure();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Add RoomTemplate to Selected", GUILayout.Height(25)))
        {
            AddRoomTemplateToSelected();
        }

        if (GUILayout.Button("Auto-Configure Selected Room", GUILayout.Height(25)))
        {
            AutoConfigureSelectedRoom();
        }

        GUILayout.Space(10);
        
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Click 'Create Room Structure' to set up tilemap layers");
        GUILayout.Label("2. Paint your tiles using the Tile Palette");
        GUILayout.Label("3. Use 'Add RoomTemplate' and 'Auto-Configure' buttons");
        GUILayout.Label("4. Save as prefab in Assets/Prefabs/Rooms/Forests/");
        GUILayout.Label("5. Add to Forest.asset theme");
    }

    private void CreateRoomStructure()
    {
        // Create root container
        GameObject roomContainer = new GameObject(roomName);
        roomContainer.transform.position = Vector3.zero;

        // Create tilemap layers
        CreateTilemapLayer("Background", roomContainer.transform, 0, Color.white);
        CreateTilemapLayer("Walls", roomContainer.transform, -1, Color.white);
        CreateTilemapLayer("Details", roomContainer.transform, -2, Color.white);
        var collisionTilemap = CreateTilemapLayer("Collision", roomContainer.transform, -3, Color.red);
        
        // Make collision layer invisible
        var collisionRenderer = collisionTilemap.GetComponent<TilemapRenderer>();
        if (collisionRenderer != null)
        {
            collisionRenderer.enabled = false;
        }

        // Add collider to collision layer
        collisionTilemap.AddComponent<TilemapCollider2D>();
        var composite = collisionTilemap.AddComponent<CompositeCollider2D>();
        var rb = collisionTilemap.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        // Create spawn points
        CreateSpawnPoints(roomContainer.transform);

        // Add RoomTemplate component
        var roomTemplate = roomContainer.AddComponent<RoomTemplate>();
        AutoConfigureRoomTemplate(roomTemplate, roomContainer.transform);

        // Select the created room
        Selection.activeGameObject = roomContainer;

        Debug.Log($"Created room structure '{roomName}' with {enemySpawnCount} enemy spawns!");
    }

    private GameObject CreateTilemapLayer(string layerName, Transform parent, int sortingOrder, Color gizmosColor)
    {
        GameObject tilemapGO = new GameObject(layerName);
        tilemapGO.transform.SetParent(parent);
        tilemapGO.transform.localPosition = Vector3.zero;

        var tilemap = tilemapGO.AddComponent<Tilemap>();
        var renderer = tilemapGO.AddComponent<TilemapRenderer>();
        
        renderer.sortingOrder = sortingOrder;

        return tilemapGO;
    }

    private void CreateSpawnPoints(Transform parent)
    {
        // Player spawn (bottom-left area)
        GameObject playerSpawn = new GameObject("PlayerSpawn");
        playerSpawn.transform.SetParent(parent);
        playerSpawn.transform.localPosition = new Vector3(-roomSize.x/2 + 2, -roomSize.y/2 + 2, 0);

        // Exit anchor (top-right area)
        GameObject exitAnchor = new GameObject("ExitAnchor");
        exitAnchor.transform.SetParent(parent);
        exitAnchor.transform.localPosition = new Vector3(roomSize.x/2 - 2, roomSize.y/2 - 2, 0);

        // Enemy spawns container
        GameObject enemySpawnsContainer = new GameObject("EnemySpawns");
        enemySpawnsContainer.transform.SetParent(parent);
        enemySpawnsContainer.transform.localPosition = Vector3.zero;

        // Create enemy spawn points distributed around the room
        for (int i = 0; i < enemySpawnCount; i++)
        {
            GameObject enemySpawn = new GameObject($"EnemySpawn_{i + 1:D2}");
            enemySpawn.transform.SetParent(enemySpawnsContainer.transform);

            // Distribute spawn points in a circle pattern
            float angle = (i * 360f / enemySpawnCount) * Mathf.Deg2Rad;
            float radius = Mathf.Min(roomSize.x, roomSize.y) * 0.3f;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );
            enemySpawn.transform.localPosition = position;
        }
    }

    private void AddRoomTemplateToSelected()
    {
        var selectedGO = Selection.activeGameObject;
        if (selectedGO == null)
        {
            Debug.LogWarning("Please select a GameObject first!");
            return;
        }

        var roomTemplate = selectedGO.GetComponent<RoomTemplate>();
        if (roomTemplate == null)
        {
            roomTemplate = selectedGO.AddComponent<RoomTemplate>();
            Debug.Log($"Added RoomTemplate component to '{selectedGO.name}'");
        }
        else
        {
            Debug.Log($"RoomTemplate already exists on '{selectedGO.name}'");
        }

        AutoConfigureRoomTemplate(roomTemplate, selectedGO.transform);
    }

    private void AutoConfigureSelectedRoom()
    {
        var selectedGO = Selection.activeGameObject;
        if (selectedGO == null)
        {
            Debug.LogWarning("Please select a GameObject first!");
            return;
        }

        var roomTemplate = selectedGO.GetComponent<RoomTemplate>();
        if (roomTemplate == null)
        {
            Debug.LogWarning("Selected GameObject doesn't have a RoomTemplate component!");
            return;
        }

        AutoConfigureRoomTemplate(roomTemplate, selectedGO.transform);
    }

    private void AutoConfigureRoomTemplate(RoomTemplate roomTemplate, Transform roomTransform)
    {
        // Find player spawn
        var playerSpawn = roomTransform.Find("PlayerSpawn");
        if (playerSpawn != null)
        {
            roomTemplate.playerSpawn = playerSpawn;
        }

        // Find exit anchor
        var exitAnchor = roomTransform.Find("ExitAnchor");
        if (exitAnchor != null)
        {
            roomTemplate.exitAnchor = exitAnchor;
        }

        // Find enemy spawns
        var enemySpawnsContainer = roomTransform.Find("EnemySpawns");
        if (enemySpawnsContainer != null)
        {
            var enemySpawns = new Transform[enemySpawnsContainer.childCount];
            for (int i = 0; i < enemySpawnsContainer.childCount; i++)
            {
                enemySpawns[i] = enemySpawnsContainer.GetChild(i);
            }
            roomTemplate.enemySpawns = enemySpawns;
        }

        EditorUtility.SetDirty(roomTemplate);
        Debug.Log($"Auto-configured RoomTemplate for '{roomTransform.name}'");
    }
}
#endif