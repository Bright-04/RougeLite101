using UnityEngine;
using UnityEngine.InputSystem;

public class StatsMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject statsMenu;
    private PlayerControls playerControls;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if(statsMenu)
        {
            statsMenu.SetActive(false);
        }
        // Đợi đến Start để đảm bảo InputManager đã Awake
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }
        playerControls = InputManager.Instance.Controls;

        // Subscribe ESC key
        playerControls.NavigateUI.OpenStatsMenu.performed += OnOpenStatsPerformed;
        playerControls.UI.CloseStatsMenu.performed += OnCloseStatsPerformed;
    }
    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.NavigateUI.OpenStatsMenu.performed -= OnOpenStatsPerformed;
            playerControls.UI.CloseStatsMenu.performed -= OnCloseStatsPerformed;
        }
            
    }
    private void OnOpenStatsPerformed(InputAction.CallbackContext ctx)
    {
        statsMenu.SetActive(true);
        // Disable gameplay inputs
        InputManager.Instance.EnableUIMap();
        Debug.Log("OPEN Stats Menu");
    }

    private void OnCloseStatsPerformed(InputAction.CallbackContext ctx)
    {
        statsMenu.SetActive(false);
        // Enable gameplay inputs
        InputManager.Instance.DisableUIMap();
        Debug.Log("CLOSE Stats Menu");
    }

    

    
}
