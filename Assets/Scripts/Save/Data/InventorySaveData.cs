using System;
using System.Collections.Generic;

[Serializable]
public class SafeInventorySaveData
{
    public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();
}

[Serializable]
public class DungeonInventorySaveData
{
    public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();
}

[Serializable]
public class InventoryItemSaveData
{
    public string stableItemId;
    public int quantity;
    public string debugItemName;
}
