using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSaveManager : MonoBehaviour
{
    [Header("Auto Save Settings")]
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private bool enableAutoSave = true;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMoney playerMoney;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private ArmorController armorController;

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

        if (playerMoney == null)
        {
            playerMoney = FindAnyObjectByType<PlayerMoney>();
            if (playerMoney == null)
            {
                Debug.LogError("AutoSaveManager: PlayerMoney not found! Please assign it in the inspector.");
            }
        }

        if (inventoryController == null)
        {
            inventoryController = FindAnyObjectByType<InventoryController>();
            if (inventoryController == null)
            {
                Debug.LogWarning("AutoSaveManager: InventoryController not found. Weapon loadout will not be saved.");
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

        if (armorController == null)
        {
            armorController = FindAnyObjectByType<ArmorController>();
            if (armorController == null)
            {
                Debug.LogWarning("AutoSaveManager: ArmorController not found. Weapon loadout will not be saved.");
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
            
        }
        else
        {
            Debug.LogError("PlayerStats không tìm thấy!");
        }

        //save money
        if (playerMoney != null)
        {
            SaveSystem.SavePlayerMoney(playerMoney);

        }
        else
        {
            Debug.LogError("PlayerMoney không tìm thấy!");
        }

        //save inventory
        if (inventoryController != null)
        {
            SaveSystem.SavePlayerSafeInventory(inventoryController.SafeInventory);
            SaveSystem.SavePlayerDungeonInventory(inventoryController.DungeonInventory);
        }
        else
        {
            Debug.LogError("InventoryController không tìm thấy!");
        }

        //save equipment
        SaveSystem.SavePlayerEquipment(equipmentManager, armorController);

        //save player position
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == "GameHome")
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                SaveSystem.SavePlayerPositionInGameHome(player);
            }              
        }

        Debug.Log($"Game saved at {System.DateTime.Now:HH:mm:ss}");
    }

    public void LoadGame()
    {
        PlayerStatsData playerStatsData = SaveSystem.LoadPlayerStats();
        PlayerMoneySaveData playerMoneySaveData = SaveSystem.LoadPlayerMoney();
        SafeInventorySaveData safeInventorySaveData = SaveSystem.LoadPlayerSafeInventory();
        DungeonInventorySaveData dungeonInventorySaveData = SaveSystem.LoadPlayerDungeonInventory();
        EquipmentSaveData equipmentSaveData = SaveSystem.LoadPlayerEquipment();

        if (playerStatsData != null)
        {
            //player stats
            if (playerStats != null)
            {
                playerStats.LoadFromData(playerStatsData);
                Debug.Log("player stats loaded successfully!");

                //EQUIPMENT
                if (equipmentSaveData != null)
                {
                    armorController.LoadArmour(equipmentSaveData);
                    equipmentManager.LoadWeapons(equipmentSaveData);
                    playerStats.RefreshAfterEquipmentLoad();
                }
            }
            else
            {
                Debug.LogError("PlayerStats không tìm thấy!");
            }           

        }

        if (playerMoneySaveData != null)
        {
            //player money
            if (playerMoney != null)
            {
                playerMoney.LoadFromData(playerMoneySaveData);
                Debug.Log("PlayerMoney loaded successfully!");
            }
            else
            {
                Debug.LogError("PlayerMoney không tìm thấy!");
            }
        }

        if(safeInventorySaveData != null)
        {
            //player inventory
            if (inventoryController != null)
            {
                inventoryController.LoadSafeInventory(safeInventorySaveData);
                Debug.Log("player safe inventory loaded successfully!");               
            }
            else
            {
                Debug.LogError("InventoryController không tìm thấy!");
            }
        }

        if (dungeonInventorySaveData != null)
        {
            //player inventory
            if (inventoryController != null)
            {             
                inventoryController.LoadDungeonInventory(dungeonInventorySaveData);
                Debug.Log("player dungeon inventory loaded successfully!");
            }
            else
            {
                Debug.LogError("InventoryController không tìm thấy!");
            }
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
