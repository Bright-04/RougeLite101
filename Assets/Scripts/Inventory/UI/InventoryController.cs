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
    [SerializeField]
    private GameObject pickupableItemPrefab;
    [SerializeField]
    private Transform playerTransform;
    [SerializeField]
    private ItemDatabase itemDatabase;

    public InventorySO CurrentInventoryData { get; private set;}
    public InventorySO SafeInventory => safeInventoryData;
    public InventorySO DungeonInventory => dungeonInventoryData;

    public List<InventoryItem> initialItems = new List<InventoryItem>();

    private PlayerControls playerControls;
    private bool isPlayerDead;

    //[SerializeField]
    //private AudioClip dropClip;

    //[SerializeField]
    //private AudioSource audioSource;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        if (inventoryUI.transferUIComponent != null)
        {
            inventoryUI.transferUIComponent.OnTransferFinished -= HandleDescriptionRequest;
        }

        // Rồi mới subscribe lại
        inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
        inventoryUI.OnSwapItems += HandleSwapItems;
        inventoryUI.OnStartDragging += HandleDragging;
        inventoryUI.OnItemActionRequested += HandleItemActionRequest;
        if (inventoryUI.transferUIComponent != null)
        {
            inventoryUI.transferUIComponent.OnTransferFinished += HandleDescriptionRequest;
        }
    }

    private void PrepareInventoryData()
    {
        // Unsubscribe trước!
        CurrentInventoryData.OnInventoryUpdated -= UpdateInventoryUI;
        if (!CurrentInventoryData.IsInitialized)
        {
            CurrentInventoryData.Initialize();
            if (CurrentInventoryData == safeInventoryData)
            {
                foreach (InventoryItem item in initialItems)
                {
                    if (item.IsEmpty)
                    {
                        continue;
                    }

                    CurrentInventoryData.AddItem(item);
                }
            }
        }

        CurrentInventoryData.OnInventoryUpdated += UpdateInventoryUI;
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

        if (CurrentInventoryData == safeInventoryData)
        {
            inventoryUI.ShowTransferUI(safeInventoryData, dungeonInventoryData, itemIndex);
        }
        else
        {
            inventoryUI.HideTransferUI();
        }

        ItemSO item = inventoryItem.item;
        string description = PrepareDescription(inventoryItem);
        inventoryUI.UpdateDescription(itemIndex, item.ItemImage, item.Name, description);

    }

    private string PrepareDescription(InventoryItem inventoryItem)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(inventoryItem.item.Description);
        sb.AppendLine();
        for (int i = 0; i < inventoryItem.item.modifiersData.Count; i++)
        {
            sb.Append($"{inventoryItem.item.modifiersData[i].StatModifier.ModifierName} " +
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

        if (!inventoryUI.HasActionPanel)
        {
            IItemAction immediateAction = inventoryItem.item as IItemAction;
            if (immediateAction != null)
            {
                PerformAction(itemIndex);
                return;
            }

            if (inventoryItem.item is IDestroyableItem)
            {
                DropItem(itemIndex, inventoryItem.quantity);
            }

            return;
        }

        IItemAction itemAction = inventoryItem.item as IItemAction;
        if (itemAction != null)
        {
            HandleDescriptionRequest(itemIndex);
            inventoryUI.ShowItemAction(itemIndex);
            inventoryUI.AddAction(itemAction.ActionName, () => PerformAction(itemIndex));       
        }


        IDestroyableItem destroyableItem = inventoryItem.item as IDestroyableItem;
        if (destroyableItem != null)
        {
            inventoryUI.AddAction("Drop", () => DropItem(itemIndex, inventoryItem.quantity));
        }
    
    }

    private void DropItem(int itemIndex, int quantity)
    {       
        InventoryItem inventoryItem = CurrentInventoryData.GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty)
        {
            return;
        }

        if (pickupableItemPrefab == null)
        {
            Debug.LogError("Missing pickupableItemPrefab.", this);
            return;
        }          

        Transform dropOrigin = playerTransform != null ? playerTransform : transform;
        Vector3 dropPosition = dropOrigin.position + dropOrigin.right * 1f;     
        GameObject spawnedObject = Instantiate(pickupableItemPrefab, dropPosition, Quaternion.identity);

        Item droppedItem = spawnedObject.GetComponent<Item>();
        if (droppedItem != null)
        {
            droppedItem.InventoryItem = inventoryItem.item;
            droppedItem.Quantity = quantity;
        }
        CurrentInventoryData.RemoveItem(itemIndex, quantity);
        inventoryUI.ResetSelection();
        //audioSource.PlayOneShot(dropClip);
    }

    public void PerformAction(int itemIndex)
    {
        InventoryItem inventoryItem = CurrentInventoryData.GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty)
        {
            return;
        }
        

        IItemAction itemAction = inventoryItem.item as IItemAction;
        if (itemAction != null)
        {
            bool actionPerformed = itemAction.PerformAction(gameObject);
            if (!actionPerformed)
            {
                return;
            }

            if (inventoryItem.item is IDestroyableItem)
            {
                CurrentInventoryData.RemoveItem(itemIndex, 1);
            }

            //audioSource.PlayOneShot(itemAction.actionSFX);
            if (CurrentInventoryData.GetItemAt(itemIndex).IsEmpty)
            {
                inventoryUI.ResetSelection();
            }
            else
            {
                inventoryUI.itemsList[itemIndex].Select();
            }
                
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
        if (scene.name != "Dungeon" && CurrentInventoryData == dungeonInventoryData)
        {
            if (!isPlayerDead)
            {
                TransferDungeonToSafe();
                Debug.Log("transfer item from dungeon to safe");
            }
            else
            {
                Debug.Log("Player died => no transfer");
                isPlayerDead = false; // reset
            }
        }

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

    public void TransferDungeonToSafe()
    {
        dungeonInventoryData.TransferAllTo(safeInventoryData);
    }

    public void OnPlayerDeath()
    {
        isPlayerDead = true;
        dungeonInventoryData.Clear();
    }

    //========== LOAD SAFE INVENTORY =============
    //=======LOAD DATA===========
    public void LoadSafeInventory(SafeInventorySaveData inventoryData)
    {
        if (inventoryData == null)
        {
            Debug.LogWarning("InventoryController: LoadSafeInventory called with null data.", this);
            return;
        }

        safeInventoryData.Clear();

        foreach (var savedItem in inventoryData.items)
        {
            ItemSO item = itemDatabase.GetItem(savedItem.itemId);

            if (item == null)
            {
                Debug.LogWarning($"Missing item: {savedItem.itemId}");
                continue;
            }

            safeInventoryData.AddItem(item, savedItem.quantity);
        }
    }

}
