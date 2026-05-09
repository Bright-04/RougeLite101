using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class InventoryController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    public InventoryUI inventoryUI;
    [SerializeField]
    private InventorySO safeInventoryData;
    [SerializeField]
    private InventorySO dungeonInventoryData;

    public InventorySO CurrentInventoryData { get; private set;}

    public List<InventoryItem> initialItems = new List<InventoryItem>();

    private PlayerControls playerControls;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        //UpdateInventory(SceneManager.GetActiveScene().name);      

        // Đợi đến Start để đảm bảo InputManager đã Awake
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }
        playerControls = InputManager.Instance.Controls;

        // Subscribe ESC key
        playerControls.NavigateUI.OpenInventory.performed += OnOpenInventoryPerformed;
        playerControls.UI.CloseInventory.performed += OnCloseInventoryPerformed;
    }

    private void PrepareUI()
    {
        inventoryUI.InitializedInventoryUI(CurrentInventoryData.Size);

        // Unsubscribe trước để tránh duplicate
        inventoryUI.OnDescriptionRequested -= HandleDescriptionRequest;
        inventoryUI.OnSwapItems -= HandleSwapItems;
        inventoryUI.OnStartDragging -= HandleDragging;
        inventoryUI.OnItemActionRequested -= HandleItemActionRequest;
        // Rồi mới subscribe lại
        inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
        inventoryUI.OnSwapItems += HandleSwapItems;
        inventoryUI.OnStartDragging += HandleDragging;
        inventoryUI.OnItemActionRequested += HandleItemActionRequest;
    }

    private void PrepareInventoryData()
    {
        // Unsubscribe trước!
        CurrentInventoryData.OnInventoryUpdated -= UpdateInventoryUI;

        CurrentInventoryData.Initialize();
        CurrentInventoryData.OnInventoryUpdated += UpdateInventoryUI;
        foreach (InventoryItem item in initialItems)
        {
            if (item.IsEmpty)
            {
                continue;
            }
            CurrentInventoryData.AddItem(item);
        }
    }

    private void UpdateInventoryUI(Dictionary<int, InventoryItem> inventoryState)
    {
        inventoryUI.ResetAllItems();
        foreach (var item in inventoryState)
        {
            inventoryUI.UpdateData(item.Key, item.Value.item.ItemImage,
                item.Value.quantity);
        }
    }

    private void HandleDescriptionRequest(int itemIndex)
    {
        InventoryItem inventoryItem = CurrentInventoryData.GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty)
        {
            inventoryUI.ResetSelection();
            return;
        }
        ItemSO item = inventoryItem.item;
        string description = PrepareDescription(inventoryItem);
        inventoryUI.UpdateDescription(itemIndex, item.ItemImage, item.name, description);

    }

    private string PrepareDescription(InventoryItem inventoryItem)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(inventoryItem.item.Description);
        sb.AppendLine();
        for (int i = 0; i < inventoryItem.item.modifiersData.Count; i++)
        {
            sb.Append($"{inventoryItem.item.modifiersData[i].statModifier.ModifierName} " +
                $": {inventoryItem.item.modifiersData[i].value}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private void HandleSwapItems(int itemIndex_1, int itemIndex_2)
    {
        CurrentInventoryData.SwapItems(itemIndex_1, itemIndex_2);
    }
    private void HandleDragging(int itemIndex)
    {
        InventoryItem inventoryItem = CurrentInventoryData.GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty)
        {
            return;
        }       
        inventoryUI.CreateDraggedItem(inventoryItem.item.ItemImage, inventoryItem.quantity);
    }

    private void HandleItemActionRequest(int itemIndex)
    {
        InventoryItem inventoryItem = CurrentInventoryData.GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty)
        {
            return;
        }

        IItemAction itemAction = inventoryItem.item as IItemAction;

        if (itemAction != null)
        {

            itemAction.PerformAction(gameObject);
        }

        IDestroyableItem destroyableItem = inventoryItem.item as IDestroyableItem;
        if (destroyableItem != null)
        {
            //inventoryUI.AddAction("Drop", () => DropItem(itemIndex, inventoryItem.quantity));
        }


    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.NavigateUI.OpenInventory.performed -= OnOpenInventoryPerformed;
            playerControls.UI.CloseInventory.performed -= OnCloseInventoryPerformed;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe inventory CŨ trước khi switch
        if (CurrentInventoryData != null)
        {
            CurrentInventoryData.OnInventoryUpdated -= UpdateInventoryUI;
        }

        // Tìm lại InventoryUI của scene mới
        if (inventoryUI == null)
        {
            inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);

            if (inventoryUI == null)
            {
                Debug.LogError("InventoryUI not found in scene!");
                return;
            }
        }
        inventoryUI.ClearInventoryUI(); // clear hết item cũ trước
        inventoryUI.HideInventory();
        UpdateInventory(scene.name);
        PrepareUI();
        PrepareInventoryData();
    }

    private void UpdateInventory(string sceneName)
    {
        CurrentInventoryData = sceneName == "Dungeon" ? dungeonInventoryData : safeInventoryData;
        Debug.Log($"Inventory switch to: {CurrentInventoryData.name}");
    }

    private void OnOpenInventoryPerformed(InputAction.CallbackContext ctx)
    {
        if (!inventoryUI.IsInventoryActive() && !InputManager.Instance.IsUIActive())
        {
            inventoryUI.ShowInventory();
            foreach(var item in CurrentInventoryData.GetCurrentInventoryState())
            {
                inventoryUI.UpdateData(item.Key, item.Value.item.ItemImage, item.Value.quantity);
            }
            // Disable gameplay inputs
            InputManager.Instance.EnableUIMap();
            Debug.Log("OPEN inventory");
        }
    }

    private void OnCloseInventoryPerformed(InputAction.CallbackContext ctx)
    {
        if (inventoryUI.IsInventoryActive() && InputManager.Instance.IsUIActive())
        {
            inventoryUI.HideInventory();
            // Enable gameplay inputs
            InputManager.Instance.DisableUIMap();
            Debug.Log("CLOSE inventory");
        }
    }
}
