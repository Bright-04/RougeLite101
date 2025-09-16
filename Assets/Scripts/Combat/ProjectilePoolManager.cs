using UnityEngine;
using System.Collections.Generic;
using RougeLite.ObjectPooling;
using RougeLite.Events;

namespace RougeLite.Combat
{
    /// <summary>
    /// Specialized pool manager for handling projectiles
    /// Manages multiple projectile types and provides convenient launching methods
    /// </summary>
    public class ProjectilePoolManager : MonoBehaviour
    {
        private static ProjectilePoolManager instance;
        public static ProjectilePoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("ProjectilePoolManager");
                    instance = go.AddComponent<ProjectilePoolManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [System.Serializable]
        public class ProjectilePoolConfig
        {
            public string poolName;
            public Projectile prefab;
            public int initialSize = 20;
            public int maxSize = 100;
            public bool allowGrowth = true;
        }

        [Header("Projectile Pool Configuration")]
        [SerializeField] private ProjectilePoolConfig[] poolConfigs = new ProjectilePoolConfig[]
        {
            new ProjectilePoolConfig { poolName = "BasicProjectile", initialSize = 20, maxSize = 100 },
            new ProjectilePoolConfig { poolName = "FireProjectile", initialSize = 15, maxSize = 75 },
            new ProjectilePoolConfig { poolName = "IceProjectile", initialSize = 15, maxSize = 75 },
            new ProjectilePoolConfig { poolName = "LightningProjectile", initialSize = 10, maxSize = 50 }
        };

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool logProjectileLaunches = false;

        // Pool storage
        private readonly Dictionary<string, ObjectPool<Projectile>> projectilePools = 
            new Dictionary<string, ObjectPool<Projectile>>();
        
        // Active projectile tracking
        private readonly HashSet<Projectile> activeProjectiles = new HashSet<Projectile>();
        
        // Statistics
        private int totalProjectilesLaunched = 0;
        private int totalProjectilesReturned = 0;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializePools()
        {
            foreach (var config in poolConfigs)
            {
                if (config.prefab != null)
                {
                    CreatePool(config);
                }
                else
                {
                    Debug.LogWarning($"ProjectilePoolManager: No prefab assigned for pool '{config.poolName}'");
                }
            }

            Debug.Log($"ProjectilePoolManager: Initialized {projectilePools.Count} projectile pools");
        }

        private void CreatePool(ProjectilePoolConfig config)
        {
            var pool = new ObjectPool<Projectile>(
                config.prefab,
                config.initialSize,
                config.maxSize,
                transform,
                config.allowGrowth
            );

            projectilePools[config.poolName] = pool;
            Debug.Log($"ProjectilePoolManager: Created pool '{config.poolName}' with {config.initialSize} objects");
        }

        /// <summary>
        /// Register a custom projectile pool
        /// </summary>
        public void RegisterPool(string poolName, Projectile prefab, int initialSize = 20, int maxSize = 100, bool allowGrowth = true)
        {
            if (projectilePools.ContainsKey(poolName))
            {
                Debug.LogWarning($"ProjectilePoolManager: Pool '{poolName}' already exists!");
                return;
            }

            var pool = new ObjectPool<Projectile>(prefab, initialSize, maxSize, transform, allowGrowth);
            projectilePools[poolName] = pool;
            Debug.Log($"ProjectilePoolManager: Registered custom pool '{poolName}'");
        }

        /// <summary>
        /// Launch a projectile from the pool
        /// </summary>
        public Projectile LaunchProjectile(string poolName, Vector2 startPosition, Vector2 direction, 
                                         float speed, float damage, GameObject shooter, string projectileType = "")
        {
            if (!projectilePools.TryGetValue(poolName, out var pool))
            {
                Debug.LogError($"ProjectilePoolManager: Pool '{poolName}' not found!");
                return null;
            }

            var projectile = pool.Get();
            if (projectile == null)
            {
                Debug.LogWarning($"ProjectilePoolManager: Failed to get projectile from pool '{poolName}'");
                return null;
            }

            // Initialize projectile
            projectile.Initialize(startPosition, direction, speed, damage, shooter, projectileType);
            
            // Track active projectile
            activeProjectiles.Add(projectile);
            totalProjectilesLaunched++;

            // Set up return callback
            projectile.OnProjectileDestroyed += OnProjectileDestroyed;

            if (logProjectileLaunches)
            {
                Debug.Log($"ProjectilePoolManager: Launched {poolName} projectile from {shooter?.name ?? "Unknown"}");
            }

            return projectile;
        }

        /// <summary>
        /// Launch projectile with simplified parameters
        /// </summary>
        public Projectile LaunchProjectile(string poolName, Vector2 startPosition, Vector2 direction, GameObject shooter)
        {
            return LaunchProjectile(poolName, startPosition, direction, 10f, 25f, shooter);
        }

        /// <summary>
        /// Return projectile to its pool
        /// </summary>
        public void ReturnProjectile(Projectile projectile)
        {
            if (projectile == null) return;

            // Remove from active tracking
            activeProjectiles.Remove(projectile);
            totalProjectilesReturned++;

            // Find the correct pool and return projectile
            string poolName = GetPoolNameForProjectile(projectile);
            if (!string.IsNullOrEmpty(poolName) && projectilePools.TryGetValue(poolName, out var pool))
            {
                pool.Return(projectile);
            }
            else
            {
                Debug.LogWarning($"ProjectilePoolManager: Could not find pool for projectile {projectile.name}");
                Destroy(projectile.gameObject);
            }
        }

        /// <summary>
        /// Return all active projectiles to their pools
        /// </summary>
        public void ReturnAllProjectiles()
        {
            var projectilesToReturn = new List<Projectile>(activeProjectiles);
            foreach (var projectile in projectilesToReturn)
            {
                if (projectile != null)
                {
                    ReturnProjectile(projectile);
                }
            }
        }

        /// <summary>
        /// Clear all pools and destroy projectiles
        /// </summary>
        public void ClearAllPools()
        {
            ReturnAllProjectiles();

            foreach (var pool in projectilePools.Values)
            {
                pool.Clear();
            }

            projectilePools.Clear();
            activeProjectiles.Clear();
            
            totalProjectilesLaunched = 0;
            totalProjectilesReturned = 0;
        }

        /// <summary>
        /// Get pool statistics for monitoring
        /// </summary>
        public Dictionary<string, PoolStatistics> GetPoolStatistics()
        {
            var stats = new Dictionary<string, PoolStatistics>();
            foreach (var kvp in projectilePools)
            {
                stats[kvp.Key] = kvp.Value.GetStatistics();
            }
            return stats;
        }

        /// <summary>
        /// Get overall projectile manager statistics
        /// </summary>
        public ProjectileManagerStatistics GetManagerStatistics()
        {
            return new ProjectileManagerStatistics
            {
                TotalPoolsRegistered = projectilePools.Count,
                TotalActiveProjectiles = activeProjectiles.Count,
                TotalProjectilesLaunched = totalProjectilesLaunched,
                TotalProjectilesReturned = totalProjectilesReturned,
                ProjectilesInFlight = totalProjectilesLaunched - totalProjectilesReturned
            };
        }

        /// <summary>
        /// Warm up all pools by pre-creating objects
        /// </summary>
        public void WarmUpAllPools()
        {
            foreach (var kvp in projectilePools)
            {
                kvp.Value.WarmUp(10); // Warm up with 10 additional objects
            }
            Debug.Log("ProjectilePoolManager: Warmed up all pools");
        }

        private string GetPoolNameForProjectile(Projectile projectile)
        {
            // Try to determine pool name from projectile type or name
            string projectileName = projectile.name.Replace("(Clone)", "").Trim();
            
            foreach (var kvp in projectilePools)
            {
                // Check if the pool's prefab name matches
                foreach (var config in poolConfigs)
                {
                    if (config.prefab != null && config.prefab.name == projectileName && config.poolName == kvp.Key)
                    {
                        return kvp.Key;
                    }
                }
            }

            // Fallback: try to match by projectile type
            if (!string.IsNullOrEmpty(projectile.ProjectileType))
            {
                foreach (var poolName in projectilePools.Keys)
                {
                    if (poolName.ToLower().Contains(projectile.ProjectileType.ToLower()))
                    {
                        return poolName;
                    }
                }
            }

            return "BasicProjectile"; // Default fallback
        }

        private void OnProjectileDestroyed(Projectile projectile)
        {
            // This is called when projectile's lifetime expires or hits target
            // The projectile will call ReturnProjectile on itself
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            var managerStats = GetManagerStatistics();
            var poolStats = GetPoolStatistics();

            GUILayout.BeginArea(new Rect(10, 220, 500, 300));
            GUILayout.Label("Projectile Pool Manager", GUI.skin.box);
            
            GUILayout.Label($"Total Pools: {managerStats.TotalPoolsRegistered}");
            GUILayout.Label($"Active Projectiles: {managerStats.TotalActiveProjectiles}");
            GUILayout.Label($"Launched: {managerStats.TotalProjectilesLaunched}");
            GUILayout.Label($"Returned: {managerStats.TotalProjectilesReturned}");
            GUILayout.Label($"In Flight: {managerStats.ProjectilesInFlight}");

            GUILayout.Space(10);
            GUILayout.Label("Pool Details:", GUI.skin.box);

            foreach (var kvp in poolStats)
            {
                GUILayout.Label($"{kvp.Key}: {kvp.Value.Active}/{kvp.Value.Total} active, {kvp.Value.Available} available");
            }

            if (GUILayout.Button("Return All Projectiles"))
            {
                ReturnAllProjectiles();
            }

            if (GUILayout.Button("Warm Up Pools"))
            {
                WarmUpAllPools();
            }

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }

    /// <summary>
    /// Statistics for the projectile manager
    /// </summary>
    [System.Serializable]
    public struct ProjectileManagerStatistics
    {
        public int TotalPoolsRegistered;
        public int TotalActiveProjectiles;
        public int TotalProjectilesLaunched;
        public int TotalProjectilesReturned;
        public int ProjectilesInFlight;

        public override string ToString()
        {
            return $"Pools: {TotalPoolsRegistered}, Active: {TotalActiveProjectiles}, Launched: {TotalProjectilesLaunched}, Returned: {TotalProjectilesReturned}, In Flight: {ProjectilesInFlight}";
        }
    }
}