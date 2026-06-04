using System;
using System.Collections.Generic;

[Serializable]
public class ShopRestockData
{
    public List<ShopSaveEntry> shops = new List<ShopSaveEntry>();
}

[Serializable]
public class ShopSaveEntry
{
    public string shopId;
    public long nextRestockTicks;
    public List<ShopStockEntry> stockEntries = new List<ShopStockEntry>();
}

[Serializable]
public class ShopStockEntry
{
    public int slotIndex;
    public int currentStock;
    public string debugItemName;
}
