using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RougeLite.Player;

namespace RougeLite.Managers
{
    /// <summary>
    /// Save Manager handles save/load functionality, player progress, and game settings persistence
    /// Supports multiple save slots and automatic backup system
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        #region Singleton
        
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SaveManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveManager");
                        _instance = go.AddComponent<SaveManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Save Settings

        [Header("Save Settings")]
        [SerializeField] private int maxSaveSlots = 3;
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 60f; // seconds
        [SerializeField] private bool enableBackups = true;
        [SerializeField] private int maxBackups = 5;

        private string saveDirectory;
        private string settingsFileName = "GameSettings.json";
        private float lastAutoSaveTime;

        #endregion

        #region Events

        public System.Action<GameSaveData> OnGameSaved;
        public System.Action<GameSaveData> OnGameLoaded;
        public System.Action<string> OnSaveError;
        public System.Action<string> OnLoadError;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Handle auto-save
            if (enableAutoSave && Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSave();
            }
        }

        #endregion

        #region Initialization

        private void InitializeSaveManager()
        {
            Debug.Log("SaveManager: Initializing...");

            // Set save directory
            saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            
            // Ensure save directory exists
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            // Load game settings
            LoadGameSettings();

            Debug.Log($"SaveManager: Save directory: {saveDirectory}");
            Debug.Log("SaveManager: Initialization complete");
        }

        #endregion

        #region Game Save/Load

        public bool SaveGame(int slotIndex = 0, string saveName = "")
        {
            try
            {
                if (slotIndex < 0 || slotIndex >= maxSaveSlots)
                {
                    Debug.LogError($"SaveManager: Invalid save slot {slotIndex}");
                    OnSaveError?.Invoke($"Invalid save slot {slotIndex}");
                    return false;
                }

                // Create save data
                GameSaveData saveData = CreateSaveData(saveName);
                
                // Get save file path
                string fileName = $"Save_Slot_{slotIndex}.json";
                string filePath = Path.Combine(saveDirectory, fileName);

                // Create backup if enabled
                if (enableBackups && File.Exists(filePath))
                {
                    CreateBackup(filePath, slotIndex);
                }

                // Save to file
                string jsonData = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(filePath, jsonData);

                Debug.Log($"SaveManager: Game saved to slot {slotIndex}");
                OnGameSaved?.Invoke(saveData);
                
                lastAutoSaveTime = Time.time;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Error saving game: {e.Message}");
                OnSaveError?.Invoke(e.Message);
                return false;
            }
        }

        public GameSaveData LoadGame(int slotIndex = 0)
        {
            try
            {
                if (slotIndex < 0 || slotIndex >= maxSaveSlots)
                {
                    Debug.LogError($"SaveManager: Invalid save slot {slotIndex}");
                    OnLoadError?.Invoke($"Invalid save slot {slotIndex}");
                    return null;
                }

                string fileName = $"Save_Slot_{slotIndex}.json";
                string filePath = Path.Combine(saveDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"SaveManager: No save file found in slot {slotIndex}");
                    OnLoadError?.Invoke($"No save file found in slot {slotIndex}");
                    return null;
                }

                string jsonData = File.ReadAllText(filePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);

                Debug.Log($"SaveManager: Game loaded from slot {slotIndex}");
                OnGameLoaded?.Invoke(saveData);

                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Error loading game: {e.Message}");
                OnLoadError?.Invoke(e.Message);
                return null;
            }
        }

        public bool DeleteSave(int slotIndex)
        {
            try
            {
                if (slotIndex < 0 || slotIndex >= maxSaveSlots)
                {
                    Debug.LogError($"SaveManager: Invalid save slot {slotIndex}");
                    return false;
                }

                string fileName = $"Save_Slot_{slotIndex}.json";
                string filePath = Path.Combine(saveDirectory, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"SaveManager: Save slot {slotIndex} deleted");
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Error deleting save: {e.Message}");
                return false;
            }
        }

        public bool SaveExists(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSaveSlots) return false;

            string fileName = $"Save_Slot_{slotIndex}.json";
            string filePath = Path.Combine(saveDirectory, fileName);
            
            return File.Exists(filePath);
        }

        public SaveSlotInfo GetSaveSlotInfo(int slotIndex)
        {
            if (!SaveExists(slotIndex)) return null;

            try
            {
                string fileName = $"Save_Slot_{slotIndex}.json";
                string filePath = Path.Combine(saveDirectory, fileName);
                
                FileInfo fileInfo = new FileInfo(filePath);
                string jsonData = File.ReadAllText(filePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);

                return new SaveSlotInfo
                {
                    slotIndex = slotIndex,
                    saveName = saveData.saveName,
                    saveDate = fileInfo.LastWriteTime,
                    playtime = saveData.playtime,
                    level = saveData.playerLevel,
                    enemiesKilled = saveData.enemiesKilled
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Error reading save slot info: {e.Message}");
                return null;
            }
        }

        public List<SaveSlotInfo> GetAllSaveSlots()
        {
            List<SaveSlotInfo> saveSlots = new List<SaveSlotInfo>();

            for (int i = 0; i < maxSaveSlots; i++)
            {
                SaveSlotInfo slotInfo = GetSaveSlotInfo(i);
                if (slotInfo != null)
                {
                    saveSlots.Add(slotInfo);
                }
            }

            return saveSlots;
        }

        #endregion

        #region Auto Save

        private void AutoSave()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameplayActive)
            {
                SaveGame(0, "AutoSave");
                Debug.Log("SaveManager: Auto-save completed");
            }
        }

        public void EnableAutoSave(bool enable)
        {
            enableAutoSave = enable;
        }

        public void SetAutoSaveInterval(float interval)
        {
            autoSaveInterval = Mathf.Max(30f, interval); // Minimum 30 seconds
        }

        #endregion

        #region Backup System

        private void CreateBackup(string originalFilePath, int slotIndex)
        {
            try
            {
                string backupDir = Path.Combine(saveDirectory, "Backups", $"Slot_{slotIndex}");
                
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // Clean old backups if over limit
                CleanOldBackups(backupDir);

                // Create new backup
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"Save_Slot_{slotIndex}_Backup_{timestamp}.json";
                string backupFilePath = Path.Combine(backupDir, backupFileName);

                File.Copy(originalFilePath, backupFilePath);
                
                Debug.Log($"SaveManager: Backup created: {backupFileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Error creating backup: {e.Message}");
            }
        }

        private void CleanOldBackups(string backupDir)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(backupDir);
                FileInfo[] backupFiles = dir.GetFiles("*.json");

                if (backupFiles.Length >= maxBackups)
                {
                    // Sort by creation time (oldest first)
                    Array.Sort(backupFiles, (x, y) => x.CreationTime.CompareTo(y.CreationTime));

                    // Delete oldest files to keep under limit
                    int filesToDelete = backupFiles.Length - maxBackups + 1;
                    for (int i = 0; i < filesToDelete; i++)
                    {
                        backupFiles[i].Delete();
                        Debug.Log($"SaveManager: Deleted old backup: {backupFiles[i].Name}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Error cleaning old backups: {e.Message}");
            }
        }

        #endregion

        #region Settings Save/Load

        public void SaveGameSettings()
        {
            try
            {
                GameSettings settings = CreateGameSettings();
                string filePath = Path.Combine(saveDirectory, settingsFileName);
                string jsonData = JsonUtility.ToJson(settings, true);
                File.WriteAllText(filePath, jsonData);

                Debug.Log("SaveManager: Game settings saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Error saving settings: {e.Message}");
            }
        }

        public void LoadGameSettings()
        {
            try
            {
                string filePath = Path.Combine(saveDirectory, settingsFileName);
                
                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    GameSettings settings = JsonUtility.FromJson<GameSettings>(jsonData);
                    ApplyGameSettings(settings);

                    Debug.Log("SaveManager: Game settings loaded");
                }
                else
                {
                    Debug.Log("SaveManager: No settings file found, using defaults");
                    SaveGameSettings(); // Create default settings file
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Error loading settings: {e.Message}");
            }
        }

        #endregion

        #region Data Creation

        private GameSaveData CreateSaveData(string saveName = "")
        {
            GameSaveData saveData = new GameSaveData();
            
            // Basic save info
            saveData.saveName = string.IsNullOrEmpty(saveName) ? $"Save_{DateTime.Now:yyyy-MM-dd_HH-mm}" : saveName;
            saveData.saveDate = DateTime.Now.ToString();
            saveData.gameVersion = Application.version;

            // Game state
            if (GameManager.Instance != null)
            {
                saveData.playtime = GameManager.Instance.GameTime;
                saveData.enemiesKilled = GameManager.Instance.EnemiesKilled;
                saveData.damageDealt = GameManager.Instance.DamageDealt;
            }

            // Player data
            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                saveData.playerLevel = 1; // You can add level system later
                saveData.playerHealth = playerStats.currentHP;
                saveData.maxPlayerHealth = playerStats.maxHP;
                saveData.playerMana = playerStats.currentMana;
                saveData.maxPlayerMana = playerStats.maxMana;
                saveData.playerStamina = playerStats.currentStamina;
                saveData.maxPlayerStamina = playerStats.maxStamina;
                saveData.attackDamage = playerStats.attackDamage;
                saveData.abilityPower = playerStats.abilityPower;
                saveData.defense = playerStats.defense;
                saveData.critChance = playerStats.critChance;
                saveData.critDamage = playerStats.critDamage;
                saveData.luck = playerStats.luck;
            }

            // Scene data
            saveData.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            // Player position
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                saveData.playerPosition = player.transform.position;
            }

            return saveData;
        }

        private GameSettings CreateGameSettings()
        {
            GameSettings settings = new GameSettings();

            // Audio settings
            if (AudioManager.Instance != null)
            {
                settings.masterVolume = AudioManager.Instance.MasterVolume;
                settings.musicVolume = AudioManager.Instance.MusicVolume;
                settings.sfxVolume = AudioManager.Instance.SfxVolume;
            }

            // Graphics settings
            settings.screenResolution = Screen.currentResolution;
            settings.fullscreen = Screen.fullScreen;
            settings.vsync = QualitySettings.vSyncCount > 0;
            settings.qualityLevel = QualitySettings.GetQualityLevel();

            // Control settings
            // Add input settings when InputManager is created

            return settings;
        }

        private void ApplyGameSettings(GameSettings settings)
        {
            // Apply audio settings
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(settings.masterVolume);
                AudioManager.Instance.SetMusicVolume(settings.musicVolume);
                AudioManager.Instance.SetSfxVolume(settings.sfxVolume);
            }

            // Apply graphics settings
            Screen.SetResolution(settings.screenResolution.width, settings.screenResolution.height, settings.fullscreen);
            QualitySettings.vSyncCount = settings.vsync ? 1 : 0;
            QualitySettings.SetQualityLevel(settings.qualityLevel);

            // Apply control settings
            // Add input settings when InputManager is created
        }

        #endregion

        #region Debug

#if RL_DEBUG_UI
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
#endif
#if RL_DEBUG_UI

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.Box(new Rect(10, 480, 300, 120), "Save Manager Debug");
            GUI.Label(new Rect(20, 500, 280, 20), $"Save Directory: {Path.GetFileName(saveDirectory)}");
            GUI.Label(new Rect(20, 520, 280, 20), $"Auto Save: {(enableAutoSave ? "ON" : "OFF")} ({autoSaveInterval}s)");
            GUI.Label(new Rect(20, 540, 280, 20), $"Backups: {(enableBackups ? "ON" : "OFF")} (Max: {maxBackups})");

            if (GUI.Button(new Rect(20, 560, 80, 20), "Quick Save"))
            {
                SaveGame(0, "QuickSave");
            }

            if (GUI.Button(new Rect(110, 560, 80, 20), "Quick Load"))
            {
                LoadGame(0);
            }

            if (GUI.Button(new Rect(200, 560, 100, 20), "Save Settings"))
            {
                SaveGameSettings();
            }
        }
#endif

        #endregion
    }

    #region Save Data Structures

    [System.Serializable]
    public class GameSaveData
    {
        [Header("Save Info")]
        public string saveName;
        public string saveDate;
        public string gameVersion;

        [Header("Game Progress")]
        public float playtime;
        public int enemiesKilled;
        public float damageDealt;
        public int playerLevel;

        [Header("Player Stats")]
        public float playerHealth;
        public float maxPlayerHealth;
        public float playerMana;
        public float maxPlayerMana;
        public float playerStamina;
        public float maxPlayerStamina;
        public float attackDamage;
        public float abilityPower;
        public float defense;
        public float critChance;
        public float critDamage;
        public float luck;

        [Header("World Data")]
        public string currentScene;
        public Vector3 playerPosition;
        
        // Add more fields as needed for inventory, unlocks, etc.
    }

    [System.Serializable]
    public class GameSettings
    {
        [Header("Audio")]
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;

        [Header("Graphics")]
        public Resolution screenResolution;
        public bool fullscreen = true;
        public bool vsync = true;
        public int qualityLevel = 2;

        [Header("Controls")]
        // Add input settings when InputManager is created
        public float mouseSensitivity = 1f;
        public bool invertMouseY = false;
    }

    [System.Serializable]
    public class SaveSlotInfo
    {
        public int slotIndex;
        public string saveName;
        public DateTime saveDate;
        public float playtime;
        public int level;
        public int enemiesKilled;
    }

    #endregion
}

