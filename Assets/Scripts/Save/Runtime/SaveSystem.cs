using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public const int CurrentSaveVersion = 1;

    private const string SaveFolderName = "GameSaves";
    private const string SaveVersionFolderName = "v1";
    private const string ProfileFileName = "profile.json";
    private const string RunSnapshotFileName = "runSnapshot.json";
    private const string ShopRestockFileName = "shopRestock.json";

    private static string RootDirectoryPath => Path.Combine(Application.persistentDataPath, SaveFolderName, SaveVersionFolderName);
    private static string ProfilePath => Path.Combine(RootDirectoryPath, ProfileFileName);
    private static string RunSnapshotPath => Path.Combine(RootDirectoryPath, RunSnapshotFileName);
    private static string ShopRestockPath => Path.Combine(RootDirectoryPath, ShopRestockFileName);

    public static bool HasProfileSave()
    {
        return File.Exists(ProfilePath);
    }

    public static ProfileSaveFile LoadProfile()
    {
        return ReadJson<ProfileSaveFile>(ProfilePath);
    }

    public static void SaveProfile(ProfileSaveFile profile)
    {
        WriteJson(ProfilePath, profile, "profile");
    }

    public static void DeleteProfile()
    {
        DeleteSaveFile(ProfilePath, "profile");
    }

    public static bool HasRunSnapshot()
    {
        return File.Exists(RunSnapshotPath);
    }

    public static RunSnapshotFile LoadRunSnapshot()
    {
        return ReadJson<RunSnapshotFile>(RunSnapshotPath);
    }

    public static void SaveRunSnapshot(RunSnapshotFile snapshot)
    {
        WriteJson(RunSnapshotPath, snapshot, "run snapshot");
    }

    public static void DeleteRunSnapshot()
    {
        DeleteSaveFile(RunSnapshotPath, "run snapshot");
    }

    public static ShopRestockSaveFile LoadShopRestock()
    {
        return ReadJson<ShopRestockSaveFile>(ShopRestockPath);
    }

    public static void SaveShopRestock(ShopRestockSaveFile shop)
    {
        WriteJson(ShopRestockPath, shop, "shop restock");
    }

    public static ProfileSaveFile CreateDefaultProfile()
    {
        return new ProfileSaveFile
        {
            version = CurrentSaveVersion,
            playerStats = new PlayerStatsSaveData(),
            equipment = new EquipmentSaveData(),
            playerMoney = new PlayerMoneySaveData(),
            safeInventory = new SafeInventorySaveData(),
            hub = new HubSaveData()
        };
    }

    private static void EnsureRootDirectoryExists()
    {
        if (!Directory.Exists(RootDirectoryPath))
        {
            Directory.CreateDirectory(RootDirectoryPath);
        }
    }

    private static void CleanupTempFile(string targetPath)
    {
        string tempPath = targetPath + ".tmp";
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveSystem: Failed to clean temp file '{tempPath}'. {ex.Message}");
        }
    }

    private static void DeleteSaveFile(string path, string label)
    {
        CleanupTempFile(path);

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveSystem: Failed to delete {label} file '{path}'. {ex.Message}");
        }
    }

    private static T ReadJson<T>(string path) where T : class
    {
        CleanupTempFile(path);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"SaveSystem: Save file '{path}' is empty.");
                return null;
            }

            T data = JsonUtility.FromJson<T>(json);
            if (data == null)
            {
                Debug.LogWarning($"SaveSystem: Save file '{path}' is invalid or corrupt.");
            }

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveSystem: Failed to read save file '{path}'. {ex.Message}");
            return null;
        }
    }

    private static void WriteJson<T>(string path, T data, string label)
    {
        if (data == null)
        {
            Debug.LogWarning($"SaveSystem: Ignoring null {label} save payload.");
            return;
        }

        EnsureRootDirectoryExists();
        CleanupTempFile(path);

        string tempPath = path + ".tmp";

        try
        {
            string json = JsonUtility.ToJson(data, true);

            using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tempPath, path, null);
                }
                catch (Exception)
                {
                    File.Delete(path);
                    File.Move(tempPath, path);
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveSystem: Failed to write {label} file '{path}'. {ex.Message}");
            CleanupTempFile(path);
        }
    }
}
