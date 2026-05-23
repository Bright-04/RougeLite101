using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseCanvas;
   
    private PlayerControls playerControls;
    private bool isPaused;

    private void Start()
    {
        // Ẩn pause menu ban đầu
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
            isPaused = false;
        }
        // Đợi đến Start để đảm bảo InputManager đã Awake
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }

        playerControls = InputManager.Instance.Controls;

        // Subscribe ESC key
        playerControls.NavigateUI.OpenPauseMenu.performed += OnOpenPausePerformed;
        
        Debug.Log("PauseMenu initialized!");
    }

    private void OnDestroy()
    {
        if (playerControls != null)
            playerControls.NavigateUI.OpenPauseMenu.performed -= OnOpenPausePerformed;
    }


    private void OnOpenPausePerformed(InputAction.CallbackContext ctx)
    {
        Pause();
    }

    public void Pause()
    {
        if (RunResultController.Instance != null &&
            (RunResultController.Instance.IsResultActive || RunResultController.Instance.IsRunFinished))
        {
            return;
        }

        if (isPaused && !InputManager.Instance.IsUIActive()) return;

        isPaused = true;
        pauseCanvas.SetActive(true);

        // Disable gameplay inputs
        InputManager.Instance.EnableUIMap();
        
        Debug.Log("Game PAUSED");
    }

    
    public void Resume()
    {
        if (!isPaused && InputManager.Instance.IsUIActive()) return;

        isPaused = false;
        pauseCanvas.SetActive(false);

        // Enable gameplay inputs
        InputManager.Instance.DisableUIMap();

        Debug.Log("Game RESUMED");
    }

    public void HideForSystemOverlay()
    {
        if (pauseCanvas == null)
        {
            return;
        }

        isPaused = false;
        pauseCanvas.SetActive(false);
    }

    public void Quit()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Resume();
        if (activeScene.name == "GameHome")
        {
            GameManager.Instance.CleanupBeforeQuit();
            SceneManager.LoadScene("MainMenu");
        }
        else if(activeScene.name == "Dungeon")
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                player.transform.position = new Vector3(0f, 9f, 0f);
                SceneManager.LoadScene("GameHome");
            }
        }
        else
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                player.transform.position = new Vector3(0f, 9f, 0f);
                SceneManager.LoadScene("GameHome");
            }
        }
    }
}
