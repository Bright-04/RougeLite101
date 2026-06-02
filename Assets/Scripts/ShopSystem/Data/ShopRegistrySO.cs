using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopRegistry", menuName = "Shop/Shop Registry")]
public class ShopRegistrySO : ScriptableObject
{
    [SerializeField] private List<ShopInventorySO> shops = new List<ShopInventorySO>();

    private Dictionary<string, ShopInventorySO> lookupById;
    private bool isInitialized;

    public IReadOnlyList<ShopInventorySO> GetAll()
    {
        EnsureInitialized();
        return shops;
    }

    public ShopInventorySO GetById(string shopId)
    {
        if (string.IsNullOrWhiteSpace(shopId))
        {
            return null;
        }

        EnsureInitialized();
        if (lookupById.TryGetValue(shopId, out ShopInventorySO shop))
        {
            return shop;
        }

        return null;
    }

    private void OnEnable()
    {
        isInitialized = false;
        lookupById = null;
    }

    private void OnValidate()
    {
        ValidateRegistry(logContext: this);
        isInitialized = false;
        lookupById = null;
    }

    private void EnsureInitialized()
    {
        if (isInitialized && lookupById != null)
        {
            return;
        }

        ValidateRegistry(logContext: this);

        lookupById = new Dictionary<string, ShopInventorySO>(StringComparer.Ordinal);
        for (int i = 0; i < shops.Count; i++)
        {
            ShopInventorySO shop = shops[i];
            if (shop == null)
            {
                continue;
            }

            string shopId = shop.ShopId;
            if (string.IsNullOrWhiteSpace(shopId))
            {
                continue;
            }

            if (lookupById.ContainsKey(shopId))
            {
                continue;
            }

            lookupById.Add(shopId, shop);
        }

        isInitialized = true;
    }

    private void ValidateRegistry(UnityEngine.Object logContext)
    {
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < shops.Count; i++)
        {
            ShopInventorySO shop = shops[i];
            if (shop == null)
            {
                Debug.LogWarning($"ShopRegistrySO '{name}': shops[{i}] is null.", logContext);
                continue;
            }

            string shopId = shop.ShopId;
            if (string.IsNullOrWhiteSpace(shopId))
            {
                Debug.LogWarning($"ShopRegistrySO '{name}': shop asset '{shop.name}' has an empty ShopId and will be ignored.", logContext);
                continue;
            }

            if (!seenIds.Add(shopId))
            {
                Debug.LogWarning($"ShopRegistrySO '{name}': duplicate ShopId '{shopId}' found on asset '{shop.name}'. The first valid entry will be used.", logContext);
            }
        }
    }
}
