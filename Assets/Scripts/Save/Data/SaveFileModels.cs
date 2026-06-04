using System;

[Serializable]
public class ProfileSaveFile
{
    public int version = SaveSystem.CurrentSaveVersion;
    public PlayerStatsSaveData playerStats;
    public EquipmentSaveData equipment;
    public PlayerMoneySaveData playerMoney;
    public SafeInventorySaveData safeInventory;
    public HubSaveData hub;
}

[Serializable]
public class RunSnapshotFile
{
    public int version = SaveSystem.CurrentSaveVersion;
    public RunSnapshotSaveData run;
    public DungeonInventorySaveData dungeonInventory;
    public PlayerStatsSaveData playerStats;
    public EquipmentSaveData equipment;
    public PlayerMoneySaveData playerMoney;
}

[Serializable]
public class ShopRestockSaveFile
{
    public int version = SaveSystem.CurrentSaveVersion;
    public ShopRestockData shopRestock;
}
