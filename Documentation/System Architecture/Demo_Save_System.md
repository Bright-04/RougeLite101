# Tài liệu hệ thống Save - RougeLite101

## Mục lục

1. Mục tiêu hệ thống
2. Hiện trạng trước khi refactor
3. Kiến trúc save mới
4. Các file save chính
5. Luồng hoạt động
6. Các domain đã được hỗ trợ
7. Implementation status
8. Những điểm cần chú ý
9. Công việc tiếp theo
10. Checklist test cho developer

---

# 1. Mục tiêu hệ thống

Hệ thống save mới được thiết kế để thay thế cách save cũ chỉ lưu `PlayerStats` bằng `BinaryFormatter`.

Mục tiêu chính:

```text
- Dùng JSON thay cho BinaryFormatter.
- Tách dữ liệu save theo lifecycle rõ ràng.
- Không nhồi toàn bộ dữ liệu vào PlayerStats.
- Save bằng DTO, không serialize trực tiếp MonoBehaviour/GameObject/ScriptableObject.
- Có thể mở rộng về sau cho inventory, run snapshot, shop, meta progression.
```

Phase hiện tại tập trung vào:

```text
- Player stats
- Player money
- Weapon equipment
- Shop restock state
```

Chưa tập trung vào:

```text
- Armor restore
- Safe inventory persistence
- Dungeon inventory persistence
- Dungeon layout/enemy/world state
- Hub object state
```

---

# 2. Hiện trạng trước khi refactor

Trước refactor, `SaveSystem` chủ yếu lưu `PlayerStatsData` và `ShopRestockData` bằng `BinaryFormatter`. Save path cũ nằm dưới `Application.persistentDataPath/GameSaves`, trong đó player stats được lưu vào `playerStats.sav` và shop restock vào `shopRestock.sav` .

`PlayerStatsData` cũ đang chứa cả stat của player lẫn weapon loadout như `mainWeaponId`, `subWeaponId`, `activeSlot`, và các armor ID, khiến trách nhiệm bị trộn giữa player stats và equipment .

`AutoSaveManager` cũ cũng tự `LoadGame()` trong `Start()`, có thể gây load sai thời điểm khi scene transition giữa `GameHome`, `Dungeon`, và `RunResultScene` .

---

# 3. Kiến trúc save mới

Hệ thống mới chia trách nhiệm như sau:

```text
SaveSystem
= DTO file IO
= đọc/ghi/xoá JSON
= không biết scene object

AutoSaveManager
= runtime orchestration
= capture dữ liệu từ scene object
= apply dữ liệu lại vào scene object

DTO classes
= dữ liệu thuần để serialize
= không chứa Unity object reference

ShopRegistrySO
= source of truth cho toàn bộ persistent shop
= resolve shop bằng stable ShopId
```

Nguyên tắc chính:

```text
Save stable ID, không save Unity object.
Save data, không save behaviour.
Save lifecycle rõ ràng, không trộn profile với run snapshot.
```

---

# 4. Các file save chính

Save mới dùng các file JSON versioned dưới:

```text
Application.persistentDataPath/GameSaves/v1/
```

## 4.1 `profile.json`

Lưu dữ liệu profile lâu dài của player.

Dự kiến gồm:

```text
ProfileSaveFile
- version
- playerStats
- equipment
- playerMoney
- safeInventory placeholder
- hub placeholder
```

Hiện đã wire:

```text
- PlayerStatsSaveData
- EquipmentSaveData cho weapon
- PlayerMoneySaveData
```

Chưa wire:

```text
- SafeInventorySaveData
- HubSaveData
```

---

## 4.2 `shopRestock.json`

Lưu trạng thái shop.

Dữ liệu gồm:

```text
ShopRestockSaveFile
- version
- shopRestock
```

Mỗi shop entry lưu:

```text
- shopId
- nextRestockTicks
- stockEntries
- slotIndex
- currentStock
- optional debugItemName
```

Shop được resolve thông qua `ShopRegistrySO`, không tìm bằng scene object.

---

## 4.3 `runSnapshot.json`

Dành cho run snapshot / crash resume trong tương lai.

Hiện tại chỉ là infrastructure/API, chưa tự động dùng trong gameplay.

Chưa lưu:

```text
- dungeon layout
- enemies
- generated rooms
- runtime object state
- raw player transform
```

---

# 5. Luồng hoạt động

## 5.1 Save profile

Luồng save hiện tại:

```text
AutoSaveManager.SaveGame()
→ CaptureProfileFromScene()
→ SaveSystem.SaveProfile(profile)
→ SaveShopRestockState() best-effort
```

Trong đó:

```text
PlayerStats → PlayerStatsSaveData
EquipmentManager → EquipmentSaveData
PlayerMoney → PlayerMoneySaveData
ShopRegistrySO → ShopRestockSaveFile
```

Shop save là **best-effort**. Nếu thiếu `ShopRegistrySO` hoặc shop save lỗi, profile save vẫn không bị block.

---

## 5.2 Load profile

Luồng load:

```text
AutoSaveManager.LoadGame()
→ SaveSystem.LoadProfile()
→ ApplyProfileToScene(profile)
→ LoadShopRestockState() best-effort
```

Thứ tự apply:

```text
1. Player stats
2. Weapon equipment
3. Player money
4. Shop restock
```

Money phải được load qua `PlayerMoney.SetGold(int)` để trigger `OnGoldChanged` và refresh UI.

---

## 5.3 Shop save/load

Shop persistence dùng `ShopRegistrySO` làm source of truth.

Capture:

```text
ShopRegistrySO.GetAll()
→ từng ShopInventorySO.CreateSaveEntry()
→ shopRestock.json
```

Apply:

```text
shopRestock.json
→ resolve shopId bằng ShopRegistrySO.GetById()
→ ShopInventorySO.ApplySaveEntry()
```

Nếu gặp lỗi:

```text
- unknown shopId
- slotIndex ngoài range
- empty slot
- debugItemName mismatch
```

thì hệ thống warning và skip entry, không apply bừa.

---

# 6. Các domain đã được hỗ trợ

## 6.1 PlayerStats

Đã tách khỏi equipment.

`PlayerStats.LoadFromData(...)` hiện chỉ nên apply stat data, không restore weapon nữa.

Trách nhiệm:

```text
PlayerStats
= HP/Mana/Stamina/base stat/level/exp
```

Không còn trách nhiệm:

```text
- restore weapon
- restore armor
- restore inventory
```

---

## 6.2 Equipment

Weapon save/load đã được wire.

Dữ liệu:

```text
EquipmentSaveData
- mainWeaponId
- subWeaponId
- activeSlot
- armor IDs placeholder
```

Restore weapon qua:

```text
EquipmentManager.LoadWeapons(mainWeaponId, subWeaponId, activeSlot)
```

Armor field có thể tồn tại trong DTO, nhưng armor restore chưa wire vì chưa có ID-based restore API ổn định.

---

## 6.3 PlayerMoney

Đã wire:

```text
Save:
PlayerMoney.Gold → PlayerMoneySaveData.gold

Load:
PlayerMoneySaveData.gold → PlayerMoney.SetGold(int)
```

Không được load bằng direct field assignment, vì UI cần event refresh.

---

## 6.4 ShopRestock

Đã implement code path:

```text
ShopRegistrySO
ShopInventorySO.CreateSaveEntry()
ShopInventorySO.ApplySaveEntry()
shopRestock.json
```

Nhưng cần chú ý: **ShopRegistrySO asset chưa được tạo/assign thì shop save/load sẽ warn và skip**.

---

## 6.5 SafeInventory / DungeonInventory

Chưa wire.

Lý do:

```text
- ItemSO.ID hiện là runtime-only.
- Chưa có stable authored item ID.
- Chưa có ItemRegistry để resolve saved itemId về ItemSO.
```

Vì vậy hiện tại chỉ nên giữ DTO placeholder, không serialize item thật.

---

# 7. Implementation status

## Đã hoàn thành

```text
[Done] Phase 1 JSON save infrastructure
[Done] SaveSystem DTO-only JSON IO
[Done] profile.json / runSnapshot.json / shopRestock.json structure
[Done] temp-first JSON write
[Done] safe JSON read failure
[Done] CreateDefaultProfile()
[Done] PlayerStatsSaveData
[Done] EquipmentSaveData
[Done] PlayerMoneySaveData
[Done] ShopRestock DTO expansion
[Done] AutoSaveManager capture/apply profile
[Done] Weapon save/load through EquipmentManager
[Done] Money save/load through PlayerMoney.SetGold()
[Done] ShopRegistrySO script
[Done] Registry-backed shop restock save/load code path
[Done] Defensive shop stock restore
[Done] Unity compile clean
```

## Chưa hoàn thành

```text
[Pending] Create actual ShopRegistrySO asset
[Pending] Assign ShopRegistrySO to AutoSaveManager
[Pending] Runtime-test no-save startup
[Pending] Runtime-test profile save/load
[Pending] Runtime-test MoneyUI refresh
[Pending] Runtime-test weapon restore
[Pending] Runtime-test shop restock save/load
[Pending] Runtime-test scene transition save triggers
```

## Deferred

```text
[Deferred] Armor restore
[Deferred] SafeInventory persistence
[Deferred] DungeonInventory persistence
[Deferred] true run crash-resume
[Deferred] dungeon layout/enemy/world save
[Deferred] hub object state save
[Deferred] raw player transform persistence
```

---

# 8. Những điểm cần chú ý

## 8.1 Không dùng lại BinaryFormatter

`.sav` cũ được giữ lại trên disk nhưng không còn là normal save path.

Không migrate, không xoá tự động.

---

## 8.2 Không save Unity object reference

Không save:

```text
- MonoBehaviour
- GameObject
- Transform
- ScriptableObject reference trực tiếp
- Runtime instance ID
- GetInstanceID()
```

Chỉ save:

```text
- string ID ổn định
- int/float/bool
- DTO thuần
```

---

## 8.3 ShopRegistrySO là bắt buộc cho shop persistence

Shop save/load không scan scene.

Không dùng:

```text
FindObjectsByType<ShopController>()
ShopNPC loaded trong scene
GameObject name
Scene hierarchy
```

Dùng:

```text
ShopRegistrySO.GetAll()
ShopRegistrySO.GetById(shopId)
```

---

## 8.4 Shop save/load không được làm fail profile

Profile là dữ liệu chính. Shop là dữ liệu phụ.

Nếu shop registry thiếu hoặc shop data lỗi:

```text
- warning
- skip
- profile save/load vẫn tiếp tục
```

---

## 8.5 Inventory chưa an toàn để save

Không wire inventory cho tới khi có:

```text
- stable authored ItemId trên ItemSO
- ItemRegistry
- import/export API cho InventoryController
```

---

# 9. Công việc tiếp theo

## 9.1 Setup ShopRegistrySO

Cần tạo asset:

```text
Assets/ScriptableObjects/Shops/ShopRegistry.asset
```

Sau đó add toàn bộ `ShopInventorySO` assets vào list.

Yêu cầu:

```text
- ShopId không rỗng
- ShopId không trùng
- ShopId ổn định, không đổi sau khi đã release save
```

Assign asset này vào `AutoSaveManager.shopRegistry`.

---

## 9.2 Runtime validation

Test bắt buộc:

```text
1. No-save startup
2. Save profile
3. Load profile
4. MoneyUI refresh
5. Weapon restore
6. Shop restock save/load
7. Corrupt/missing JSON handling
8. Scene flow regression
```

Scene flow cần test:

```text
GameHome → Dungeon → RunResultScene → GameHome
```

Và các trigger save:

```text
- PauseMenu.Quit()
- RunResultController.ReturnToHub()
- PlayerStats.RespawnToHubFallback()
- ChangeSceneOnCollide
```

---

## 9.3 Phase 2B sau khi Phase 2A ổn

Phase 2B nên làm inventory identity trước:

```text
1. Add stable authored ItemId to ItemSO
2. Add ItemRegistry
3. Add SafeInventory import/export API
4. Wire SafeInventorySaveData into ProfileSaveFile
```

Chưa nên làm `DungeonInventory` trước `SafeInventory`.

---

# 10. Checklist test cho developer

## Save file

```text
[ ] profile.json được tạo khi SaveGame()
[ ] shopRestock.json được tạo khi có ShopRegistrySO
[ ] runSnapshot.json không bị tạo ngoài ý muốn
[ ] .sav cũ không bị xoá
```

## Startup

```text
[ ] Không có profile.json vẫn vào GameHome bình thường
[ ] LoadProfile() không tự tạo profile
[ ] Corrupt profile.json không crash
```

## Player

```text
[ ] Player stats load đúng
[ ] Weapon main/sub load đúng
[ ] Active slot load đúng
[ ] Không duplicate weapon object
```

## Money

```text
[ ] Gold save đúng
[ ] Gold load qua SetGold()
[ ] MoneyUI refresh đúng
```

## Shop

```text
[ ] ShopRegistrySO đã assign vào AutoSaveManager
[ ] Không có ShopId rỗng
[ ] Không có ShopId trùng
[ ] nextRestockTicks save/load đúng
[ ] currentStock save/load đúng
[ ] unknown shopId warning + skip
[ ] slotIndex out-of-range warning + skip
[ ] debugItemName mismatch warning + skip
```

## Scene flow

```text
[ ] GameHome → Dungeon không lỗi
[ ] Dungeon → RunResultScene không lỗi
[ ] RunResultScene → GameHome không spawn sai
[ ] PauseMenu.Quit() vẫn save được
[ ] TrySaveActiveSceneState() vẫn compile và hoạt động
```

---

# Tóm tắt ngắn

Save system hiện tại đã chuyển từ prototype binary save sang kiến trúc JSON lifecycle save.

```text
Profile save:
player stats + weapon equipment + money

Shop save:
registry-backed restock timing + stock count

Run snapshot:
infrastructure only

Inventory:
deferred until stable item ID + registry
```
