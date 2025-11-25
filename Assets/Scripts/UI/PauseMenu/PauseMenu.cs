using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseCanvas;
   
    private PlayerControls playerControls;
    private bool isPaused = false;

    private void Awake()
    {
        // Ẩn pause menu ban đầu
        if (pauseCanvas)
            pauseCanvas.SetActive(false);
    }

    private void Start()
    {
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

    //private void OnEnable()
    //{
    //    // Subscribe to ESC key
    //    // Kiểm tra null trước khi unsubscribe
    //    if (playerControls != null)
    //    {
    //        playerControls.NavigateUI.OpenPauseMenu.performed += OnOpenPausePerformed;
    //    }


    //}

    //private void OnDisable()
    //{
    //    // Kiểm tra null trước khi unsubscribe
    //    if (playerControls != null)
    //    {
    //        playerControls.NavigateUI.OpenPauseMenu.performed -= OnOpenPausePerformed;
    //    }

    //}

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
        if (isPaused) return;

        isPaused = true;
        pauseCanvas.SetActive(true);

        // Disable gameplay inputs
        InputManager.Instance.EnableUIMap();
        

        Debug.Log("Game PAUSED");
    }

    
    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        pauseCanvas.SetActive(false);

        // Enable gameplay inputs
        InputManager.Instance.DisableUIMap();

        Debug.Log("Game RESUMED");
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
