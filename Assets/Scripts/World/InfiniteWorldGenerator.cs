using System.Collections.Generic;
using UnityEngine;
using RougeLite.Events;

namespace RougeLite.World
{
    /// <summary>
    /// Infinite world generator that creates procedural content as the player explores
    /// Generates terrain, enemies, items, and structures in chunks around the player
    /// </summary>
    public class InfiniteWorldGenerator : EventBehaviour
    {
        [Header("World Generation Settings")]
        [SerializeField] private int chunkSize = 50; // Size of each world chunk
        [SerializeField] private int chunksAroundPlayer = 3; // How many chunks to keep loaded
        [SerializeField] private Transform player;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Generation Prefabs")]
        [SerializeField] private GameObject[] terrainPrefabs;
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private GameObject[] itemPrefabs;
        [SerializeField] private GameObject[] structurePrefabs;
        [SerializeField] private GameObject[] decorationPrefabs;

        [Header("Generation Probabilities")]
        [Range(0f, 1f)] [SerializeField] private float enemySpawnChance = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float itemSpawnChance = 0.1f;
        [Range(0f, 1f)] [SerializeField] private float structureSpawnChance = 0.05f;
        [Range(0f, 1f)] [SerializeField] private float decorationSpawnChance = 0.2f;

        [Header("Biome Settings")]
        [SerializeField] private BiomeDataSO[] biomeDataSOs; // ScriptableObject approach
        [SerializeField] private BiomeData[] biomes; // Fallback struct approach
        [SerializeField] private float biomeScale = 0.01f; // Controls biome size
        [SerializeField] private bool useBiomes = true;

        [Header("Performance")]
        [SerializeField] private int maxObjectsPerChunk = 20;
        [SerializeField] private bool enableObjectPooling = true;
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance = 100f;

        private Dictionary<Vector2Int, WorldChunk> loadedChunks = new Dictionary<Vector2Int, WorldChunk>();
        private Vector2Int lastPlayerChunkPosition;
        private System.Random worldSeed;
        private Queue<WorldChunk> chunkPool = new Queue<WorldChunk>();

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            
            // Initialize world seed for consistent generation
            worldSeed = new System.Random(System.DateTime.Now.Millisecond);
        }

        private void Start()
        {
            // Find player if not assigned
            if (autoFindPlayer && player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (player == null)
            {
                Debug.LogError("InfiniteWorldGenerator: No player found! Infinite generation disabled.");
                enabled = false;
                return;
            }

            // Generate initial chunks around player
            GenerateChunksAroundPlayer();
        }

        private void Update()
        {
            if (player == null) return;

            Vector2Int currentPlayerChunk = GetChunkPosition(player.position);

            // Only update chunks if player moved to a new chunk
            if (currentPlayerChunk != lastPlayerChunkPosition)
            {
                GenerateChunksAroundPlayer();
                UnloadDistantChunks();
                lastPlayerChunkPosition = currentPlayerChunk;

                // Broadcast chunk changed event
                BroadcastEvent(new ChunkChangedEvent(currentPlayerChunk, lastPlayerChunkPosition));
            }

            // Update LOD for loaded chunks if enabled
            if (enableLOD)
            {
                UpdateChunkLOD();
            }
        }

        #endregion

        #region Chunk Management

        private void GenerateChunksAroundPlayer()
        {
            Vector2Int playerChunkPos = GetChunkPosition(player.position);

            // Generate chunks in a square around the player
            for (int x = -chunksAroundPlayer; x <= chunksAroundPlayer; x++)
            {
                for (int y = -chunksAroundPlayer; y <= chunksAroundPlayer; y++)
                {
                    Vector2Int chunkPos = playerChunkPos + new Vector2Int(x, y);
                    
                    if (!loadedChunks.ContainsKey(chunkPos))
                    {
                        GenerateChunk(chunkPos);
                    }
                }
            }
        }

        private void GenerateChunk(Vector2Int chunkPosition)
        {
            WorldChunk chunk = GetPooledChunk();
            chunk.position = chunkPosition;
            chunk.worldPosition = ChunkToWorldPosition(chunkPosition);
            chunk.gameObjects = new List<GameObject>();

            // Determine biome for this chunk
            BiomeData biome = GetBiomeForChunk(chunkPosition);
            
            // Generate content based on biome
            GenerateChunkContent(chunk, biome);

            loadedChunks[chunkPosition] = chunk;

            // Broadcast chunk generated event
            BroadcastEvent(new ChunkGeneratedEvent(chunkPosition, chunk));
        }

        private void GenerateChunkContent(WorldChunk chunk, BiomeData biome)
        {
            // Use chunk position as seed for consistent generation
            System.Random chunkRandom = new System.Random(GetChunkSeed(chunk.position));
            
            Vector3 chunkWorldPos = chunk.worldPosition;
            int objectsGenerated = 0;

            // Generate terrain/ground
            GenerateChunkTerrain(chunk, biome, chunkRandom);

            // Generate content in grid pattern within chunk
            int gridSize = 5; // 5x5 grid within each chunk
            float cellSize = chunkSize / (float)gridSize;

            for (int x = 0; x < gridSize && objectsGenerated < maxObjectsPerChunk; x++)
            {
                for (int y = 0; y < gridSize && objectsGenerated < maxObjectsPerChunk; y++)
                {
                    Vector3 cellPosition = chunkWorldPos + new Vector3(
                        (x - gridSize/2f) * cellSize + (float)(chunkRandom.NextDouble() * (cellSize/3f * 2) - cellSize/3f),
                        (y - gridSize/2f) * cellSize + (float)(chunkRandom.NextDouble() * (cellSize/3f * 2) - cellSize/3f),
                        0
                    );

                    // Generate different content types
                    if (chunkRandom.NextDouble() < biome.enemySpawnRate * enemySpawnChance)
                    {
                        GenerateEnemy(chunk, cellPosition, biome, chunkRandom);
                        objectsGenerated++;
                    }
                    else if (chunkRandom.NextDouble() < biome.itemSpawnRate * itemSpawnChance)
                    {
                        GenerateItem(chunk, cellPosition, biome, chunkRandom);
                        objectsGenerated++;
                    }
                    else if (chunkRandom.NextDouble() < structureSpawnChance)
                    {
                        GenerateStructure(chunk, cellPosition, biome, chunkRandom);
                        objectsGenerated++;
                    }
                    else if (chunkRandom.NextDouble() < biome.decorationDensity * decorationSpawnChance)
                    {
                        GenerateDecoration(chunk, cellPosition, biome, chunkRandom);
                        objectsGenerated++;
                    }
                }
            }
        }

        private void GenerateChunkTerrain(WorldChunk chunk, BiomeData biome, System.Random random)
        {
            if (biome.terrainPrefabs == null || biome.terrainPrefabs.Length == 0)
                return;

            // Generate background terrain tiles
            int tilesPerChunk = 4; // 4x4 terrain tiles per chunk
            float tileSize = chunkSize / (float)tilesPerChunk;

            for (int x = 0; x < tilesPerChunk; x++)
            {
                for (int y = 0; y < tilesPerChunk; y++)
                {
                    Vector3 tilePosition = chunk.worldPosition + new Vector3(
                        (x - tilesPerChunk/2f) * tileSize + tileSize/2f,
                        (y - tilesPerChunk/2f) * tileSize + tileSize/2f,
                        0
                    );

                    GameObject terrainPrefab = biome.terrainPrefabs[random.Next(biome.terrainPrefabs.Length)];
                    GameObject terrain = Instantiate(terrainPrefab, tilePosition, Quaternion.identity, transform);
                    
                    // Set terrain to background layer
                    SpriteRenderer sr = terrain.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = -100;
                    }

                    chunk.gameObjects.Add(terrain);
                }
            }
        }

        private void GenerateEnemy(WorldChunk chunk, Vector3 position, BiomeData biome, System.Random random)
        {
            GameObject[] enemies = biome.enemyPrefabs?.Length > 0 ? biome.enemyPrefabs : enemyPrefabs;
            if (enemies == null || enemies.Length == 0) return;

            GameObject enemyPrefab = enemies[random.Next(enemies.Length)];
            GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform);
            chunk.gameObjects.Add(enemy);

            // Add distance-based difficulty scaling
            float distanceFromOrigin = Vector2.Distance(Vector2.zero, position);
            ScaleEnemyDifficulty(enemy, distanceFromOrigin);
        }

        private void GenerateItem(WorldChunk chunk, Vector3 position, BiomeData biome, System.Random random)
        {
            GameObject[] items = biome.itemPrefabs?.Length > 0 ? biome.itemPrefabs : itemPrefabs;
            if (items == null || items.Length == 0) return;

            GameObject itemPrefab = items[random.Next(items.Length)];
            GameObject item = Instantiate(itemPrefab, position, Quaternion.identity, transform);
            chunk.gameObjects.Add(item);
        }

        private void GenerateStructure(WorldChunk chunk, Vector3 position, BiomeData biome, System.Random random)
        {
            GameObject[] structures = biome.structurePrefabs?.Length > 0 ? biome.structurePrefabs : structurePrefabs;
            if (structures == null || structures.Length == 0) return;

            GameObject structurePrefab = structures[random.Next(structures.Length)];
            GameObject structure = Instantiate(structurePrefab, position, Quaternion.identity, transform);
            chunk.gameObjects.Add(structure);
        }

        private void GenerateDecoration(WorldChunk chunk, Vector3 position, BiomeData biome, System.Random random)
        {
            GameObject[] decorations = biome.decorationPrefabs?.Length > 0 ? biome.decorationPrefabs : decorationPrefabs;
            if (decorations == null || decorations.Length == 0) return;

            GameObject decorationPrefab = decorations[random.Next(decorations.Length)];
            GameObject decoration = Instantiate(decorationPrefab, position, Quaternion.identity, transform);
            chunk.gameObjects.Add(decoration);
        }

        private void UnloadDistantChunks()
        {
            Vector2Int playerChunkPos = GetChunkPosition(player.position);
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();

            foreach (var kvp in loadedChunks)
            {
                Vector2Int chunkPos = kvp.Key;
                float distance = Vector2Int.Distance(playerChunkPos, chunkPos);

                if (distance > chunksAroundPlayer + 1)
                {
                    chunksToUnload.Add(chunkPos);
                }
            }

            foreach (Vector2Int chunkPos in chunksToUnload)
            {
                UnloadChunk(chunkPos);
            }
        }

        private void UnloadChunk(Vector2Int chunkPosition)
        {
            if (loadedChunks.ContainsKey(chunkPosition))
            {
                WorldChunk chunk = loadedChunks[chunkPosition];
                
                // Destroy all objects in chunk
                foreach (GameObject obj in chunk.gameObjects)
                {
                    if (obj != null)
                        Destroy(obj);
                }
                
                // Return chunk to pool
                ReturnChunkToPool(chunk);
                loadedChunks.Remove(chunkPosition);

                // Broadcast chunk unloaded event
                BroadcastEvent(new ChunkUnloadedEvent(chunkPosition));
            }
        }

        #endregion

        #region Biome System

        private BiomeData GetBiomeForChunk(Vector2Int chunkPosition)
        {
            // Try ScriptableObject approach first
            if (useBiomes && biomeDataSOs != null && biomeDataSOs.Length > 0)
            {
                // Use Perlin noise to determine biome
                float noiseValue = Mathf.PerlinNoise(
                    chunkPosition.x * biomeScale,
                    chunkPosition.y * biomeScale
                );

                int biomeIndex = Mathf.FloorToInt(noiseValue * biomeDataSOs.Length);
                biomeIndex = Mathf.Clamp(biomeIndex, 0, biomeDataSOs.Length - 1);

                return biomeDataSOs[biomeIndex].ToBiomeData();
            }
            
            // Fallback to struct approach
            if (useBiomes && biomes != null && biomes.Length > 0)
            {
                // Use Perlin noise to determine biome
                float noiseValue = Mathf.PerlinNoise(
                    chunkPosition.x * biomeScale,
                    chunkPosition.y * biomeScale
                );

                int biomeIndex = Mathf.FloorToInt(noiseValue * biomes.Length);
                biomeIndex = Mathf.Clamp(biomeIndex, 0, biomes.Length - 1);

                return biomes[biomeIndex];
            }
            
            // Create default biome with basic prefabs from inspector
            return CreateDefaultBiome();
        }

        private BiomeData CreateDefaultBiome()
        {
            return new BiomeData
            {
                biomeName = "Default",
                biomeColor = Color.white,
                enemySpawnRate = enemySpawnChance,
                itemSpawnRate = itemSpawnChance,
                decorationDensity = decorationSpawnChance,
                terrainPrefabs = terrainPrefabs,
                enemyPrefabs = enemyPrefabs,
                itemPrefabs = itemPrefabs,
                structurePrefabs = structurePrefabs,
                decorationPrefabs = decorationPrefabs
            };
        }

        #endregion

        #region Helper Methods

        private Vector2Int GetChunkPosition(Vector3 worldPosition)
        {
            int chunkX = Mathf.FloorToInt(worldPosition.x / chunkSize);
            int chunkY = Mathf.FloorToInt(worldPosition.y / chunkSize);
            return new Vector2Int(chunkX, chunkY);
        }

        private Vector3 ChunkToWorldPosition(Vector2Int chunkPosition)
        {
            return new Vector3(
                chunkPosition.x * chunkSize + chunkSize / 2f,
                chunkPosition.y * chunkSize + chunkSize / 2f,
                0
            );
        }

        private int GetChunkSeed(Vector2Int chunkPosition)
        {
            // Create deterministic seed from chunk position
            return (chunkPosition.x * 73856093) ^ (chunkPosition.y * 19349663);
        }

        private void ScaleEnemyDifficulty(GameObject enemy, float distanceFromOrigin)
        {
            // Scale enemy health and damage based on distance from origin
            float difficultyMultiplier = 1f + (distanceFromOrigin * 0.01f);
            
            // You can implement this based on your enemy system
            // For example, if enemies have a health component:
            // var health = enemy.GetComponent<Health>();
            // if (health != null) health.maxHealth *= difficultyMultiplier;
        }

        private WorldChunk GetPooledChunk()
        {
            if (enableObjectPooling && chunkPool.Count > 0)
            {
                return chunkPool.Dequeue();
            }
            return new WorldChunk();
        }

        private void ReturnChunkToPool(WorldChunk chunk)
        {
            if (enableObjectPooling)
            {
                chunk.gameObjects.Clear();
                chunkPool.Enqueue(chunk);
            }
        }

        private void UpdateChunkLOD()
        {
            Vector3 playerPos = player.position;

            foreach (var kvp in loadedChunks)
            {
                WorldChunk chunk = kvp.Value;
                float distance = Vector3.Distance(playerPos, chunk.worldPosition);
                
                bool shouldBeActive = distance <= lodDistance;
                
                foreach (GameObject obj in chunk.gameObjects)
                {
                    if (obj != null && obj.activeSelf != shouldBeActive)
                    {
                        obj.SetActive(shouldBeActive);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        public void SetChunkSize(int newSize)
        {
            chunkSize = Mathf.Max(10, newSize);
        }

        public void SetChunksAroundPlayer(int count)
        {
            chunksAroundPlayer = Mathf.Max(1, count);
        }

        public WorldChunk GetChunkAt(Vector3 worldPosition)
        {
            Vector2Int chunkPos = GetChunkPosition(worldPosition);
            return loadedChunks.ContainsKey(chunkPos) ? loadedChunks[chunkPos] : null;
        }

        public void ForceRegenerateChunk(Vector2Int chunkPosition)
        {
            if (loadedChunks.ContainsKey(chunkPosition))
            {
                UnloadChunk(chunkPosition);
            }
            GenerateChunk(chunkPosition);
        }

        public int GetLoadedChunkCount()
        {
            return loadedChunks.Count;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (player == null) return;

            // Draw loaded chunks
            Gizmos.color = Color.green;
            foreach (var kvp in loadedChunks)
            {
                Vector3 chunkWorldPos = ChunkToWorldPosition(kvp.Key);
                Gizmos.DrawWireCube(chunkWorldPos, Vector3.one * chunkSize);
            }

            // Draw player's current chunk
            Vector2Int playerChunk = GetChunkPosition(player.position);
            Vector3 playerChunkWorldPos = ChunkToWorldPosition(playerChunk);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(playerChunkWorldPos, Vector3.one * chunkSize);

            // Draw chunk generation radius
            Gizmos.color = Color.yellow;
            float radius = chunksAroundPlayer * chunkSize;
            Gizmos.DrawWireSphere(player.position, radius);
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class WorldChunk
    {
        public Vector2Int position;
        public Vector3 worldPosition;
        public List<GameObject> gameObjects = new List<GameObject>();
        public float generationTime;
    }

    [System.Serializable]
    public class BiomeData
    {
        public string biomeName = "Default";
        public Color biomeColor = Color.white;
        
        [Range(0f, 1f)] public float enemySpawnRate = 0.3f;
        [Range(0f, 1f)] public float itemSpawnRate = 0.1f;
        [Range(0f, 1f)] public float decorationDensity = 0.2f;
        
        public GameObject[] terrainPrefabs;
        public GameObject[] enemyPrefabs;
        public GameObject[] itemPrefabs;
        public GameObject[] structurePrefabs;
        public GameObject[] decorationPrefabs;
    }

    #endregion

    #region Events

    public class ChunkGeneratedEvent : GameEvent
    {
        public Vector2Int ChunkPosition { get; private set; }
        public WorldChunk Chunk { get; private set; }

        public ChunkGeneratedEvent(Vector2Int position, WorldChunk chunk, GameObject source = null) : base(source)
        {
            ChunkPosition = position;
            Chunk = chunk;
        }
    }

    public class ChunkUnloadedEvent : GameEvent
    {
        public Vector2Int ChunkPosition { get; private set; }

        public ChunkUnloadedEvent(Vector2Int position, GameObject source = null) : base(source)
        {
            ChunkPosition = position;
        }
    }

    public class ChunkChangedEvent : GameEvent
    {
        public Vector2Int NewChunk { get; private set; }
        public Vector2Int PreviousChunk { get; private set; }

        public ChunkChangedEvent(Vector2Int newChunk, Vector2Int prevChunk, GameObject source = null) : base(source)
        {
            NewChunk = newChunk;
            PreviousChunk = prevChunk;
        }
    }

    #endregion
}