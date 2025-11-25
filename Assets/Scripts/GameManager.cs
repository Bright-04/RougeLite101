using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Persistent Objects")]
    public GameObject[] persistentObject;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            CleanUpAndDestroy();
            //Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            MarkPersistentObjects();
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void MarkPersistentObjects()
    {
        foreach (GameObject obj in persistentObject)
        {
            if (obj != null)
            {
                DontDestroyOnLoad(obj);
            }
        }
    }

    private void CleanUpAndDestroy()
    {
        foreach(GameObject obj in persistentObject)
        {
            if(obj != null)
            {
                Destroy(obj);
            }
        }

        Destroy(gameObject);
    }

    // Cleanup tất cả persistent objects trước khi quit
    public void CleanupBeforeQuit()
    {
        CleanUpAndDestroy();
    }

}
