using System;
using System.Collections.Generic;

[Serializable]
public class SafeInventorySaveData
{
    public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();

    public SafeInventorySaveData(InventorySO inventory)
    {
        foreach (var pair in inventory.GetCurrentInventoryState())
        {
            InventoryItem item = pair.Value;

            items.Add(new InventoryItemSaveData
            {
                itemId = item.item.ItemId,
                quantity = item.quantity
            });
        }
    }
}

[Serializable]
public class DungeonInventorySaveData
{
    public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();
}

[Serializable]
public class InventoryItemSaveData
{
    public string itemId;
    public int quantity;
}
