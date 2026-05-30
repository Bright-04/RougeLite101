using UnityEngine;

public class AutoSaveManager : MonoBehaviour
{
    [Header("Auto Save Settings")]
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private bool logSaveLoadInfo = false;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private EquipmentManager equipmentManager;
    //[SerializeField] private EquipmentController equipmentController;

    private float autoSaveTimer = 0f;

    private void Start()
    {
        // Tìm PlayerStats nếu chưa được gán
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("AutoSaveManager: PlayerStats not found! Please assign it in the inspector.");
            }
        }

        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
            if (equipmentManager == null)
            {
                Debug.LogWarning("AutoSaveManager: EquipmentManager not found. Weapon loadout will not be saved.");
            }
        }

        //if (equipmentController == null)
        //{
        //    equipmentController = FindAnyObjectByType<EquipmentController>();
        //}

        LoadGame();
    }

    private void Update()
    {
        if (enableAutoSave)
        {
            autoSaveTimer += Time.deltaTime;

            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveGame();
                autoSaveTimer = 0f;
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (logSaveLoadInfo)
        {
            Debug.Log("Game đang tắt... Đang save...");
        }
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (logSaveLoadInfo)
            {
                Debug.Log("Game bị pause... Đang save...");
            }
            SaveGame();
        }
    }

    public void SaveGame()
    {
        if (playerStats != null)
        {
            SaveSystem.SavePlayerStats(playerStats, equipmentManager, logSaveLoadInfo);
            if (logSaveLoadInfo)
            {
                Debug.Log($"Game saved at {System.DateTime.Now:HH:mm:ss}");
            }
        }
        else
        {
            Debug.LogError("PlayerStats không tìm thấy!");
        }
    }

    public void LoadGame()
    {
        PlayerStatsData data = SaveSystem.LoadPlayerStats(logSaveLoadInfo);

        if (data != null)
        {
            if (playerStats != null)
            {
                playerStats.LoadFromData(data);
                if (logSaveLoadInfo)
                {
                    Debug.Log("Game loaded successfully!");
                }
            }
            else
            {
                Debug.LogError("PlayerStats không tìm thấy!");
            }
        }
        else
        {
            if (logSaveLoadInfo)
            {
                Debug.Log("Không có save file. Starting new game...");
            }
        }
    }

    public void ManualSave()
    {
        SaveGame();
        autoSaveTimer = 0f;
    }

    public static void TrySaveActiveSceneState()
    {
        AutoSaveManager saveManager = FindAnyObjectByType<AutoSaveManager>();
        if (saveManager != null)
        {
            saveManager.ManualSave();
        }
    }
}
