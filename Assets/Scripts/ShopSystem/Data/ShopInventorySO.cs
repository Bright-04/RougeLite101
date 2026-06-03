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

    //public void SetNextRestockTime(DateTime value)
    //{
    //    nextRestockTime = value;
    //}
}
