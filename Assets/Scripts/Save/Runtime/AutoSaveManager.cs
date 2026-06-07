using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSaveManager : MonoBehaviour
{
    [Header("Auto Save Settings")]
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private bool enableAutoSave = true;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private EquipmentManager equipmentManager;

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
        //save stats
        if (playerStats != null)
        {
            SaveSystem.SavePlayerStats(playerStats);
            Debug.Log($"Game saved at {System.DateTime.Now:HH:mm:ss}");
        }
        else
        {
            Debug.LogError("PlayerStats không tìm thấy!");
        }

        //save player
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == "GameHome")
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                SaveSystem.SavePlayerPositionInGameHome(player);
            }              
        }
    }

    public void LoadGame()
    {
        //player stats
        PlayerStatsData playerStatsData = SaveSystem.LoadPlayerStats();

        if (playerStatsData != null)
        {
            if (playerStats != null)
            {
                playerStats.LoadFromData(playerStatsData);
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

        //GameHome position
        GameHomePositionData positionData = SaveSystem.LoadPlayerPositionInGameHome();
        if (positionData != null)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player && activeScene.name == "GameHome")
            {
                Vector3 position;
                position.x = positionData.position[0];
                position.y = positionData.position[1];
                position.z = positionData.position[2];
                player.transform.position = position;
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
