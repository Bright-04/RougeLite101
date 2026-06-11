using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField]
    private ShopInventorySO[] shops;

    private void Awake()
    {
        foreach (var shop in shops)
        {
            ShopSaveData data = SaveSystem.LoadShop(shop.ShopId);

            if (data != null)
            {
                shop.LoadFromData(data);
            }
            else
            {
                shop.Initialize();
            }
        }
    }

    private void Update()
    {
        foreach (var shop in shops)
        {
            shop.CheckRestock();
        }
    }

    private void OnApplicationQuit()
    {
        SaveAllShops();
    }

    public void SaveAllShops()
    {
        foreach (var shop in shops)
        {
            SaveSystem.SaveShop(shop);
        }
    }
}
