using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeSceneOnCollide : MonoBehaviour
{
    [SerializeField] private string changeSceneName;
    [SerializeField] private bool useLoadingScreen = true;
    
    private bool hasTriggered = false;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            Debug.Log("Change to " + changeSceneName);
            
            // Use loading screen if available and enabled
            if (useLoadingScreen && LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.LoadSceneAsync(changeSceneName);
            }
            else
            {
                SceneManager.LoadScene(changeSceneName);
            }
        }
    }
}
