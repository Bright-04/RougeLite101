using UnityEngine;
using UnityEngine.UI;

public class WeaponHUDUI : MonoBehaviour
{
    private EquipmentManager equipmentManager;

    [Header("Slot Containers")]
    [SerializeField] private Image mainSlotImage;
    [SerializeField] private Image subSlotImage;

    [Header("Icon Displays")]
    [SerializeField] private Image mainWeaponIcon;
    [SerializeField] private Image subWeaponIcon;

    [Header("Colors / Highlighting")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    [SerializeField] private float activeScale = 1.1f;
    [SerializeField] private float inactiveScale = 1.0f;

    [Header("Settings")]
    [SerializeField] private Sprite emptySlotIcon;

    private void Start()
    {
        equipmentManager = FindAnyObjectByType<EquipmentManager>();

        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponChanged += UpdateWeaponIcon;
            equipmentManager.OnActiveSlotChanged += UpdateActiveSlotHighlight;

            // Initial UI state
            UpdateWeaponIcon(EquipmentManager.WeaponSlot.Main, equipmentManager.GetWeaponDefinition(EquipmentManager.WeaponSlot.Main));
            UpdateWeaponIcon(EquipmentManager.WeaponSlot.Sub, equipmentManager.GetWeaponDefinition(EquipmentManager.WeaponSlot.Sub));
            UpdateActiveSlotHighlight(equipmentManager.GetActiveSlot());
        }
    }

    private void OnDestroy()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponChanged -= UpdateWeaponIcon;
            equipmentManager.OnActiveSlotChanged -= UpdateActiveSlotHighlight;
        }
    }

    private void UpdateWeaponIcon(EquipmentManager.WeaponSlot slot, WeaponDefinitionSO weaponDef)
    {
        Image targetIcon = (slot == EquipmentManager.WeaponSlot.Main) ? mainWeaponIcon : subWeaponIcon;
        
        if (targetIcon == null) return;

        if (weaponDef != null && weaponDef.Icon != null)
        {
            targetIcon.sprite = weaponDef.Icon;
            targetIcon.color = Color.white;
        }
        else
        {
            targetIcon.sprite = emptySlotIcon;
            // If no icon and no placeholder, make it invisible
            targetIcon.color = (emptySlotIcon != null) ? Color.white : new Color(1, 1, 1, 0);
        }
    }

    private void UpdateActiveSlotHighlight(EquipmentManager.WeaponSlot activeSlot)
    {
        bool isMainActive = (activeSlot == EquipmentManager.WeaponSlot.Main);

        ApplyHighlight(mainSlotImage, mainWeaponIcon, isMainActive);
        ApplyHighlight(subSlotImage, subWeaponIcon, !isMainActive);
    }

    private void ApplyHighlight(Image slotBg, Image icon, bool isActive)
    {
        if (slotBg != null)
        {
            slotBg.color = isActive ? activeColor : inactiveColor;
            slotBg.transform.localScale = Vector3.one * (isActive ? activeScale : inactiveScale);
        }

        if (icon != null)
        {
            // Dim the icon slightly if its slot is inactive
            Color iconColor = icon.color;
            iconColor.a = isActive ? 1.0f : 0.6f;
            icon.color = iconColor;
        }
    }
}
