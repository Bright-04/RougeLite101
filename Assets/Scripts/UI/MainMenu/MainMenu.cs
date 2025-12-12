using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        //InputManager.Instance.EnableGameplayMaps();
        SceneManager.LoadScene("GameHome");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
