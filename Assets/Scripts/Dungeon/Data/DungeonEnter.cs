using UnityEngine;

public class DungeonEnter : MonoBehaviour, IInteractable
{
    [SerializeField] string changeSceneName;


    public void Interact(GameObject interactor)
    {
        AutoSaveManager.TrySaveActiveSceneState();
        Debug.Log("Change to " + changeSceneName);
        SceneLoadingRequest.LoadSceneWithLoading(changeSceneName);
    }

    public string GetInteractionText(GameObject interactor)
    {
        return $"[F] Enter Dungeon";
    }
}
