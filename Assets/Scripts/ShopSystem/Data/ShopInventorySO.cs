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
    [SerializeField] private int restockMinutes = 1;

    [NonSerialized] private DateTime nextRestockTime;
    public DateTime NextRestockTime => nextRestockTime;
    //public event Action<TimeSpan> OnRestockTimeChanged;

    [TextArea]
    public string greeting;

    [Header("Portrait")]
    public Sprite portraitSprite;
    public RuntimeAnimatorController portraitAnimator;

    [Header("Items")]
    public List<ShopItem> items = new List<ShopItem>();
    public event Action<Dictionary<int, ShopItem>> OnShopInventoryUpdated;


    public void Initialize()
    {
        if (nextRestockTime == default)
        {
            nextRestockTime = DateTime.UtcNow.AddMinutes(restockMinutes);
        }
    }

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



    //RESTOCK LOGIC
    public void CheckRestock()
    {
        while (DateTime.UtcNow >= nextRestockTime)
        {
            RestockAllItems();
            nextRestockTime = nextRestockTime.AddMinutes(restockMinutes);
        }
    }

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

    //SAVE and LOAD
    public ShopSaveData GetSaveData()
    {
        ShopSaveData data = new();

        data.shopId = shopId;
        data.nextRestockTicks = nextRestockTime.Ticks;

        foreach (var item in items)
        {
            if (item == null || item.IsEmpty) continue;

            data.items.Add(new ShopItemSaveData
            {
                itemId = item.item.ItemId,
                currentStock = item.CurrentStock
            });
        }

        return data;
    }

    public void LoadFromData(ShopSaveData data)
    {
        if (data == null) return;

        nextRestockTime = new DateTime(data.nextRestockTicks, DateTimeKind.Utc);

        for (int i = 0; i < items.Count && i < data.items.Count; i++)
        {
            items[i].CurrentStock = data.items[i].currentStock;
        }
        CheckRestock();
        InformAboutChange();
    }


}
