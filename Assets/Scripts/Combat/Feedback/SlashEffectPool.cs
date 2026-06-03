using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class SlashEffectPool : MonoBehaviour
{
    private static SlashEffectPool instance;

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> prefabLookup = new Dictionary<GameObject, GameObject>();

    public static SlashEffectPool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SlashEffectPool>();
                if (instance == null)
                {
                    GameObject poolObject = new GameObject("SlashEffectPool");
                    instance = poolObject.AddComponent<SlashEffectPool>();
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

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject effect = GetPool(prefab).Get();
        effect.transform.SetParent(parent, true);
        effect.transform.SetPositionAndRotation(position, rotation);

        SlashAnim slashAnim = effect.GetComponent<SlashAnim>();
        if (slashAnim != null)
        {
            slashAnim.Initialize(this);
        }

        effect.SetActive(true);
        return effect;
    }

    public void Release(GameObject effect)
    {
        if (effect == null)
        {
            return;
        }

        if (!prefabLookup.TryGetValue(effect, out GameObject prefab) || !pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            Destroy(effect);
            return;
        }

        effect.transform.SetParent(transform, false);
        pool.Release(effect);
    }

    private ObjectPool<GameObject> GetPool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            return pool;
        }

        pool = new ObjectPool<GameObject>(
            createFunc: () => CreateEffect(prefab),
            actionOnGet: effect => { },
            actionOnRelease: effect => effect.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: 64);

        pools.Add(prefab, pool);
        return pool;
    }

    private GameObject CreateEffect(GameObject prefab)
    {
        GameObject effect = Instantiate(prefab, transform);
        prefabLookup[effect] = prefab;
        effect.SetActive(false);
        return effect;
    }
}
