using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    private const string SAVE_FOLDER = "GameSaves";
    private const string PLAYER_FOLDER = "PlayerData";

    private const string PLAYER_STATS_FILE = "playerStats.sav";
    private const string PLAYER_MONEY_FILE = "playerMoney.sav";
    private const string PLAYER_POSITION_FILE = "playerPositionInGameHome.sav";
    private const string SAFE_INVENTORY_FILE = "playerSafeInventory.sav";
    private const string DUNGEON_INVENTORY_FILE = "playerDungeonInventory.sav";
    private const string PLAYER_EQUIPMENT_FILE = "playerEquipment.sav";
    private const string SHOP_FILE_PREFIX = "shop_";

    // Helper method to get the full path with folder structure
    private static string GetPath(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FOLDER, PLAYER_FOLDER);

        // Ensure directories exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return Path.Combine(path, fileName);
    }

    private static void Save<T>(T data, string fileName)
    {
        string path = GetPath(fileName);

        BinaryFormatter formatter = new BinaryFormatter();
        using FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, data);
        stream.Close();
        Debug.Log("saved to: " + path);
    }

    private static T Load<T>(string fileName) where T : class
    {
        string path = GetPath(fileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Save file not found: {path}");
            return null;
        }
        BinaryFormatter formatter = new BinaryFormatter();
        using FileStream stream = new FileStream(path, FileMode.Open);
        Debug.Log("loaded from: " + path);
        return formatter.Deserialize(stream) as T;
    }

    //========================== PlayerStats ============================================== 
    public static void SavePlayerStats(PlayerStats playerStats)
    {
        Save(new PlayerStatsData(playerStats), PLAYER_STATS_FILE);
    }

    public static PlayerStatsData LoadPlayerStats()
    {
        return Load<PlayerStatsData>(PLAYER_STATS_FILE);
    }


    //========================== Player Position in GameHome ===============================
    public static void SavePlayerPositionInGameHome(GameObject player)
    {
        Save(new GameHomePositionData(player), PLAYER_POSITION_FILE);
    }

    public static GameHomePositionData LoadPlayerPositionInGameHome()
    {
        return Load<GameHomePositionData>(PLAYER_POSITION_FILE);
    }

    //========================== PlayerMoney ===============================
    public static void SavePlayerMoney(PlayerMoney gold)
    {
        Save(new PlayerMoneySaveData(gold), PLAYER_MONEY_FILE);
    }

    public static PlayerMoneySaveData LoadPlayerMoney()
    {
        return Load<PlayerMoneySaveData>(PLAYER_MONEY_FILE);
    }

    //========================== Safe Inventory ===============================  
    public static void SavePlayerSafeInventory(InventorySO inventory)
    {
        Save(new SafeInventorySaveData(inventory), SAFE_INVENTORY_FILE);
    }

    public static SafeInventorySaveData LoadPlayerSafeInventory()
    {
        return Load<SafeInventorySaveData>(SAFE_INVENTORY_FILE);
    }

    //========================== Dungeon Inventory ===============================  
    public static void SavePlayerDungeonInventory(InventorySO inventory)
    {
        Save(new DungeonInventorySaveData(inventory), DUNGEON_INVENTORY_FILE);
    }

    public static DungeonInventorySaveData LoadPlayerDungeonInventory()
    {
        return Load<DungeonInventorySaveData>(DUNGEON_INVENTORY_FILE);
    }

    //========================== Equipment (weapon and armor) ===============================  
    public static void SavePlayerEquipment(EquipmentManager equipment, ArmorController armorController)
    {
        Save(new EquipmentSaveData(equipment, armorController), PLAYER_EQUIPMENT_FILE);
    }

    public static EquipmentSaveData LoadPlayerEquipment()
    {
        return Load<EquipmentSaveData>(PLAYER_EQUIPMENT_FILE);
    }

    //========================== Save Shops ===============================  
    public static void SaveShop(ShopInventorySO shop)
    {
        Save(shop.GetSaveData(), $"{SHOP_FILE_PREFIX}{shop.ShopId}.sav");
    }

    public static ShopSaveData LoadShop(string shopId)
    {
        return Load<ShopSaveData>($"{SHOP_FILE_PREFIX}{shopId}.sav");
    }
}

