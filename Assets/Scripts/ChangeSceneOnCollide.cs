using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeSceneOnCollide : MonoBehaviour
{
    [SerializeField] string changeSceneName;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Change to " + changeSceneName);
            SceneManager.LoadScene(changeSceneName);
        }
    }
}
