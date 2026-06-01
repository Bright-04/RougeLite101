using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;

[CreateAssetMenu(fileName = "ShopInventory", menuName = "Shop/Inventory")]
public class ShopInventorySO : ScriptableObject
{
    public string shopName;

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
}
