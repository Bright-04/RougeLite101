using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class WeaponHUDUI : MonoBehaviour
{
    private EquipmentManager equipmentManager;

    [Header("Icon Displays")]
    //[SerializeField] private Image mainWeaponIcon; // Ô UI to (Main)
    //[SerializeField] private Image subWeaponIcon;  // Ô UI nhỏ (Sub)
    [SerializeField] private WeaponSlotUI mainWeaponSlot;
    [SerializeField] private WeaponSlotUI subWeaponSlot;

    [SerializeField]
    private InventoryController inventoryController;

    [Header("Settings")]
    [SerializeField] private Sprite emptySlotIcon;

    private void Start()
    {
        equipmentManager = FindAnyObjectByType<EquipmentManager>();

        if (equipmentManager != null)
        {
            // Cả hai sự kiện đều kích hoạt việc vẽ lại toàn bộ icon
            equipmentManager.OnWeaponChanged += (slot, def) => RefreshAllIcons();
            equipmentManager.OnActiveSlotChanged += (slot) => RefreshAllIcons();

            RefreshAllIcons();
        }

        if (inventoryController == null)
        {
            inventoryController = FindAnyObjectByType<InventoryController>();
        }
        mainWeaponSlot.OnRightClickWeaponSlot += HandleRightClick;
        subWeaponSlot.OnRightClickWeaponSlot += HandleRightClick;
    }

    private void HandleRightClick(WeaponSlotUI slotUI)
    {
        EquipmentManager.WeaponSlot active = equipmentManager.GetActiveSlot();

        EquipmentManager.WeaponSlot inactive = active == EquipmentManager.WeaponSlot.Main
            ? EquipmentManager.WeaponSlot.Sub
            : EquipmentManager.WeaponSlot.Main;

        EquipmentManager.WeaponSlot actualSlot;

        // MAIN ICON = ACTIVE
        if (slotUI == mainWeaponSlot)
        {
            actualSlot = active;
        }
        // SUB ICON = INACTIVE
        else
        {
            actualSlot = inactive;
        }

        WeaponDefinitionSO weapon = equipmentManager.GetWeaponDefinition(actualSlot);

        if (weapon == null) return;

        inventoryController.CurrentInventoryData.AddItem(weapon, 1);

        equipmentManager.UnequipWeapon(actualSlot);
    }

    private void OnDestroy()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponChanged -= (slot, def) => RefreshAllIcons();
            equipmentManager.OnActiveSlotChanged -= (slot) => RefreshAllIcons();
        }
        mainWeaponSlot.OnRightClickWeaponSlot -= HandleRightClick;
        subWeaponSlot.OnRightClickWeaponSlot -= HandleRightClick;
    }

    private void RefreshAllIcons()
    {
        if (equipmentManager == null) return;

        // Xác định Weapon nào đang là "Active" trên tay người chơi
        EquipmentManager.WeaponSlot activeSlot = equipmentManager.GetActiveSlot();
        EquipmentManager.WeaponSlot inactiveSlot = (activeSlot == EquipmentManager.WeaponSlot.Main) 
            ? EquipmentManager.WeaponSlot.Sub 
            : EquipmentManager.WeaponSlot.Main;

        WeaponDefinitionSO activeDef = equipmentManager.GetWeaponDefinition(activeSlot);
        WeaponDefinitionSO inactiveDef = equipmentManager.GetWeaponDefinition(inactiveSlot);

        // Ô chính luôn hiện vũ khí đang Active
        //SetIcon(mainWeaponIcon, activeDef);

        // Ô phụ hiện vũ khí còn lại (nếu có)
        //SetIcon(subWeaponIcon, inactiveDef);
        SetIcon(mainWeaponSlot.Icon, activeDef);
        SetIcon(subWeaponSlot.Icon, inactiveDef);
    }

    private void SetIcon(Image targetIcon, WeaponDefinitionSO weaponDef)
    {
        if (targetIcon == null) return;

        if (weaponDef != null && weaponDef.ItemImage != null)
        {
            targetIcon.sprite = weaponDef.ItemImage;
            targetIcon.color = Color.white;
        }
        else
        {
            targetIcon.sprite = emptySlotIcon;
            // Nếu không có icon và không có hình mặc định, làm Icon tàng hình
            targetIcon.color = (emptySlotIcon != null) ? Color.white : new Color(1, 1, 1, 0);
        }
    }
}
