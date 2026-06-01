using System;
using UnityEngine;

[Serializable]
public class ShopItem
{
    public ItemSO item;

    public int buyPrice = 50;

    public int sellPrice = 25;

    public int stock = -1;
}
