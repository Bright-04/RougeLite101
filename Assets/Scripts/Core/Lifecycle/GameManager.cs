using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Persistent Objects")]
    public GameObject[] persistentObject;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool thisIsInActiveScene = gameObject.scene == activeScene;
            bool instanceIsInActiveScene = Instance.gameObject.scene == activeScene;

            if (thisIsInActiveScene && !instanceIsInActiveScene)
            {
                Destroy(Instance.gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
    }

    private void Start()
    {
        EnforceSingleSceneRuntime();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void EnforceSingleSceneRuntime()
    {
        if (SceneManager.sceneCount <= 1)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();

        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (!loadedScene.isLoaded || loadedScene == activeScene)
            {
                continue;
            }

            Debug.Log($"GameManager: Unloading extra runtime scene '{loadedScene.name}' to keep a single gameplay scene active.");
            SceneManager.UnloadSceneAsync(loadedScene);
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
