using UnityEngine;
using UnityEngine.InputSystem;

public class SafeInventoryController : MonoBehaviour
{
    [Header("UI References")]
    public InventoryUI inventoryUI;
    public int inventorySize = 20;
    private PlayerControls playerControls;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (inventoryUI == null)
        {
            inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);

            if (inventoryUI == null)
            {
                Debug.LogError("InventoryUI not found in scene!");
                return;
            }
        }
        inventoryUI.InitializedInventoryUI(inventorySize);
        inventoryUI.HideInventory();
        // Đợi đến Start để đảm bảo InputManager đã Awake
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }
        playerControls = InputManager.Instance.Controls;

        // Subscribe ESC key
        playerControls.NavigateUI.OpenInventory.performed += OnOpenSafeInventoryPerformed;
        playerControls.UI.CloseInventory.performed += OnCloseSafeInventoryPerformed;
    }

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.NavigateUI.OpenStatsMenu.performed -= OnOpenSafeInventoryPerformed;
            playerControls.UI.CloseStatsMenu.performed -= OnCloseSafeInventoryPerformed;
        }

    }

    private void OnOpenSafeInventoryPerformed(InputAction.CallbackContext ctx)
    {
        if (!inventoryUI.IsInventoryActive() && !InputManager.Instance.IsUIActive())
        {
            inventoryUI.ShowInventory();
            // Disable gameplay inputs
            InputManager.Instance.EnableUIMap();
            Debug.Log("OPEN safe inventory");
        }
    }

    private void OnCloseSafeInventoryPerformed(InputAction.CallbackContext ctx)
    {
        if (inventoryUI.IsInventoryActive() && InputManager.Instance.IsUIActive())
        {
            inventoryUI.HideInventory();
            // Enable gameplay inputs
            InputManager.Instance.DisableUIMap();
            Debug.Log("CLOSE safe inventory");
        }
    }
}
