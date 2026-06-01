using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ShopController : MonoBehaviour
{

    [Header("UI References")]
    [SerializeField]
    public ShopUI shopUI;

    public ShopInventorySO CurrentShopData { get; private set; }

    [SerializeField] private InventoryController inventoryController;

    //private void Awake()
    //{
    //    shopUI.OnClosingShop += CloseShop;
    //    shopUI.OnInitiateShopItemList += HandleShopItemList;
    //}

    //private void OnDestroy()
    //{
    //    shopUI.OnClosingShop -= CloseShop;
    //    shopUI.OnInitiateShopItemList -= HandleShopItemList;
    //}

    private void OnEnable()
    {
        shopUI.OnClosingShop += CloseShop;
        shopUI.OnInitiateShopItemList += HandleShopItemList;
    }

    private void OnDisable()
    {
        shopUI.OnClosingShop -= CloseShop;
        shopUI.OnInitiateShopItemList -= HandleShopItemList;
    }

    void Start()
    {
        // Đợi đến Start để đảm bảo InputManager đã Awake
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }
        //playerControls = InputManager.Instance.Controls;
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }
    }

    private void PrepareUI(ShopState currentState)
    {
        shopUI.OnShopItemDescriptionRequested -= HandleSellShopItemDescription;
        shopUI.OnShopItemDescriptionRequested -= HandleBuyShopItemDescription;
        if (currentState == ShopState.Buy)
        {
            shopUI.InitializedShopInventoryUI(CurrentShopData.GetListCount());
            //shopUI.OnShopItemDescriptionRequested -= HandleBuyShopItemDescription;
            shopUI.OnShopItemDescriptionRequested += HandleBuyShopItemDescription;
        }
        else if (currentState == ShopState.Sell)
        {
            shopUI.InitializedShopInventoryUI(inventoryController.CurrentInventoryData.GetListCount());
            //shopUI.OnShopItemDescriptionRequested -= HandleSellShopItemDescription;
            shopUI.OnShopItemDescriptionRequested += HandleSellShopItemDescription;
        }
        
    }

    private void PrepareShopData(ShopState currentState)
    {
        CurrentShopData.OnShopInventoryUpdated -= UpdateShopInventoryUI;
        if (currentState == ShopState.Buy)
        {
            CurrentShopData.OnShopInventoryUpdated -= UpdateShopInventoryUI;
            foreach (var item in CurrentShopData.GetCurrentShopInventoryState())
            {
                shopUI.UpdateData(item.Key, item.Value.item.ItemImage, item.Value.currentStock, item.Value.maxStock);
            }
            CurrentShopData.OnShopInventoryUpdated += UpdateShopInventoryUI;
        }
        else if (currentState == ShopState.Sell)
        {
            inventoryController.CurrentInventoryData.OnInventoryUpdated -= UpdateInventoryUI;
            foreach (var item in inventoryController.CurrentInventoryData.GetCurrentInventoryState())
            {
                shopUI.UpdateData(item.Key, item.Value.item.ItemImage, item.Value.quantity);
            }
            inventoryController.CurrentInventoryData.OnInventoryUpdated += UpdateInventoryUI;
        }
        
    }

    private void UpdateInventoryUI(Dictionary<int, InventoryItem> inventoryState)
    {
        shopUI.ResetAllItems();
        foreach (var item in inventoryState)
        {
            shopUI.UpdateData(item.Key, item.Value.item.ItemImage, item.Value.quantity);
        }
    }

    private void UpdateShopInventoryUI(Dictionary<int, ShopItem> inventoryState)
    {
        shopUI.ResetAllItems();
        foreach (var item in inventoryState)
        {
            shopUI.UpdateData(item.Key, item.Value.item.ItemImage, item.Value.currentStock, item.Value.maxStock);
        }
    }

   private void HandleShopItemList(ShopState currentState)
    {
        shopUI.ClearInventoryUI();
        PrepareUI(currentState);
        PrepareShopData(currentState);
    }

    private void HandleBuyShopItemDescription(int itemIndex)
    {
        ShopItem shopItem = CurrentShopData.GetItemAt(itemIndex);
        if (shopItem.IsEmpty)
        {
            shopUI.ResetSelection();
            return;
        }       

        ItemSO item = shopItem.item;
        string description = PrepareDescription(shopItem);
        shopUI.UpdateDescription(itemIndex, item.Name, description);
    }

    private void HandleSellShopItemDescription(int itemIndex)
    {
        InventoryItem shopItem = inventoryController.CurrentInventoryData.GetItemAt(itemIndex);
        if (shopItem.IsEmpty)
        {
            shopUI.ResetSelection();
            return;
        }

        ItemSO item = shopItem.item;
        string description = PrepareDescription(shopItem);
        shopUI.UpdateDescription(itemIndex, item.Name, description);
    }

    private string PrepareDescription(InventoryItem shopItem)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(shopItem.item.Description);
        sb.AppendLine();
        for (int i = 0; i < shopItem.item.modifiersData.Count; i++)
        {
            sb.Append($"{shopItem.item.modifiersData[i].StatModifier.ModifierName} " +
                $": {shopItem.item.modifiersData[i].value}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private string PrepareDescription(ShopItem shopItem)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(shopItem.item.Description);
        sb.AppendLine();
        for (int i = 0; i < shopItem.item.modifiersData.Count; i++)
        {
            sb.Append($"{shopItem.item.modifiersData[i].StatModifier.ModifierName} " +
                $": {shopItem.item.modifiersData[i].value}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public void OpenShop(ShopInventorySO shopData,GameObject interactor)
    {
        if (!InputManager.Instance.IsUIActive())
        {
            CurrentShopData = shopData;
            shopUI.ShowShop(CurrentShopData);          
            // Disable gameplay inputs
            InputManager.Instance.EnableUIMap();
            Debug.Log("OPEN Shop");
        }
    }

    public void CloseShop()
    {
        if (InputManager.Instance.IsUIActive())
        {
            shopUI.HideShop();
            // Enable gameplay inputs
            InputManager.Instance.DisableUIMap();
            Debug.Log("CLOSE Shop");
        }
    }
}
