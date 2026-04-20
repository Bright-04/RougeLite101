using UnityEngine;
using UnityEngine.UI;

public class WeaponHUDUI : MonoBehaviour
{
    private EquipmentManager equipmentManager;

    [Header("Icon Displays")]
    [SerializeField] private Image mainWeaponIcon; // Ô UI to (Main)
    [SerializeField] private Image subWeaponIcon;  // Ô UI nhỏ (Sub)

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
    }

    private void OnDestroy()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponChanged -= (slot, def) => RefreshAllIcons();
            equipmentManager.OnActiveSlotChanged -= (slot) => RefreshAllIcons();
        }
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
        SetIcon(mainWeaponIcon, activeDef);

        // Ô phụ hiện vũ khí còn lại (nếu có)
        SetIcon(subWeaponIcon, inactiveDef);
    }

    private void SetIcon(Image targetIcon, WeaponDefinitionSO weaponDef)
    {
        if (targetIcon == null) return;

        if (weaponDef != null && weaponDef.Icon != null)
        {
            targetIcon.sprite = weaponDef.Icon;
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
