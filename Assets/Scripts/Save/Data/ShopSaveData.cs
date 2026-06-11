using System;
using System.Collections.Generic;

[Serializable]
public class ShopSaveData
{
    public string shopId;

    // thời điểm restock tiếp theo
    public long nextRestockTicks;

    public List<ShopItemSaveData> items = new();
}

[Serializable]
public class ShopItemSaveData
{
    public string itemId;
    public int currentStock;
}
