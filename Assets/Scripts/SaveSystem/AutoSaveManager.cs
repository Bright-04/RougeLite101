using UnityEngine;

public class AutoSaveManager : MonoBehaviour
{
    [Header("Auto Save Settings")]
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private bool enableAutoSave = true;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

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
        Debug.Log("Game đang tắt... Đang save...");
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("Game bị pause... Đang save...");
            SaveGame();
        }
    }

    public void SaveGame()
    {
        if (playerStats != null)
        {
            SaveSystem.SavePlayerStats(playerStats);
            Debug.Log($"Game saved at {System.DateTime.Now:HH:mm:ss}");
        }
        else
        {
            Debug.LogError("PlayerStats không tìm thấy!");
        }
    }

    public void LoadGame()
    {
        PlayerStatsData data = SaveSystem.LoadPlayerStats();

        if (data != null)
        {
            if (playerStats != null)
            {
                playerStats.LoadFromData(data);
                Debug.Log("Game loaded successfully!");
            }
            else
            {
                Debug.LogError("PlayerStats không tìm thấy!");
            }
        }
        else
        {
            Debug.Log("Không có save file. Starting new game...");
        }
    }

    public void ManualSave()
    {
        SaveGame();
        autoSaveTimer = 0f;
    }
}
