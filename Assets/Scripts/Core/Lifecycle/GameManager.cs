using System.Collections.Generic;
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
            RemoveDuplicateRuntimeFromScene(this);
            return;
        }

        // This manager owns the persistent runtime set, and Unity moves those objects into the internal DontDestroyOnLoad scene.
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        MarkPersistentObjects();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void MarkPersistentObjects()
    {
        if (persistentObject == null)
        {
            return;
        }

        foreach (GameObject obj in persistentObject)
        {
            if (obj == null)
            {
                continue;
            }

            if (obj.transform.parent != null)
            {
                Debug.LogWarning(
                    $"GameManager: Persistent object '{obj.name}' is not a root object. Skipping DontDestroyOnLoad.",
                    obj);
                continue;
            }

            DontDestroyOnLoad(obj);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this)
        {
            return;
        }

        // Newly loaded scenes may bring their own GameManager, Player, Canvas_UI, and SaveManager, so scene-local duplicates must be reconciled.
        GameManager[] loadedManagers = FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameManager manager in loadedManagers)
        {
            if (manager == null || manager == this || manager.gameObject.scene != scene)
            {
                continue;
            }

            RemoveDuplicateRuntimeFromScene(manager);
        }
    }

    private void RemoveDuplicateRuntimeFromScene(GameManager duplicate)
    {
        if (duplicate == null || Instance == null)
        {
            return;
        }

        HashSet<GameObject> protectedObjects = BuildProtectedPrimarySet();
        Scene duplicateScene = duplicate.gameObject.scene;

        // Only clean objects explicitly listed by the duplicate manager, which avoids broad scene cleanup.
        if (duplicate.persistentObject != null)
        {
            foreach (GameObject obj in duplicate.persistentObject)
            {
                if (!CanDestroyDuplicateObject(obj, duplicate, duplicateScene, protectedObjects))
                {
                    continue;
                }

                DestroyDuplicateImmediately(obj);
            }
        }

        DestroyDuplicateImmediately(duplicate.gameObject);
    }

    private HashSet<GameObject> BuildProtectedPrimarySet()
    {
        HashSet<GameObject> protectedObjects = new HashSet<GameObject>();

        if (Instance == null)
        {
            return protectedObjects;
        }

        protectedObjects.Add(Instance.gameObject);

        if (Instance.persistentObject == null)
        {
            return protectedObjects;
        }

        foreach (GameObject obj in Instance.persistentObject)
        {
            if (obj != null)
            {
                protectedObjects.Add(obj);
            }
        }

        return protectedObjects;
    }

    private bool CanDestroyDuplicateObject(GameObject obj, GameManager duplicate, Scene duplicateScene, HashSet<GameObject> protectedObjects)
    {
        if (obj == null || obj == duplicate.gameObject)
        {
            return false;
        }

        if (obj.scene != duplicateScene)
        {
            return false;
        }

        if (obj == Instance.gameObject || protectedObjects.Contains(obj))
        {
            return false;
        }

        return true;
    }

    private static void DestroyDuplicateImmediately(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        // This is intentionally limited to duplicate scene-load cleanup because runtime validation showed deferred Destroy() could leave duplicate objects alive here.
        DestroyImmediate(target);
    }

    private void CleanUpAndDestroy()
    {
        if (persistentObject != null)
        {
            foreach (GameObject obj in persistentObject)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        Destroy(gameObject);
    }

    public void CleanupBeforeQuit()
    {
        // Player and Canvas_UI persistence are compatibility risks; weapon carry-over should remain data-level behavior rather than justify permanent full-Player persistence.
        CleanUpAndDestroy();
    }
}
