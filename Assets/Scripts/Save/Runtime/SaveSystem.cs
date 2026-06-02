using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Collections.Generic;

public static class SaveSystem
{
    // Root save folder
    private static string saveFolderName = "GameSaves";

    // Sub-folders
    private static string playerDataFolder = "PlayerData";
    private static string shopFolder = "ShopData";


    //============================= PlayerData ===============================================
    // Helper method to get the full path with folder structure
    private static string GetPlayerStatsPath()
    {
        string rootPath = Path.Combine(Application.persistentDataPath, saveFolderName);
        string playerPath = Path.Combine(rootPath, playerDataFolder);

        // Ensure directories exist
        if (!Directory.Exists(playerPath))
        {
            Directory.CreateDirectory(playerPath);
        }

        return Path.Combine(playerPath, "playerStats.sav");
    }

    public static void SavePlayerStats(PlayerStats playerStats, EquipmentManager equipmentManager, bool logInfo = false)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = GetPlayerStatsPath();
        FileStream stream = new FileStream(path, FileMode.Create);

        PlayerStatsData playerStatsData = new PlayerStatsData(playerStats, equipmentManager);

        formatter.Serialize(stream, playerStatsData);
        stream.Close();

        if (logInfo)
        {
            Debug.Log("Player stats saved to: " + path);
        }
    }

    public static PlayerStatsData LoadPlayerStats(bool logInfo = false)
    {
        string path = GetPlayerStatsPath();

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            PlayerStatsData data = formatter.Deserialize(stream) as PlayerStatsData;
            stream.Close();

            if (logInfo)
            {
                Debug.Log("Player stats loaded from: " + path);
            }
            return data;
        }
        else
        {
            Debug.LogWarning("Save file not found at: " + path);
            return null;
        }
    }

    //============================= ShopData ================================================
    private static string GetShopPath()
    {
        string root =Path.Combine(Application.persistentDataPath, saveFolderName);
        string folder = Path.Combine(root, shopFolder);

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return Path.Combine(folder, "shopRestock.sav");
    }

    public static void SaveShopRestock(
    List<ShopInventorySO> shops)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = GetShopPath();

        FileStream stream = new FileStream(path, FileMode.Create);

        ShopRestockData data = new ShopRestockData();

        foreach (var shop in shops)
        {
            data.shops.Add(
                new ShopSaveEntry
                {
                    shopId = shop.ShopId,
                    nextRestockTicks = shop.NextRestockTime.Ticks
                });
        }
        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static ShopRestockData LoadShopRestock()
    {
        string path = GetShopPath();

        if (!File.Exists(path)) return null;

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Open);
        ShopRestockData data = formatter.Deserialize(stream) as ShopRestockData;
        stream.Close();
        return data;
    }
}
