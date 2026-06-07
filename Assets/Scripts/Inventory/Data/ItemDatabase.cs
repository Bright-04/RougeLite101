using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private List<ItemSO> items;

    private Dictionary<string, ItemSO> lookup;

    public void Initialize()
    {
        lookup = new();

        foreach (var item in items)
        {
            lookup[item.ItemId] = item;
        }
    }

    public ItemSO GetItem(string id)
    {
        if (lookup == null)
        {
            Initialize();
        }
           
        lookup.TryGetValue(id, out var item);

        return item;
    }
}
