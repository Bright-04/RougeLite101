using UnityEngine;
using RougeLite.Events;
using RougeLite.Combat;
using RougeLite.ObjectPooling;

namespace RougeLite.Examples
{
    /// <summary>
    /// Comprehensive test script for the projectile pooling system
    /// Demonstrates performance improvements and proper usage patterns
    /// </summary>
    public class ProjectilePoolingTest : EventBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTests = true;
        [SerializeField] private bool showPerformanceStats = true;
        [SerializeField] private float testInterval = 2f;
        [SerializeField] private int projectilesPerTest = 10;

        [Header("Test Projectile Prefabs")]
        [SerializeField] private Projectile basicProjectilePrefab;
        [SerializeField] private FireProjectile fireProjectilePrefab;
        [SerializeField] private IceProjectile iceProjectilePrefab;
        [SerializeField] private LightningProjectile lightningProjectilePrefab;

        [Header("Performance Comparison")]
        [SerializeField] private bool enablePerformanceComparison = true;
        [SerializeField] private int performanceTestCount = 100;

        // Test components
        private ProjectileLauncher launcher;
        private ProjectilePoolManager poolManager;
        
        // Performance tracking
        private float pooledCreationTime;
        private float instantiateCreationTime;
        private int pooledProjectilesCreated;
        private int instantiatedProjectilesCreated;

        // Test state
        private int currentTestPhase = 0;
        private string[] testPhases = { "Basic", "Fire", "Ice", "Lightning", "Spread", "Burst", "Performance" };

        protected override void Awake()
        {
            base.Awake();
            
            // Get or create launcher
            launcher = GetComponent<ProjectileLauncher>();
            if (launcher == null)
            {
                launcher = gameObject.AddComponent<ProjectileLauncher>();
            }
        }

        private void Start()
        {
            if (runTests)
            {
                InitializePoolingSystem();
                RegisterForEvents();
                InvokeRepeating(nameof(RunNextTest), 1f, testInterval);
            }
        }

        private void InitializePoolingSystem()
        {
            poolManager = ProjectilePoolManager.Instance;
            
            // Register custom pools if prefabs are assigned
            if (basicProjectilePrefab != null)
                poolManager.RegisterPool("BasicProjectile", basicProjectilePrefab, 25, 100);
            
            if (fireProjectilePrefab != null)
                poolManager.RegisterPool("FireProjectile", fireProjectilePrefab, 20, 75);
            
            if (iceProjectilePrefab != null)
                poolManager.RegisterPool("IceProjectile", iceProjectilePrefab, 20, 75);
            
            if (lightningProjectilePrefab != null)
                poolManager.RegisterPool("LightningProjectile", lightningProjectilePrefab, 15, 50);

            Debug.Log("ProjectilePoolingTest: Initialized pooling system with custom prefabs");
        }

        private void RegisterForEvents()
        {
            RegisterForEvent<ProjectileLaunchedEvent>(OnProjectileLaunched);
            RegisterForEvent<ProjectileHitEvent>(OnProjectileHit);
            RegisterForEvent<ProjectileDestroyedEvent>(OnProjectileDestroyed);
            RegisterForEvent<PoolCreatedEvent>(OnPoolCreated);
        }

        protected override void OnDestroy()
        {
            if (runTests)
            {
                UnregisterFromEvent<ProjectileLaunchedEvent>(OnProjectileLaunched);
                UnregisterFromEvent<ProjectileHitEvent>(OnProjectileHit);
                UnregisterFromEvent<ProjectileDestroyedEvent>(OnProjectileDestroyed);
                UnregisterFromEvent<PoolCreatedEvent>(OnPoolCreated);
            }
            
            base.OnDestroy();
        }

        private void RunNextTest()
        {
            if (!runTests) return;
            
            if (currentTestPhase >= testPhases.Length)
            {
                currentTestPhase = 0; // Loop back to start
            }

            string phase = testPhases[currentTestPhase];
            Debug.Log($"ProjectilePoolingTest: Running {phase} test phase");

            switch (phase)
            {
                case "Basic":
                    TestBasicProjectiles();
                    break;
                case "Fire":
                    TestFireProjectiles();
                    break;
                case "Ice":
                    TestIceProjectiles();
                    break;
                case "Lightning":
                    TestLightningProjectiles();
                    break;
                case "Spread":
                    TestSpreadPattern();
                    break;
                case "Burst":
                    TestBurstPattern();
                    break;
                case "Performance":
                    if (enablePerformanceComparison)
                        TestPerformanceComparison();
                    break;
            }

            currentTestPhase++;
        }

        private void TestBasicProjectiles()
        {
            for (int i = 0; i < projectilesPerTest; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                launcher.Fire("BasicProjectile", direction, 8f, 20f, "Basic Test");
            }
        }

        private void TestFireProjectiles()
        {
            for (int i = 0; i < projectilesPerTest; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                launcher.Fire("FireProjectile", direction, 10f, 30f, "Fire Test");
            }
        }

        private void TestIceProjectiles()
        {
            for (int i = 0; i < projectilesPerTest; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                launcher.Fire("IceProjectile", direction, 6f, 25f, "Ice Test");
            }
        }

        private void TestLightningProjectiles()
        {
            for (int i = 0; i < projectilesPerTest; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                launcher.Fire("LightningProjectile", direction, 15f, 35f, "Lightning Test");
            }
        }

        private void TestSpreadPattern()
        {
            launcher.FireSpread(5, 60f, "BasicProjectile");
        }

        private void TestBurstPattern()
        {
            launcher.FireBurst(8, "BasicProjectile");
        }

        private void TestPerformanceComparison()
        {
            Debug.Log("ProjectilePoolingTest: Running performance comparison...");
            
            // Test pooled creation
            float startTime = Time.realtimeSinceStartup;
            TestPooledCreation();
            pooledCreationTime = Time.realtimeSinceStartup - startTime;
            
            // Wait a frame, then test instantiate creation
            Invoke(nameof(TestInstantiateCreation), 0.1f);
        }

        private void TestPooledCreation()
        {
            for (int i = 0; i < performanceTestCount; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                var projectile = launcher.Fire("BasicProjectile", direction, 8f, 20f, "Performance Test");
                if (projectile != null)
                {
                    pooledProjectilesCreated++;
                    // Immediately return to pool to test cycling
                    Invoke(() => poolManager.ReturnProjectile(projectile), 0.1f);
                }
            }
        }

        private void TestInstantiateCreation()
        {
            if (basicProjectilePrefab == null) return;
            
            float startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < performanceTestCount; i++)
            {
                var projectile = Instantiate(basicProjectilePrefab, transform.position, Quaternion.identity);
                if (projectile != null)
                {
                    instantiatedProjectilesCreated++;
                    // Destroy immediately to simulate typical usage
                    Destroy(projectile.gameObject, 0.1f);
                }
            }
            
            instantiateCreationTime = Time.realtimeSinceStartup - startTime;
            
            // Log performance results
            LogPerformanceResults();
        }

        private void LogPerformanceResults()
        {
            float speedImprovement = instantiateCreationTime / pooledCreationTime;
            
            Debug.Log($"=== PROJECTILE POOLING PERFORMANCE RESULTS ===");
            Debug.Log($"Objects Created: {performanceTestCount}");
            Debug.Log($"Pooled Creation Time: {pooledCreationTime * 1000f:F2}ms");
            Debug.Log($"Instantiate Creation Time: {instantiateCreationTime * 1000f:F2}ms");
            Debug.Log($"Speed Improvement: {speedImprovement:F2}x faster");
            Debug.Log($"Pooled Objects Created: {pooledProjectilesCreated}");
            Debug.Log($"Instantiated Objects Created: {instantiatedProjectilesCreated}");
            Debug.Log($"===============================================");
        }

        // Event handlers
        private void OnProjectileLaunched(ProjectileLaunchedEvent eventData)
        {
            if (showPerformanceStats)
            {
                var projectile = eventData.Projectile;
                Debug.Log($"Projectile Launched: {projectile.ProjectileType} by {eventData.Shooter?.name ?? "Unknown"}");
            }
        }

        private void OnProjectileHit(ProjectileHitEvent eventData)
        {
            if (showPerformanceStats)
            {
                Debug.Log($"Projectile Hit: {eventData.Target?.name ?? "Unknown"} for {eventData.Damage} damage");
            }
        }

        private void OnProjectileDestroyed(ProjectileDestroyedEvent eventData)
        {
            if (showPerformanceStats)
            {
                Debug.Log($"Projectile Destroyed: {eventData.Projectile.ProjectileType} (Hits: {eventData.TargetsHit})");
            }
        }

        private void OnPoolCreated(PoolCreatedEvent eventData)
        {
            Debug.Log($"Pool Created: {eventData.Data.poolName} (Size: {eventData.Data.totalCount})");
        }

        // GUI display for runtime stats
        private void OnGUI()
        {
            if (!showPerformanceStats) return;

            var managerStats = poolManager.GetManagerStatistics();
            var poolStats = poolManager.GetPoolStatistics();

            GUILayout.BeginArea(new Rect(Screen.width - 350, 10, 340, 400));
            GUILayout.Label("Projectile Pooling Test Stats", GUI.skin.box);
            
            GUILayout.Label($"Current Test Phase: {testPhases[currentTestPhase % testPhases.Length]}");
            GUILayout.Label($"Active Projectiles: {managerStats.TotalActiveProjectiles}");
            GUILayout.Label($"Total Launched: {managerStats.TotalProjectilesLaunched}");
            GUILayout.Label($"Total Returned: {managerStats.TotalProjectilesReturned}");

            GUILayout.Space(10);
            GUILayout.Label("Pool Details:", GUI.skin.box);

            foreach (var kvp in poolStats)
            {
                GUILayout.Label($"{kvp.Key}: {kvp.Value.Active}/{kvp.Value.Total}");
            }

            GUILayout.Space(10);
            
            if (pooledCreationTime > 0 && instantiateCreationTime > 0)
            {
                float improvement = instantiateCreationTime / pooledCreationTime;
                GUILayout.Label("Performance Results:", GUI.skin.box);
                GUILayout.Label($"Pooled: {pooledCreationTime * 1000f:F2}ms");
                GUILayout.Label($"Instantiate: {instantiateCreationTime * 1000f:F2}ms");
                GUILayout.Label($"Improvement: {improvement:F2}x faster");
            }

            if (GUILayout.Button("Run Performance Test"))
            {
                TestPerformanceComparison();
            }

            if (GUILayout.Button("Return All Projectiles"))
            {
                poolManager.ReturnAllProjectiles();
            }

            GUILayout.EndArea();
        }

        // Context menu methods for manual testing
        [ContextMenu("Test All Phases")]
        private void TestAllPhases()
        {
            InvokeRepeating(nameof(RunNextTest), 0f, 1f);
        }

        [ContextMenu("Run Performance Test")]
        private void ManualPerformanceTest()
        {
            TestPerformanceComparison();
        }

        [ContextMenu("Fire Random Projectiles")]
        private void FireRandomProjectiles()
        {
            string[] pools = { "BasicProjectile", "FireProjectile", "IceProjectile", "LightningProjectile" };
            
            for (int i = 0; i < 20; i++)
            {
                string pool = pools[Random.Range(0, pools.Length)];
                Vector2 direction = Random.insideUnitCircle.normalized;
                launcher.Fire(pool, direction, Random.Range(5f, 15f), Random.Range(15f, 40f), "Random Test");
            }
        }

        // Helper method for delayed actions
        private void Invoke(System.Action action, float delay)
        {
            StartCoroutine(DelayedAction(action, delay));
        }

        private System.Collections.IEnumerator DelayedAction(System.Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}