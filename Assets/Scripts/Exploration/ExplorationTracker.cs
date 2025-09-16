using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RougeLite.Exploration
{
    /// <summary>
    /// Tracks player exploration and discoveries in the infinite world
    /// Records points of interest, maintains exploration statistics
    /// </summary>
    public class ExplorationTracker : MonoBehaviour
    {
        [Header("Tracking Settings")]
        [SerializeField] private Transform player;
        [SerializeField] private float discoveryRadius = 15f;
        [SerializeField] private float trackingInterval = 2f;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Discovery Categories")]
        [SerializeField] private bool trackStructures = true;
        [SerializeField] private bool trackEnemyGroups = true;
        [SerializeField] private bool trackItemCaches = true;
        [SerializeField] private bool trackBiomes = true;

        [Header("Statistics")]
        [SerializeField, ReadOnly] private int totalDiscoveries = 0;
        [SerializeField, ReadOnly] private int structuresDiscovered = 0;
        [SerializeField, ReadOnly] private int enemyGroupsDiscovered = 0;
        [SerializeField, ReadOnly] private int itemCachesDiscovered = 0;
        [SerializeField, ReadOnly] private int biomesExplored = 0;
        [SerializeField, ReadOnly] private float totalDistanceTraveled = 0f;

        // Discovery tracking
        private List<Discovery> discoveries = new List<Discovery>();
        private HashSet<Vector2Int> exploredChunks = new HashSet<Vector2Int>();
        private Vector3 lastPlayerPosition;
        private float nextTrackingTime;

        // Events
        public System.Action<Discovery> OnNewDiscovery;
        public System.Action<ExplorationStats> OnStatsUpdated;

        #region Unity Lifecycle

        private void Start()
        {
            // Find player if not assigned
            if (autoFindPlayer && player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }

            if (player != null)
            {
                lastPlayerPosition = player.position;
            }

            nextTrackingTime = Time.time + trackingInterval;
        }

        private void Update()
        {
            if (player == null || Time.time < nextTrackingTime) return;

            UpdateExploration();
            nextTrackingTime = Time.time + trackingInterval;
        }

        #endregion

        #region Exploration Tracking

        private void UpdateExploration()
        {
            Vector3 currentPosition = player.position;
            
            // Update distance traveled
            float distanceThisFrame = Vector3.Distance(currentPosition, lastPlayerPosition);
            totalDistanceTraveled += distanceThisFrame;
            lastPlayerPosition = currentPosition;

            // Track current chunk
            Vector2Int currentChunk = WorldToChunk(currentPosition);
            if (!exploredChunks.Contains(currentChunk))
            {
                exploredChunks.Add(currentChunk);
                CheckForBiomeDiscovery(currentPosition);
            }

            // Check for new discoveries
            CheckForDiscoveries(currentPosition);

            // Update statistics
            UpdateStatistics();
        }

        private void CheckForDiscoveries(Vector3 playerPosition)
        {
            // Find all objects within discovery radius
            Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(playerPosition, discoveryRadius);
            
            foreach (var collider in nearbyObjects)
            {
                if (collider.transform == player) continue;

                CheckStructureDiscovery(collider.gameObject);
                CheckEnemyGroupDiscovery(collider.gameObject);
                CheckItemCacheDiscovery(collider.gameObject);
            }
        }

        private void CheckStructureDiscovery(GameObject obj)
        {
            if (!trackStructures) return;

            if (obj.CompareTag("Structure") || obj.GetComponent<Structure>() != null)
            {
                Vector3 position = obj.transform.position;
                if (!IsAlreadyDiscovered(position, DiscoveryType.Structure))
                {
                    string structureName = obj.name;
                    Structure structureComponent = obj.GetComponent<Structure>();
                    if (structureComponent != null)
                    {
                        structureName = structureComponent.structureName;
                    }

                    AddDiscovery(position, DiscoveryType.Structure, structureName, obj);
                    structuresDiscovered++;
                }
            }
        }

        private void CheckEnemyGroupDiscovery(GameObject obj)
        {
            if (!trackEnemyGroups) return;

            if (obj.CompareTag("Enemy"))
            {
                Vector3 position = obj.transform.position;
                
                // Check if this is part of a larger enemy group
                Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(position, 10f);
                int enemyCount = nearbyEnemies.Count(c => c.CompareTag("Enemy"));
                
                if (enemyCount >= 3 && !IsAlreadyDiscovered(position, DiscoveryType.EnemyGroup, 15f))
                {
                    string groupName = $"Enemy Group ({enemyCount} enemies)";
                    AddDiscovery(position, DiscoveryType.EnemyGroup, groupName, obj);
                    enemyGroupsDiscovered++;
                }
            }
        }

        private void CheckItemCacheDiscovery(GameObject obj)
        {
            if (!trackItemCaches) return;

            if (obj.CompareTag("Item") || obj.GetComponent<ItemCache>() != null)
            {
                Vector3 position = obj.transform.position;
                
                // Check if this is a significant item cache
                Collider2D[] nearbyItems = Physics2D.OverlapCircleAll(position, 5f);
                int itemCount = nearbyItems.Count(c => c.CompareTag("Item"));
                
                if (itemCount >= 2 && !IsAlreadyDiscovered(position, DiscoveryType.ItemCache, 8f))
                {
                    string cacheName = $"Item Cache ({itemCount} items)";
                    AddDiscovery(position, DiscoveryType.ItemCache, cacheName, obj);
                    itemCachesDiscovered++;
                }
            }
        }

        private void CheckForBiomeDiscovery(Vector3 position)
        {
            if (!trackBiomes) return;

            // Try to determine current biome
            string biomeName = DetermineBiome(position);
            
            if (!string.IsNullOrEmpty(biomeName) && !IsAlreadyDiscovered(position, DiscoveryType.Biome, 50f))
            {
                AddDiscovery(position, DiscoveryType.Biome, biomeName, null);
                biomesExplored++;
            }
        }

        private string DetermineBiome(Vector3 position)
        {
            // This would integrate with your biome system
            // For now, use simple position-based determination
            
            float x = position.x;
            float y = position.y;
            
            // Example biome determination (replace with your actual biome system)
            if (Mathf.Abs(x) < 50 && Mathf.Abs(y) < 50)
                return "Starting Plains";
            else if (x < -100)
                return "Western Desert";
            else if (x > 100)
                return "Eastern Forest";
            else if (y > 100)
                return "Northern Mountains";
            else if (y < -100)
                return "Southern Swamp";
            else
                return "Frontier Lands";
        }

        #endregion

        #region Discovery Management

        private void AddDiscovery(Vector3 position, DiscoveryType type, string name, GameObject relatedObject)
        {
            Discovery discovery = new Discovery
            {
                id = System.Guid.NewGuid().ToString(),
                position = position,
                type = type,
                name = name,
                discoveryTime = System.DateTime.Now,
                relatedObject = relatedObject
            };

            discoveries.Add(discovery);
            totalDiscoveries++;

            // Trigger discovery event
            OnNewDiscovery?.Invoke(discovery);

            // Log discovery
            Debug.Log($"Discovered {type}: {name} at {position}");
        }

        private bool IsAlreadyDiscovered(Vector3 position, DiscoveryType type, float tolerance = 5f)
        {
            return discoveries.Any(d => 
                d.type == type && 
                Vector3.Distance(d.position, position) <= tolerance);
        }

        private Vector2Int WorldToChunk(Vector3 worldPosition)
        {
            int chunkSize = 50; // Should match your world generator chunk size
            int chunkX = Mathf.FloorToInt(worldPosition.x / chunkSize);
            int chunkY = Mathf.FloorToInt(worldPosition.y / chunkSize);
            return new Vector2Int(chunkX, chunkY);
        }

        #endregion

        #region Statistics

        private void UpdateStatistics()
        {
            ExplorationStats stats = new ExplorationStats
            {
                totalDiscoveries = totalDiscoveries,
                structuresDiscovered = structuresDiscovered,
                enemyGroupsDiscovered = enemyGroupsDiscovered,
                itemCachesDiscovered = itemCachesDiscovered,
                biomesExplored = biomesExplored,
                totalDistanceTraveled = totalDistanceTraveled,
                chunksExplored = exploredChunks.Count,
                explorationTime = Time.time
            };

            OnStatsUpdated?.Invoke(stats);
        }

        public ExplorationStats GetCurrentStats()
        {
            return new ExplorationStats
            {
                totalDiscoveries = totalDiscoveries,
                structuresDiscovered = structuresDiscovered,
                enemyGroupsDiscovered = enemyGroupsDiscovered,
                itemCachesDiscovered = itemCachesDiscovered,
                biomesExplored = biomesExplored,
                totalDistanceTraveled = totalDistanceTraveled,
                chunksExplored = exploredChunks.Count,
                explorationTime = Time.time
            };
        }

        #endregion

        #region Public Methods

        public List<Discovery> GetDiscoveries(DiscoveryType? typeFilter = null)
        {
            if (typeFilter.HasValue)
            {
                return discoveries.Where(d => d.type == typeFilter.Value).ToList();
            }
            return new List<Discovery>(discoveries);
        }

        public List<Discovery> GetNearbyDiscoveries(Vector3 position, float radius)
        {
            return discoveries.Where(d => Vector3.Distance(d.position, position) <= radius).ToList();
        }

        public Discovery GetClosestDiscovery(Vector3 position, DiscoveryType? typeFilter = null)
        {
            var filteredDiscoveries = typeFilter.HasValue 
                ? discoveries.Where(d => d.type == typeFilter.Value)
                : discoveries;

            return filteredDiscoveries
                .OrderBy(d => Vector3.Distance(d.position, position))
                .FirstOrDefault();
        }

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
            if (player != null)
            {
                lastPlayerPosition = player.position;
            }
        }

        public void ClearDiscoveries()
        {
            discoveries.Clear();
            exploredChunks.Clear();
            ResetStatistics();
        }

        private void ResetStatistics()
        {
            totalDiscoveries = 0;
            structuresDiscovered = 0;
            enemyGroupsDiscovered = 0;
            itemCachesDiscovered = 0;
            biomesExplored = 0;
            totalDistanceTraveled = 0f;
        }

        #endregion

        #region Save/Load Support

        [System.Serializable]
        public class ExplorationSaveData
        {
            public List<Discovery> discoveries;
            public List<Vector2Int> exploredChunks;
            public ExplorationStats stats;
        }

        public ExplorationSaveData GetSaveData()
        {
            return new ExplorationSaveData
            {
                discoveries = new List<Discovery>(discoveries),
                exploredChunks = new List<Vector2Int>(exploredChunks),
                stats = GetCurrentStats()
            };
        }

        public void LoadSaveData(ExplorationSaveData data)
        {
            if (data == null) return;

            discoveries = data.discoveries ?? new List<Discovery>();
            exploredChunks = new HashSet<Vector2Int>(data.exploredChunks ?? new List<Vector2Int>());
            
            if (data.stats != null)
            {
                totalDiscoveries = data.stats.totalDiscoveries;
                structuresDiscovered = data.stats.structuresDiscovered;
                enemyGroupsDiscovered = data.stats.enemyGroupsDiscovered;
                itemCachesDiscovered = data.stats.itemCachesDiscovered;
                biomesExplored = data.stats.biomesExplored;
                totalDistanceTraveled = data.stats.totalDistanceTraveled;
            }
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class Discovery
    {
        public string id;
        public Vector3 position;
        public DiscoveryType type;
        public string name;
        public System.DateTime discoveryTime;
        public GameObject relatedObject; // Won't be saved, just for runtime reference
    }

    [System.Serializable]
    public class ExplorationStats
    {
        public int totalDiscoveries;
        public int structuresDiscovered;
        public int enemyGroupsDiscovered;
        public int itemCachesDiscovered;
        public int biomesExplored;
        public float totalDistanceTraveled;
        public int chunksExplored;
        public float explorationTime;
    }

    public enum DiscoveryType
    {
        Structure,
        EnemyGroup,
        ItemCache,
        Biome,
        Special
    }

    // Simple component to mark objects as structures
    [System.Serializable]
    public class Structure : MonoBehaviour
    {
        public string structureName = "Unknown Structure";
    }

    // Simple component to mark item caches
    [System.Serializable]
    public class ItemCache : MonoBehaviour
    {
        public string cacheName = "Item Cache";
        public int itemCount = 1;
    }

    // ReadOnly attribute for inspector
    public class ReadOnlyAttribute : PropertyAttribute { }

    #endregion
}