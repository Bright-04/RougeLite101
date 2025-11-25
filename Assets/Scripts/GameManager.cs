using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Global access to the active GameManager instance. Use only for reading.
    /// </summary>
    public static GameManager Instance { get; private set; }

    [Header("Persistent Objects")]
    [SerializeField]
    [Tooltip("Objects that should persist across scene loads. These are registered at Awake.")]
    private GameObject[] persistentObjects;

    private void Awake()
    {
        // If another instance already exists, destroy this duplicate and keep the original intact.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        MarkPersistentObjects();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Marks configured objects as persistent with DontDestroyOnLoad.
    /// Only objects assigned to this manager will be processed.
    /// </summary>
    private void MarkPersistentObjects()
    {
        if (persistentObjects == null || persistentObjects.Length == 0)
            return;

        foreach (GameObject obj in persistentObjects)
        {
            if (obj != null)
            {
                DontDestroyOnLoad(obj);
            }
        }
    }

    /// <summary>
    /// Destroys all persistent objects owned by this manager and then destroys the manager itself.
    /// Use when intentionally cleaning up before quitting or returning to a fresh state.
    /// </summary>
    public void CleanupBeforeQuit()
    {
        if (persistentObjects != null)
        {
            foreach (GameObject obj in persistentObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        Destroy(gameObject);
        Instance = null;
    }

    /// <summary>
    /// Public helper so other systems can register objects at runtime to be persistent.
    /// Avoid duplicates; the manager will call DontDestroyOnLoad on the provided object.
    /// </summary>
    public void RegisterPersistentObject(GameObject go)
    {
        if (go == null) return;
        DontDestroyOnLoad(go);
        // Note: we do not add it to the serialized array here to avoid reserializing at runtime.
    }
}
