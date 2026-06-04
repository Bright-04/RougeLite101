using UnityEngine;
using UnityEngine.SceneManagement;

public interface ISaveable
{
    void Save();
    void Load();
}

public class AutoSaveManager : MonoBehaviour
{
    private static bool startupProfileLoadConsumed;
    private static bool shopRestockLoadConsumed;

    [Header("Auto Save Settings")]
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private bool logSaveLoadInfo = false;
    [SerializeField] private bool loadProfileOnStart = false;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private PlayerMoney playerMoney;
    [SerializeField] private ShopRegistrySO shopRegistry;

    private float autoSaveTimer;

    private void Start()
    {
        ResolveReferences();

        if (ShouldLoadProfileOnStart())
        {
            startupProfileLoadConsumed = true;
            LoadGame();
        }

        LoadShopRestockState();
    }

    private void Update()
    {
        if (!enableAutoSave)
        {
            return;
        }

        autoSaveTimer += Time.deltaTime;

        if (autoSaveTimer >= autoSaveInterval)
        {
            SaveGame();
            autoSaveTimer = 0f;
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
        if (!pauseStatus)
        {
            return;
        }

        if (logSaveLoadInfo)
        {
            Debug.Log("Game bị pause... Đang save...");
        }

        SaveGame();
    }

    public ProfileSaveFile CaptureProfileFromScene()
    {
        ResolveReferences();

        if (playerStats == null)
        {
            Debug.LogError("AutoSaveManager: Cannot capture profile because PlayerStats is missing.");
            return null;
        }

        ProfileSaveFile profile = SaveSystem.CreateDefaultProfile();
        profile.playerStats = CapturePlayerStats();
        profile.equipment = CaptureEquipment();
        profile.playerMoney = CapturePlayerMoney();
        return profile;
    }

    public bool ApplyProfileToScene(ProfileSaveFile profile)
    {
        if (profile == null)
        {
            return false;
        }

        ResolveReferences();

        if (playerStats == null)
        {
            Debug.LogError("AutoSaveManager: Cannot apply profile because PlayerStats is missing.");
            return false;
        }

        if (profile.playerStats != null)
        {
            playerStats.LoadFromData(profile.playerStats);
        }

        if (profile.equipment != null)
        {
            ApplyEquipment(profile.equipment);
        }

        if (profile.playerMoney != null)
        {
            ApplyPlayerMoney(profile.playerMoney);
        }

        return true;
    }

    public void SaveGame()
    {
        ProfileSaveFile profile = CaptureProfileFromScene();
        if (profile == null)
        {
            return;
        }

        SaveSystem.SaveProfile(profile);

        SaveShopRestockState();

        if (logSaveLoadInfo)
        {
            Debug.Log($"Game saved at {System.DateTime.Now:HH:mm:ss}");
        }
    }

    public void LoadGame()
    {
        ProfileSaveFile profile = SaveSystem.LoadProfile();
        if (profile == null)
        {
            if (logSaveLoadInfo)
            {
                Debug.Log("Không có profile save. Starting current scene state...");
            }
            return;
        }

        if (!ApplyProfileToScene(profile))
        {
            Debug.LogWarning("AutoSaveManager: Profile load skipped because required scene dependencies are missing.");
            return;
        }

        if (logSaveLoadInfo)
        {
            Debug.Log("Game loaded successfully!");
        }

        LoadShopRestockState();
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

    public ShopRestockSaveFile CaptureShopRestockFromRegistry()
    {
        if (shopRegistry == null)
        {
            Debug.LogWarning("AutoSaveManager: ShopRegistrySO is not assigned. Shop restock save will be skipped.");
            return null;
        }

        ShopRestockData shopData = new ShopRestockData();
        var shops = shopRegistry.GetAll();
        for (int i = 0; i < shops.Count; i++)
        {
            ShopInventorySO shop = shops[i];
            if (shop == null)
            {
                continue;
            }

            shopData.shops.Add(shop.CreateSaveEntry());
        }

        return new ShopRestockSaveFile
        {
            version = SaveSystem.CurrentSaveVersion,
            shopRestock = shopData
        };
    }

    public void ApplyShopRestockFromRegistry(ShopRestockSaveFile shopFile)
    {
        if (shopFile == null || shopFile.shopRestock == null)
        {
            return;
        }

        if (shopRegistry == null)
        {
            Debug.LogWarning("AutoSaveManager: ShopRegistrySO is not assigned. Shop restock load will be skipped.");
            return;
        }

        for (int i = 0; i < shopFile.shopRestock.shops.Count; i++)
        {
            ShopSaveEntry entry = shopFile.shopRestock.shops[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.shopId))
            {
                Debug.LogWarning("AutoSaveManager: Encountered shop restock entry with an empty shopId. Skipping.");
                continue;
            }

            ShopInventorySO shop = shopRegistry.GetById(entry.shopId);
            if (shop == null)
            {
                Debug.LogWarning($"AutoSaveManager: Unknown saved shopId '{entry.shopId}'. Skipping shop restock entry.");
                continue;
            }

            shop.ApplySaveEntry(entry);
        }
    }

    public void SaveShopRestockState()
    {
        try
        {
            ShopRestockSaveFile shopFile = CaptureShopRestockFromRegistry();
            if (shopFile == null)
            {
                return;
            }

            SaveSystem.SaveShopRestock(shopFile);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"AutoSaveManager: Shop restock save failed but profile save will continue. {ex.Message}");
        }
    }

    public void LoadShopRestockState(bool force = false)
    {
        if (!force && shopRestockLoadConsumed)
        {
            return;
        }

        try
        {
            ShopRestockSaveFile shopFile = SaveSystem.LoadShopRestock();
            if (shopFile == null)
            {
                if (force)
                {
                    shopRestockLoadConsumed = true;
                }
                else
                {
                    shopRestockLoadConsumed = true;
                }
                return;
            }

            ApplyShopRestockFromRegistry(shopFile);
            shopRestockLoadConsumed = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"AutoSaveManager: Shop restock load failed but profile load will continue. {ex.Message}");
            shopRestockLoadConsumed = true;
        }
    }

    private void ResolveReferences()
    {
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

        if (playerMoney == null)
        {
            playerMoney = FindAnyObjectByType<PlayerMoney>();
            if (playerMoney == null)
            {
                Debug.LogWarning("AutoSaveManager: PlayerMoney not found. Money save/load will be skipped.");
            }
        }
    }

    private bool ShouldLoadProfileOnStart()
    {
        if (!loadProfileOnStart || startupProfileLoadConsumed)
        {
            return false;
        }

        if (SceneManager.GetActiveScene().name != "GameHome")
        {
            return false;
        }

        if (playerStats == null || !SaveSystem.HasProfileSave())
        {
            return false;
        }

        return true;
    }

    private PlayerStatsSaveData CapturePlayerStats()
    {
        return new PlayerStatsSaveData
        {
            level = playerStats.GetLevel(),
            currentExp = playerStats.GetCurrentExp(),
            levelUpExp = playerStats.GetLevelUpExp(),
            maxHP = playerStats.GetNoBuffMaxHP(),
            maxMana = playerStats.GetNoBuffMaxMana(),
            maxStamina = playerStats.GetNoBuffMaxStamina(),
            hpRegen = playerStats.GetNoBuffRegenHP(),
            manaRegen = playerStats.GetNoBuffRegenMana(),
            staminaRegen = playerStats.GetNoBuffRegenStamina(),
            attackDamage = playerStats.GetNoBuffAttackDamage(),
            abilityPower = playerStats.GetNoBuffAbilityPower(),
            defense = playerStats.GetNoBuffDefense(),
            critChance = playerStats.GetNoBuffCritChance(),
            critDamage = playerStats.GetNoBuffCritDamage(),
            luck = playerStats.GetNoBuffLuck()
        };
    }

    private EquipmentSaveData CaptureEquipment()
    {
        EquipmentSaveData equipment = new EquipmentSaveData();

        if (equipmentManager != null)
        {
            equipment.mainWeaponId = equipmentManager.GetMainWeaponId();
            equipment.subWeaponId = equipmentManager.GetSubWeaponId();
            equipment.activeSlot = (int)equipmentManager.GetActiveSlot();
        }

        ArmorController armorController = playerStats != null ? playerStats.GetComponent<ArmorController>() : null;
        if (armorController != null)
        {
            equipment.helmetArmorId = armorController.Helmet != null ? armorController.Helmet.EquipmentId : string.Empty;
            equipment.chestplateArmorId = armorController.Chestplate != null ? armorController.Chestplate.EquipmentId : string.Empty;
            equipment.leggingsArmorId = armorController.Leggings != null ? armorController.Leggings.EquipmentId : string.Empty;
            equipment.bootsArmorId = armorController.Boots != null ? armorController.Boots.EquipmentId : string.Empty;
        }

        return equipment;
    }

    private PlayerMoneySaveData CapturePlayerMoney()
    {
        if (playerMoney == null)
        {
            return null;
        }

        return new PlayerMoneySaveData
        {
            gold = playerMoney.Gold
        };
    }

    private void ApplyEquipment(EquipmentSaveData equipment)
    {
        if (equipmentManager == null)
        {
            Debug.LogWarning("AutoSaveManager: EquipmentManager not found while loading equipment.");
            return;
        }

        EquipmentManager.WeaponSlot loadedSlot = equipment.activeSlot == (int)EquipmentManager.WeaponSlot.Sub
            ? EquipmentManager.WeaponSlot.Sub
            : EquipmentManager.WeaponSlot.Main;

        equipmentManager.LoadWeapons(equipment.mainWeaponId, equipment.subWeaponId, loadedSlot);
    }

    private void ApplyPlayerMoney(PlayerMoneySaveData money)
    {
        if (playerMoney == null)
        {
            Debug.LogWarning("AutoSaveManager: PlayerMoney not found while loading money.");
            return;
        }

        playerMoney.SetGold(money.gold);
    }
}
