using System;
using UnityEngine;

[Serializable]
public class ShopItem
{
    public ItemSO item;
    public int currentStock = 99;
    public int maxStock = 99;

    public bool IsEmpty => item == null;

}
