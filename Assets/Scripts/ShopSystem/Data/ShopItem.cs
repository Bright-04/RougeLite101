using System;
using UnityEngine;

[Serializable]
public class ShopItem
{
    public ItemSO item;
    [SerializeField]
    private int currentStock = 99;

    [SerializeField]
    private int maxStock = 99;

    public int CurrentStock
    {
        get => currentStock;
        set => currentStock = Mathf.Clamp(value, 0, maxStock);
    }

    public int MaxStock
    {
        get => maxStock;
        set
        {
            maxStock = Mathf.Max(0, value);
            currentStock = Mathf.Min(currentStock, maxStock);
        }
    }

    public bool IsEmpty => item == null;

}
