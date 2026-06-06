using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonEnter : MonoBehaviour, IInteractable
{
    [SerializeField] string changeSceneName;


    public void Interact(GameObject interactor)
    {
        AutoSaveManager.TrySaveActiveSceneState();
        Debug.Log("Change to " + changeSceneName);
        GetComponent<Collider2D>().enabled = false;
        SceneManager.LoadScene(changeSceneName);
    }

    public string GetInteractionText()
    {
        return $"[F] Enter Dungeon";
    }
}
