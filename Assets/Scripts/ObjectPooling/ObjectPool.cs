using System.Collections.Generic;
using UnityEngine;

namespace RougeLite.ObjectPooling
{
    /// <summary>
    /// Interface for objects that can be pooled
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when object is retrieved from pool
        /// </summary>
        void OnGetFromPool();
        
        /// <summary>
        /// Called when object is returned to pool
        /// </summary>
        void OnReturnToPool();
        
        /// <summary>
        /// Check if object is currently active and in use
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// The GameObject associated with this poolable object
        /// </summary>
        GameObject GameObject { get; }
    }

    /// <summary>
    /// Generic object pool for reusing GameObjects
    /// Improves performance by avoiding constant instantiation/destruction
    /// </summary>
    /// <typeparam name="T">Type that implements IPoolable</typeparam>
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly Queue<T> availableObjects = new Queue<T>();
        private readonly HashSet<T> activeObjects = new HashSet<T>();
        private readonly Transform poolParent;
        private readonly T prefab;
        private readonly int maxPoolSize;
        private readonly bool allowGrowth;

        public int AvailableCount => availableObjects.Count;
        public int ActiveCount => activeObjects.Count;
        public int TotalCount => AvailableCount + ActiveCount;

        /// <summary>
        /// Create a new object pool
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="initialSize">Initial pool size</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        /// <param name="allowGrowth">Whether pool can grow beyond max size</param>
        public ObjectPool(T prefab, int initialSize = 10, int maxSize = 100, Transform parent = null, bool allowGrowth = true)
        {
            this.prefab = prefab;
            this.maxPoolSize = maxSize;
            this.allowGrowth = allowGrowth;
            
            // Create parent object for organization
            var poolObject = new GameObject($"{prefab.name}_Pool");
            poolParent = poolObject.transform;
            if (parent != null)
            {
                poolParent.SetParent(parent);
            }
            
            // Pre-populate pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        /// <returns>Available pooled object or new instance if pool is empty</returns>
        public T Get()
        {
            T obj;

            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else
            {
                // Pool is empty
                bool unlimited = maxPoolSize <= 0; // 0 or less implies no hard cap
                bool underCap = TotalCount < maxPoolSize;

                if (allowGrowth && (unlimited || underCap))
                {
                    obj = CreateNewObject();
                }
                else
                {
                    Debug.LogWarning($"ObjectPool<{typeof(T).Name}>: Pool exhausted (allowGrowth={allowGrowth}, total={TotalCount}, max={maxPoolSize}).");
                    return null;
                }
            }

            // Activate and track object
            activeObjects.Add(obj);
            obj.GameObject.SetActive(true);
            obj.OnGetFromPool();

            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        /// <param name="obj">Object to return</param>
        public void Return(T obj)
        {
            if (obj == null) return;
            
            if (activeObjects.Remove(obj))
            {
                // Object was active, return to pool
                obj.OnReturnToPool();
                obj.GameObject.SetActive(false);
                obj.GameObject.transform.SetParent(poolParent);
                
                // Only add to available queue if under max size
                if (availableObjects.Count < maxPoolSize)
                {
                    availableObjects.Enqueue(obj);
                }
                else
                {
                    // Pool is full, destroy excess object
                    Object.Destroy(obj.GameObject);
                }
            }
        }

        /// <summary>
        /// Return all active objects to the pool
        /// </summary>
        public void ReturnAll()
        {
            var activeList = new List<T>(activeObjects);
            foreach (var obj in activeList)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// Clear the entire pool and destroy all objects
        /// </summary>
        public void Clear()
        {
            ReturnAll();
            
            while (availableObjects.Count > 0)
            {
                var obj = availableObjects.Dequeue();
                if (obj != null && obj.GameObject != null)
                {
                    Object.Destroy(obj.GameObject);
                }
            }
            
            if (poolParent != null)
            {
                Object.Destroy(poolParent.gameObject);
            }
        }

        /// <summary>
        /// Warm up the pool by pre-creating objects
        /// </summary>
        /// <param name="count">Number of objects to pre-create</param>
        public void WarmUp(int count)
        {
            for (int i = 0; i < count && availableObjects.Count < maxPoolSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// Get pool statistics for debugging
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                PoolName = $"{typeof(T).Name}Pool",
                Available = AvailableCount,
                Active = ActiveCount,
                Total = TotalCount,
                MaxSize = maxPoolSize,
                AllowsGrowth = allowGrowth
            };
        }

        private T CreateNewObject()
        {
            var instance = Object.Instantiate(prefab, poolParent);
            instance.GameObject.SetActive(false);
            availableObjects.Enqueue(instance);
            return instance;
        }
    }

    /// <summary>
    /// Statistics data for pool monitoring
    /// </summary>
    [System.Serializable]
    public struct PoolStatistics
    {
        public string PoolName;
        public int Available;
        public int Active;
        public int Total;
        public int MaxSize;
        public bool AllowsGrowth;

        public override string ToString()
        {
            return $"{PoolName}: {Active}/{Total} active, {Available} available (Max: {MaxSize}, Growth: {AllowsGrowth})";
        }
    }

    /// <summary>
    /// Pool manager for handling multiple object pools
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager instance;
        public static PoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("PoolManager");
                    instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private readonly Dictionary<System.Type, object> pools = new Dictionary<System.Type, object>();

#if RL_DEBUG_UI
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
#endif

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Register a pool for a specific type
        /// </summary>
        public void RegisterPool<T>(ObjectPool<T> pool) where T : Component, IPoolable
        {
            pools[typeof(T)] = pool;
            Debug.Log($"PoolManager: Registered pool for {typeof(T).Name}");
        }

        /// <summary>
        /// Get pool for a specific type
        /// </summary>
        public ObjectPool<T> GetPool<T>() where T : Component, IPoolable
        {
            if (pools.TryGetValue(typeof(T), out var pool))
            {
                return pool as ObjectPool<T>;
            }
            return null;
        }

        /// <summary>
        /// Get object from pool
        /// </summary>
        public T Get<T>() where T : Component, IPoolable
        {
            var pool = GetPool<T>();
            return pool?.Get();
        }

        /// <summary>
        /// Return object to pool
        /// </summary>
        public void Return<T>(T obj) where T : Component, IPoolable
        {
            var pool = GetPool<T>();
            pool?.Return(obj);
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in pools)
            {
                // Use reflection to call Clear method on the pool object
                var clearMethod = kvp.Value.GetType().GetMethod("Clear");
                clearMethod?.Invoke(kvp.Value, null);
            }
            pools.Clear();
        }

        /// <summary>
        /// Get statistics for all pools
        /// </summary>
        public List<PoolStatistics> GetAllStatistics()
        {
            var stats = new List<PoolStatistics>();
            foreach (var kvp in pools)
            {
                // Use reflection to call GetStatistics method on the pool object
                var getStatsMethod = kvp.Value.GetType().GetMethod("GetStatistics");
                if (getStatsMethod != null)
                {
                    var stat = (PoolStatistics)getStatsMethod.Invoke(kvp.Value, null);
                    stats.Add(stat);
                }
            }
            return stats;
        }
#if RL_DEBUG_UI

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.Label("Object Pool Statistics", GUI.skin.box);
            
            foreach (var stat in GetAllStatistics())
            {
                GUILayout.Label(stat.ToString());
            }
            
            GUILayout.EndArea();
        }
#endif
    }
}

