using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInventory", menuName = "Inventory/InventorySO")]
public class InventorySO : ScriptableObject
{
    [SerializeField]
    private List<InventoryItem> inventoryItems;

    [field: SerializeField]
    public int Size { get; private set; } = 30;

    public bool IsInitialized => inventoryItems != null && inventoryItems.Count == Size;

    public event Action<Dictionary<int, InventoryItem>> OnInventoryUpdated;

    public void Initialize()
    {
        inventoryItems = new List<InventoryItem>();
        for (int i = 0; i < Size; i++)
        {
            inventoryItems.Add(InventoryItem.GetEmptyItem());
        }
    }

    public int AddItem(ItemSO item, int quantity)
    {
        EnsureInitialized();

        if (item.IsStackable == false)
        {
            while (quantity > 0 && IsInventoryFull() == false)
            {
                quantity -= AddItemToFirstFreeSlot(item, 1);
            }
            InformAboutChange();
            return quantity;
        }
        quantity = AddStackableItem(item, quantity);
        InformAboutChange();
        return quantity;
    }


    private int AddStackableItem(ItemSO item, int quantity)
    {
        EnsureInitialized();

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i].IsEmpty)
            {
                continue;
            }
               
            if (inventoryItems[i].item.ID == item.ID)
            {
                int amountPossibleToTake = inventoryItems[i].item.MaxStackSize - inventoryItems[i].quantity;

                if (quantity > amountPossibleToTake)
                {
                    inventoryItems[i] = inventoryItems[i].ChangeQuantity(inventoryItems[i].item.MaxStackSize);
                    quantity -= amountPossibleToTake;
                }
                else
                {
                    inventoryItems[i] = inventoryItems[i].ChangeQuantity(inventoryItems[i].quantity + quantity);
                    InformAboutChange();
                    return 0;
                }
            }
        }
        while (quantity > 0 && IsInventoryFull() == false)
        {
            int newQuantity = Mathf.Clamp(quantity, 0, item.MaxStackSize);
            quantity -= newQuantity;
            AddItemToFirstFreeSlot(item, newQuantity);
        }
        return quantity;
    }

    private int AddItemToFirstFreeSlot(ItemSO item, int quantity)
    {
        InventoryItem newItem = new InventoryItem
        {
            quantity = quantity,
            item = item
        };

        for(int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i].IsEmpty)
            {
                inventoryItems[i] = newItem;
                return quantity;
            }
        }
        return 0;
    }

    public bool IsInventoryFull()
            => inventoryItems.Where(item => item.IsEmpty).Any() == false;

    public Dictionary<int, InventoryItem> GetCurrentInventoryState()
    {
        EnsureInitialized();

        Dictionary<int, InventoryItem> returnValue = new Dictionary<int, InventoryItem>();

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i].IsEmpty)
                continue;
            returnValue[i] = inventoryItems[i];
        }
        return returnValue;
    }

    public int GetListCount()
    {
        int count = 0;
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i].IsEmpty) continue;
            count++;
        }
        return count;
    }

    public InventoryItem GetItemAt(int itemIndex)
    {
        EnsureInitialized();
        return inventoryItems[itemIndex];
    }

    public void AddItem(InventoryItem item)
    {
        AddItem(item.item, item.quantity);
    }

    public void SwapItems(int itemIndex_1, int itemIndex_2)
    {
        EnsureInitialized();
        if (itemIndex_1 == itemIndex_2)
        {
            return;
        }

        if (!IsValidIndex(itemIndex_1) || !IsValidIndex(itemIndex_2))
        {
            Debug.LogWarning(
                $"InventorySO: Ignoring invalid swap indices {itemIndex_1} and {itemIndex_2}. Inventory size: {inventoryItems.Count}.",
                this);
            return;
        }

        InventoryItem item1 = inventoryItems[itemIndex_1];
        inventoryItems[itemIndex_1] = inventoryItems[itemIndex_2];
        inventoryItems[itemIndex_2] = item1;
        InformAboutChange();
    }

    private bool IsValidIndex(int index)
        => index >= 0 && index < inventoryItems.Count;

    private void InformAboutChange()
    {
        OnInventoryUpdated?.Invoke(GetCurrentInventoryState());
    }

    public void RemoveItem(int itemIndex, int amount)
    {
        EnsureInitialized();

        if (inventoryItems.Count > itemIndex)
        {
            if (inventoryItems[itemIndex].IsEmpty)
            {
                return;
            }
                
            int reminder = inventoryItems[itemIndex].quantity - amount;
            if (reminder <= 0)
            {
                inventoryItems[itemIndex] = InventoryItem.GetEmptyItem();
            }
            else
            {
                inventoryItems[itemIndex] = inventoryItems[itemIndex].ChangeQuantity(reminder);
            }
                

            InformAboutChange();
        }
    }

    public void RemoveItem(ItemSO item, int amount)
    {
        EnsureInitialized();

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i].IsEmpty) continue;
            if (inventoryItems[i].item.ID != item.ID)continue;

            RemoveItem(i, amount);
            return;
        }
    }

    public void Clear()
    {
        EnsureInitialized();

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            inventoryItems[i] = InventoryItem.GetEmptyItem();
        }

        InformAboutChange();
    }

    public void TransferAllTo(InventorySO targetInventory)
    {
        if (targetInventory == null || targetInventory == this)
        {
            return;
        }

        EnsureInitialized();
        targetInventory.EnsureInitialized();

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            InventoryItem item = inventoryItems[i];
            if (item.IsEmpty)
            {
                continue;
            }

            int remainder = targetInventory.AddItem(item.item, item.quantity);
            int transferredAmount = item.quantity - remainder;
            if (transferredAmount <= 0)
            {
                continue;
            }

            inventoryItems[i] = remainder > 0
                ? item.ChangeQuantity(remainder)
                : InventoryItem.GetEmptyItem();
        }

        InformAboutChange();
    }

    public void TransferItemTo(InventorySO targetInventory, int sourceIndex, int amount)
    {
        if (targetInventory == null || targetInventory == this || amount <= 0)
        {
            return;
        }

        EnsureInitialized();
        targetInventory.EnsureInitialized();

        if (sourceIndex < 0 || sourceIndex >= inventoryItems.Count)
        {
            return;
        }

        InventoryItem sourceItem = inventoryItems[sourceIndex];
        if (sourceItem.IsEmpty)
        {
            return;
        }

        int clampedAmount = Mathf.Min(amount, sourceItem.quantity);
        int remainder = targetInventory.AddItem(sourceItem.item, clampedAmount);
        int transferredAmount = clampedAmount - remainder;
        if (transferredAmount <= 0)
        {
            return;
        }

        int remainingQuantity = sourceItem.quantity - transferredAmount;
        inventoryItems[sourceIndex] = remainingQuantity > 0
            ? sourceItem.ChangeQuantity(remainingQuantity)
            : InventoryItem.GetEmptyItem();

        InformAboutChange();
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            Initialize();
        }
    }

    public void SetItemAt(int slotIndex, ItemSO item, int quantity)
    {
        EnsureInitialized();

        if (slotIndex < 0 || slotIndex >= inventoryItems.Count) return;

        inventoryItems[slotIndex] = new InventoryItem
        {
            item = item,
            quantity = quantity
        };
        InformAboutChange();
    }

    //for shop
    public List<InventoryItem> GetNonEmptyItems()
    {
        EnsureInitialized();
        return inventoryItems.Where(x => !x.IsEmpty).ToList();
    }

    public List<(int slotIndex, InventoryItem item)> GetNonEmptyItemsWithIndex()
    {
        EnsureInitialized();

        List<(int, InventoryItem)> result = new();

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (!inventoryItems[i].IsEmpty)
            {
                result.Add((i, inventoryItems[i]));
            }
        }

        return result;
    }

}

[Serializable]
public struct InventoryItem
{
    public int quantity;
    public ItemSO item;
    public bool IsEmpty => item == null;

    public InventoryItem ChangeQuantity(int newQuantity)
    {
        return new InventoryItem
        {
            item = this.item,
            quantity = newQuantity,
        };
    }

    public static InventoryItem GetEmptyItem()
        => new InventoryItem
        {
            item = null,
            quantity = 0,
        };
}
 
