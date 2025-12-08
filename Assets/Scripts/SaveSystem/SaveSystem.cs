using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    // Root save folder
    private static string saveFolderName = "GameSaves";

    // Sub-folders
    private static string playerDataFolder = "PlayerData";

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
}
