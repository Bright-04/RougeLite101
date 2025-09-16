using System.Collections.Generic;
using UnityEngine;
using RougeLite.Events;

namespace RougeLite.World
{
    /// <summary>
    /// Manages world boundaries and enemy spawning for expanded maps
    /// Ensures enemies spawn within playable area and despawn when too far
    /// </summary>
    public class WorldManager : EventBehaviour
    {
        [Header("World Bounds")]
        [SerializeField] private float worldWidth = 100f;
        [SerializeField] private float worldHeight = 100f;
        [SerializeField] private Vector2 worldCenter = Vector2.zero;
        
        [Header("Enemy Spawning")]
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private int maxEnemiesInWorld = 50;
        [SerializeField] private float spawnRadius = 20f; // Distance from player to spawn
        [SerializeField] private float despawnRadius = 30f; // Distance from player to despawn
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private Transform player;

        [Header("Spawn Areas")]
        [SerializeField] private bool useSpawnZones = false;
        [SerializeField] private List<SpawnZone> spawnZones = new List<SpawnZone>();

        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private float lastSpawnTime;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Find player if not assigned
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }
        }

        private void Start()
        {
            // Initial enemy spawn
            SpawnInitialEnemies();
        }

        private void Update()
        {
            if (player == null) return;

            ManageEnemySpawning();
            CleanupDistantEnemies();
        }

        #endregion

        #region World Bounds

        public bool IsPositionInWorld(Vector3 position)
        {
            Vector2 pos2D = new Vector2(position.x, position.y);
            Vector2 worldMin = worldCenter - new Vector2(worldWidth, worldHeight) / 2f;
            Vector2 worldMax = worldCenter + new Vector2(worldWidth, worldHeight) / 2f;

            return pos2D.x >= worldMin.x && pos2D.x <= worldMax.x &&
                   pos2D.y >= worldMin.y && pos2D.y <= worldMax.y;
        }

        public Vector3 ClampToWorldBounds(Vector3 position)
        {
            Vector2 worldMin = worldCenter - new Vector2(worldWidth, worldHeight) / 2f;
            Vector2 worldMax = worldCenter + new Vector2(worldWidth, worldHeight) / 2f;

            position.x = Mathf.Clamp(position.x, worldMin.x, worldMax.x);
            position.y = Mathf.Clamp(position.y, worldMin.y, worldMax.y);
            return position;
        }

        public Vector3 GetRandomPositionInWorld()
        {
            Vector2 worldMin = worldCenter - new Vector2(worldWidth, worldHeight) / 2f;
            Vector2 worldMax = worldCenter + new Vector2(worldWidth, worldHeight) / 2f;

            float x = Random.Range(worldMin.x, worldMax.x);
            float y = Random.Range(worldMin.y, worldMax.y);
            return new Vector3(x, y, 0);
        }

        #endregion

        #region Enemy Management

        private void SpawnInitialEnemies()
        {
            int initialCount = Mathf.Min(10, maxEnemiesInWorld);
            for (int i = 0; i < initialCount; i++)
            {
                SpawnEnemyAroundPlayer();
            }
        }

        private void ManageEnemySpawning()
        {
            // Check if we need to spawn more enemies
            if (Time.time - lastSpawnTime >= spawnInterval && 
                spawnedEnemies.Count < maxEnemiesInWorld)
            {
                SpawnEnemyAroundPlayer();
                lastSpawnTime = Time.time;
            }
        }

        private void SpawnEnemyAroundPlayer()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("WorldManager: No enemy prefabs assigned!");
                return;
            }

            Vector3 spawnPosition = GetValidSpawnPosition();
            if (spawnPosition != Vector3.zero)
            {
                GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                spawnedEnemies.Add(enemy);

                // Broadcast enemy spawn event
                var spawnEvent = new EnemySpawnedEvent(enemy, spawnPosition);
                BroadcastEvent(spawnEvent);
            }
        }

        private Vector3 GetValidSpawnPosition()
        {
            if (useSpawnZones && spawnZones.Count > 0)
            {
                return GetSpawnPositionFromZones();
            }
            else
            {
                return GetSpawnPositionAroundPlayer();
            }
        }

        private Vector3 GetSpawnPositionAroundPlayer()
        {
            int attempts = 0;
            while (attempts < 10)
            {
                // Generate random position around player
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                float distance = Random.Range(spawnRadius, spawnRadius * 1.5f);
                Vector3 spawnPos = player.position + new Vector3(randomDirection.x, randomDirection.y, 0) * distance;

                // Make sure it's within world bounds
                if (IsPositionInWorld(spawnPos))
                {
                    return spawnPos;
                }
                attempts++;
            }
            return Vector3.zero; // Failed to find valid position
        }

        private Vector3 GetSpawnPositionFromZones()
        {
            if (spawnZones.Count == 0) return Vector3.zero;

            SpawnZone zone = spawnZones[Random.Range(0, spawnZones.Count)];
            Vector3 randomPos = new Vector3(
                Random.Range(zone.bounds.xMin, zone.bounds.xMax),
                Random.Range(zone.bounds.yMin, zone.bounds.yMax),
                0
            );

            return IsPositionInWorld(randomPos) ? randomPos : Vector3.zero;
        }

        private void CleanupDistantEnemies()
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] == null)
                {
                    spawnedEnemies.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(player.position, spawnedEnemies[i].transform.position);
                if (distance > despawnRadius)
                {
                    // Broadcast despawn event
                    var despawnEvent = new EnemyDespawnedEvent(spawnedEnemies[i]);
                    BroadcastEvent(despawnEvent);

                    Destroy(spawnedEnemies[i]);
                    spawnedEnemies.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Public Methods

        public void SetWorldSize(float width, float height)
        {
            worldWidth = Mathf.Max(10f, width);
            worldHeight = Mathf.Max(10f, height);
        }

        public void SetSpawnSettings(float spawnRad, float despawnRad, int maxEnemies)
        {
            spawnRadius = Mathf.Max(5f, spawnRad);
            despawnRadius = Mathf.Max(spawnRadius + 5f, despawnRad);
            maxEnemiesInWorld = Mathf.Max(1, maxEnemies);
        }

        public void AddSpawnZone(Rect bounds, string zoneName = "")
        {
            spawnZones.Add(new SpawnZone { bounds = bounds, name = zoneName });
        }

        public void ClearAllEnemies()
        {
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                    Destroy(enemy);
            }
            spawnedEnemies.Clear();
        }

        public int GetEnemyCount()
        {
            return spawnedEnemies.Count;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            // Draw world bounds
            Gizmos.color = Color.cyan;
            Vector3 worldSize = new Vector3(worldWidth, worldHeight, 0);
            Gizmos.DrawWireCube(worldCenter, worldSize);

            // Draw spawn radius around player
            if (player != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(player.position, spawnRadius);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(player.position, despawnRadius);
            }

            // Draw spawn zones
            if (useSpawnZones)
            {
                Gizmos.color = Color.yellow;
                foreach (var zone in spawnZones)
                {
                    Vector3 center = new Vector3(zone.bounds.center.x, zone.bounds.center.y, 0);
                    Vector3 size = new Vector3(zone.bounds.width, zone.bounds.height, 0);
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }

        #endregion
    }

    #region Helper Classes

    [System.Serializable]
    public class SpawnZone
    {
        public string name;
        public Rect bounds;
        public float spawnWeight = 1f;
    }

    // Event classes for enemy spawning
    public class EnemySpawnedEvent : GameEvent
    {
        public GameObject Enemy { get; private set; }
        public Vector3 SpawnPosition { get; private set; }

        public EnemySpawnedEvent(GameObject enemy, Vector3 position, GameObject source = null) : base(source)
        {
            Enemy = enemy;
            SpawnPosition = position;
        }
    }

    public class EnemyDespawnedEvent : GameEvent
    {
        public GameObject Enemy { get; private set; }

        public EnemyDespawnedEvent(GameObject enemy, GameObject source = null) : base(source)
        {
            Enemy = enemy;
        }
    }

    #endregion
}