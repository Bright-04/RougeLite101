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
}