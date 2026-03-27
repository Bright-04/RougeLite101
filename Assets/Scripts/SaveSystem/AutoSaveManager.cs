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

        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {

            SaveGame();
        }
    }

    public void SaveGame()
    {
        if (playerStats != null)
        {
            SaveSystem.SavePlayerStats(playerStats);

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
