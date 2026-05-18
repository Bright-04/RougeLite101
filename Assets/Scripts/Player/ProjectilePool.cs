using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    private static ProjectilePool instance;

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> prefabLookup = new Dictionary<GameObject, GameObject>();

    public static ProjectilePool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<ProjectilePool>();
                if (instance == null)
                {
                    GameObject poolObject = new GameObject("ProjectilePool");
                    instance = poolObject.AddComponent<ProjectilePool>();
                }
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        ObjectPool<GameObject> pool = GetPool(prefab);
        GameObject projectile = pool.Get();
        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.SetActive(true);
        return projectile;
    }

    public void Release(GameObject projectile)
    {
        if (projectile == null)
        {
            return;
        }

        if (!prefabLookup.TryGetValue(projectile, out GameObject prefab) || !pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            Destroy(projectile);
            return;
        }

        pool.Release(projectile);
    }

    private ObjectPool<GameObject> GetPool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            return pool;
        }

        pool = new ObjectPool<GameObject>(
            createFunc: () => CreateProjectile(prefab),
            actionOnGet: projectile => projectile.SetActive(true),
            actionOnRelease: projectile => projectile.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: 128);

        pools.Add(prefab, pool);
        return pool;
    }

    private GameObject CreateProjectile(GameObject prefab)
    {
        GameObject projectile = Instantiate(prefab, transform);
        prefabLookup[projectile] = prefab;

        PooledProjectile pooledProjectile = projectile.GetComponent<PooledProjectile>();
        if (pooledProjectile == null)
        {
            pooledProjectile = projectile.AddComponent<PooledProjectile>();
        }

        pooledProjectile.Initialize(this);
        projectile.SetActive(false);
        return projectile;
    }
}
