using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;

[CreateAssetMenu(fileName = "ShopInventory", menuName = "Shop/Inventory")]
public class ShopInventorySO : ScriptableObject
{
    public string shopName;
    [Header("Save")]
    [SerializeField]
    private string shopId;

    public string ShopId => shopId;

    [Header("Restock")]
    [SerializeField] private int restockMinutes = 30;

    //private DateTime nextRestockTime;

    //public event Action<TimeSpan> OnRestockTimeChanged;

    [TextArea]
    public string greeting;

    [Header("Portrait")]
    public Sprite portraitSprite;
    public RuntimeAnimatorController portraitAnimator;

    [Header("Items")]
    public List<ShopItem> items = new List<ShopItem>();

    public event Action<Dictionary<int, ShopItem>> OnShopInventoryUpdated;

    public Dictionary<int, ShopItem> GetCurrentShopInventoryState()
    {
        Dictionary<int, ShopItem> returnValue =  new Dictionary<int, ShopItem>();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].IsEmpty) continue;
            returnValue[i] = items[i];
        }
        return returnValue;
    }

    public int GetListCount()
    {
        int count = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].IsEmpty) continue;
            count++;
        }
        return count;
    }

    public ShopItem GetItemAt(int itemIndex)
    {
        return items[itemIndex];
    }

    private void InformAboutChange()
    {
        OnShopInventoryUpdated?.Invoke(GetCurrentShopInventoryState());
    }

    //==========================
    // BUY / SELL LOGIC
    //==========================

    public bool CanBuy(int index, int amount)
    {
        ShopItem item = items[index];

        if (item.IsEmpty)
            return false;

        return item.CurrentStock >= amount;
    }

    public bool RemoveStock(int index, int amount)
    {
        ShopItem item = items[index];

        if (item.IsEmpty) return false;

        if (item.CurrentStock < amount) return false;

        item.CurrentStock -= amount;
        InformAboutChange();
        return true;
    }

    //public bool AddStock(int index, int amount)
    //{
    //    ShopItem item = items[index];

    //    if (item.IsEmpty) return false;
    //    item.currentStock += amount;
    //    if (item.currentStock > item.maxStock)
    //    {
    //        item.currentStock = item.maxStock;
    //    }
    //    InformAboutChange();
    //    return true;
    //}

    //RESTOCK LOGIC
    //public void InitializeRestock()
    //{
    //    if (nextRestockTime == default)
    //    {
    //        nextRestockTime = DateTime.UtcNow.AddMinutes(restockMinutes);
    //    }
    //}

    //public void TickRestock()
    //{
    //    TimeSpan remaining = nextRestockTime - DateTime.UtcNow;

    //    if (remaining <= TimeSpan.Zero)
    //    {
    //        RestockAllItems();

    //        nextRestockTime =
    //            DateTime.UtcNow.AddMinutes(restockMinutes);

    //        remaining =
    //            nextRestockTime - DateTime.UtcNow;
    //    }

    //    OnRestockTimeChanged?.Invoke(remaining);
    //}

    public void RestockAllItems()
    {
        foreach (var item in items)
        {
            if (item.IsEmpty)
                continue;
            item.CurrentStock = item.MaxStock;
        }

        InformAboutChange();
    }

    //public DateTime NextRestockTime
    //{
    //    get => nextRestockTime;
    //}
    
    // Temporary SetNextRestockTime function, will be replace later
    public void SetNextRestockTime(DateTime value)
    {
        nextRestockTime = value;
    }

    public ShopSaveEntry CreateSaveEntry()
    {
        ShopSaveEntry entry = new ShopSaveEntry
        {
            shopId = shopId,
            nextRestockTicks = nextRestockTime.Ticks
        };

        for (int i = 0; i < items.Count; i++)
        {
            ShopItem item = items[i];
            if (item == null || item.IsEmpty)
            {
                continue;
            }

            entry.stockEntries.Add(new ShopStockEntry
            {
                slotIndex = i,
                currentStock = item.currentStock,
                debugItemName = item.item != null ? item.item.Name : string.Empty
            });
        }

        return entry;
    }

    public void ApplySaveEntry(ShopSaveEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning($"ShopInventorySO '{name}': Cannot apply null save entry.", this);
            return;
        }

        if (!string.IsNullOrWhiteSpace(entry.shopId) && !string.Equals(entry.shopId, shopId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"ShopInventorySO '{name}': Save entry shopId '{entry.shopId}' does not match '{shopId}'.", this);
            return;
        }

        SetNextRestockTime(new DateTime(entry.nextRestockTicks, DateTimeKind.Utc));

        for (int i = 0; i < entry.stockEntries.Count; i++)
        {
            ShopStockEntry stockEntry = entry.stockEntries[i];
            if (stockEntry.slotIndex < 0 || stockEntry.slotIndex >= items.Count)
            {
                Debug.LogWarning($"ShopInventorySO '{name}': Stock entry index {stockEntry.slotIndex} is out of range.", this);
                continue;
            }

            ShopItem shopItem = items[stockEntry.slotIndex];
            if (shopItem == null || shopItem.IsEmpty)
            {
                Debug.LogWarning($"ShopInventorySO '{name}': Stock entry index {stockEntry.slotIndex} points to an empty slot.", this);
                continue;
            }

            string actualName = shopItem.item != null ? shopItem.item.Name : string.Empty;
            if (!string.IsNullOrWhiteSpace(stockEntry.debugItemName)
                && !string.Equals(stockEntry.debugItemName, actualName, StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"ShopInventorySO '{name}': Stock entry index {stockEntry.slotIndex} expected '{stockEntry.debugItemName}' but found '{actualName}'. Skipping this stock entry.",
                    this);
                continue;
            }

            shopItem.currentStock = Mathf.Clamp(stockEntry.currentStock, 0, shopItem.maxStock);
        }

        InformAboutChange();
    }
}
