using System.Collections.Generic;
using UnityEngine;

namespace RougeLite.ObjectPooling
{
    /// <summary>
    /// Lightweight pooling for transient VFX GameObjects that don't implement IPoolable.
    /// </summary>
    public static class EffectPool
    {
        private static readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
        private static Transform poolRoot;

        private static Transform Root
        {
            get
            {
                if (poolRoot == null)
                {
                    var go = new GameObject("EffectPool");
                    Object.DontDestroyOnLoad(go);
                    poolRoot = go.transform;
                }
                return poolRoot;
            }
        }

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!pools.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameObject>();
                pools[prefab] = q;
            }

            GameObject obj;
            if (q.Count > 0)
            {
                obj = q.Dequeue();
            }
            else
            {
                obj = Object.Instantiate(prefab, Root);
                obj.SetActive(false);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public static void Return(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null) return;

            if (!pools.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameObject>();
                pools[prefab] = q;
            }

            instance.SetActive(false);
            instance.transform.SetParent(Root);
            q.Enqueue(instance);
        }
    }

    /// <summary>
    /// Helper component to auto-return effects to the EffectPool after a lifetime.
    /// </summary>
    public class EffectAutoReturn : MonoBehaviour
    {
        private GameObject sourcePrefab;
        private float lifeTime;
        private float timer;

        public void Init(GameObject prefab, float duration)
        {
            sourcePrefab = prefab;
            lifeTime = duration;
            timer = 0f;
        }

        private void OnEnable()
        {
            timer = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifeTime)
            {
                EffectPool.Return(sourcePrefab, gameObject);
            }
        }
    }
}

