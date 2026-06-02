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
    [SerializeField] private PlayerMoney playerMoney;

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

    private void Awake()
    {
        Debug.Log("InventoryController spawned");
    }

    private void OnDestroy()
    {
        Debug.Log("InventoryController destroyed");
    }

    private void OnEnable()
    {
        shopUI.OnClosingShop += CloseShop;
        shopUI.OnInitiateShopItemList += HandleShopItemList;
        shopUI.OnBuyRequested += BuyItem;
        shopUI.OnSellRequested += SellItem;
    }

    private void OnDisable()
    {
        shopUI.OnClosingShop -= CloseShop;
        shopUI.OnInitiateShopItemList -= HandleShopItemList;
        shopUI.OnBuyRequested -= BuyItem;
        shopUI.OnSellRequested -= SellItem;
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

        if (playerMoney == null)
        {
            playerMoney = FindFirstObjectByType<PlayerMoney>();
        }
    }

    private void PrepareUI(ShopState currentState)
    {
        shopUI.OnShopItemDescriptionRequested -= HandleSellShopItemDescription;
        shopUI.OnShopItemDescriptionRequested -= HandleBuyShopItemDescription;
        if (currentState == ShopState.Buy)
        {
            shopUI.InitializedShopInventoryUI(CurrentShopData.GetListCount());
            shopUI.OnShopItemDescriptionRequested += HandleBuyShopItemDescription;
        }
        else if (currentState == ShopState.Sell)
        {
            shopUI.InitializedShopInventoryUI(inventoryController.CurrentInventoryData.GetNonEmptyItems().Count);
            shopUI.OnShopItemDescriptionRequested += HandleSellShopItemDescription;
        }
        
    }

    private void PrepareShopData(ShopState currentState)
    {
        inventoryController.CurrentInventoryData.OnInventoryUpdated -= UpdateInventoryUI;
        CurrentShopData.OnShopInventoryUpdated -= UpdateShopInventoryUI;
        if (currentState == ShopState.Buy)
        {
            foreach (var item in CurrentShopData.GetCurrentShopInventoryState())
            {
                shopUI.UpdateData(item.Key, item.Value.item.ItemImage, item.Value.currentStock, item.Value.maxStock);
            }
            CurrentShopData.OnShopInventoryUpdated += UpdateShopInventoryUI;
        }
        else if (currentState == ShopState.Sell)
        {
            var items =inventoryController.CurrentInventoryData.GetNonEmptyItems();
            for (int i = 0; i < items.Count; i++)
            {
                shopUI.UpdateData(i, items[i].item.ItemImage, items[i].quantity);
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
        shopUI.SetupBuyAmount(shopItem.currentStock);

        ItemSO item = shopItem.item;
        string description = PrepareDescription(shopItem);
        shopUI.UpdateDescription(itemIndex, item.Name, description);
    }

    private void HandleSellShopItemDescription(int itemIndex)
    {
        InventoryItem shopItem = inventoryController.CurrentInventoryData.GetNonEmptyItems()[itemIndex];
        if (shopItem.IsEmpty)
        {
            shopUI.ResetSelection();
            return;
        }
        shopUI.SetupSellAmount(shopItem.quantity);

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
        sb.AppendLine();
        sb.Append("Selling Price: " + shopItem.item.SellPrice);
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
        sb.AppendLine();
        sb.Append("Buying Price: " + shopItem.item.BuyPrice);
        return sb.ToString();
    }

    private void BuyItem(int itemIndex, int amount)
    {
        ShopItem shopItem = CurrentShopData.GetItemAt(itemIndex);
        if (shopItem.IsEmpty) return;
        if (!CurrentShopData.CanBuy(itemIndex, amount)) return;

        int totalCost = shopItem.item.BuyPrice * amount;
        if (!playerMoney.SpendGold(totalCost))
        {
            Debug.Log("Not enough gold!");
            return;
        }

        shopUI.ResetSelection(); // reset description
        inventoryController.CurrentInventoryData.AddItem(shopItem.item, amount);
        CurrentShopData.RemoveStock(itemIndex, amount);
        RefreshCurrentView();
    }

    private void SellItem(int itemIndex, int amount)
    {
        var items = inventoryController.CurrentInventoryData.GetNonEmptyItemsWithIndex();

        var entry = items[itemIndex];

        int realSlot = entry.slotIndex;
        InventoryItem invItem = entry.item;

        if (invItem.IsEmpty) return;
        amount = Mathf.Min(amount, invItem.quantity);
        int totalGain = invItem.item.SellPrice * amount;
        playerMoney.AddGold(totalGain);

        shopUI.ResetSelection(); // reset description
        inventoryController.CurrentInventoryData.RemoveItem(realSlot, amount);
        RefreshCurrentView();
        Debug.Log($"Sold {invItem.item.Name} x{amount}");
    }

    private void RefreshCurrentView()
    {
        if (shopUI.CurrentState == ShopState.Buy)
        {
            HandleShopItemList(ShopState.Buy);
        }
        else if (shopUI.CurrentState == ShopState.Sell)
        {
            HandleShopItemList(ShopState.Sell);
        }
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
