using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    // Root save folder
    private static string saveFolderName = "GameSaves";

    // Sub-folders
    private static string playerDataFolder = "PlayerData";

    //========================== PlayerStats ==============================================
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

    public static void SavePlayerStats(PlayerStats playerStats)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = GetPlayerStatsPath();
        FileStream stream = new FileStream(path, FileMode.Create);

        PlayerStatsData playerStatsData = new PlayerStatsData(playerStats);

        formatter.Serialize(stream, playerStatsData);
        stream.Close();

        Debug.Log("Player stats saved to: " + path);
    }

    public static PlayerStatsData LoadPlayerStats()
    {
        string path = GetPlayerStatsPath();

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            PlayerStatsData data = formatter.Deserialize(stream) as PlayerStatsData;
            stream.Close();

            Debug.Log("Player stats loaded from: " + path);
            return data;
        }
        else
        {
            Debug.LogWarning("Save file not found at: " + path);
            return null;
        }
    }


    //========================== Player Position in GameHome ===============================
    // Helper method to get the full path with folder structure
    private static string GetPositionInGameHomePath()
    {
        string rootPath = Path.Combine(Application.persistentDataPath, saveFolderName);
        string playerPath = Path.Combine(rootPath, playerDataFolder);

        // Ensure directories exist
        if (!Directory.Exists(playerPath))
        {
            Directory.CreateDirectory(playerPath);
        }

        return Path.Combine(playerPath, "playerPositionInGameHome.sav");
    }

    public static void SavePlayerPositionInGameHome(GameObject player)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = GetPositionInGameHomePath();
        FileStream stream = new FileStream(path, FileMode.Create);

        GameHomePositionData playerPositionInGameHomeData = new GameHomePositionData(player);

        formatter.Serialize(stream, playerPositionInGameHomeData);
        stream.Close();        
    }

    public static GameHomePositionData LoadPlayerPositionInGameHome()
    {
        string path = GetPositionInGameHomePath();

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            GameHomePositionData data = formatter.Deserialize(stream) as GameHomePositionData;
            stream.Close();          
            return data;
        }
        else
        {
            Debug.LogWarning("Save file not found at: " + path);
            return null;
        }
    }

    //========================== PlayerMoney ===============================
    // Helper method to get the full path with folder structure
    private static string GetPlayerMoneyPath()
    {
        string rootPath = Path.Combine(Application.persistentDataPath, saveFolderName);
        string playerPath = Path.Combine(rootPath, playerDataFolder);

        // Ensure directories exist
        if (!Directory.Exists(playerPath))
        {
            Directory.CreateDirectory(playerPath);
        }

        return Path.Combine(playerPath, "playerMoney.sav");
    }

    public static void SavePlayerMoney(PlayerMoney gold)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = GetPlayerMoneyPath();
        FileStream stream = new FileStream(path, FileMode.Create);

        PlayerMoneySaveData playerMoney = new PlayerMoneySaveData(gold);

        formatter.Serialize(stream, playerMoney);
        stream.Close();
    }

    public static PlayerMoneySaveData LoadPlayerMoney()
    {
        string path = GetPlayerMoneyPath();

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            PlayerMoneySaveData data = formatter.Deserialize(stream) as PlayerMoneySaveData;
            stream.Close();
            return data;
        }
        else
        {
            Debug.LogWarning("Save file not found at: " + path);
            return null;
        }
    }

    //========================== Safe Inventory ===============================
    private static string GetPlayerSafeInventoryPath()
    {
        string rootPath = Path.Combine(Application.persistentDataPath, saveFolderName);
        string playerPath = Path.Combine(rootPath, playerDataFolder);

        // Ensure directories exist
        if (!Directory.Exists(playerPath))
        {
            Directory.CreateDirectory(playerPath);
        }

        return Path.Combine(playerPath, "playerSafeInventory.sav");
    }

    public static void SavePlayerSafeInventory(InventorySO inventory)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = GetPlayerSafeInventoryPath();
        FileStream stream = new FileStream(path, FileMode.Create);

        SafeInventorySaveData playerSafeInventory = new SafeInventorySaveData(inventory);

        formatter.Serialize(stream, playerSafeInventory);
        stream.Close();
    }

    public static SafeInventorySaveData LoadPlayerSafeInventory()
    {
        string path = GetPlayerSafeInventoryPath();

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            SafeInventorySaveData data = formatter.Deserialize(stream) as SafeInventorySaveData;
            stream.Close();
            return data;
        }
        else
        {
            Debug.LogWarning("Save file not found at: " + path);
            return null;
        }
    }
}

