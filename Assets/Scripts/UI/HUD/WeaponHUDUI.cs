using UnityEngine;
using UnityEngine.UI;

public class WeaponHUDUI : MonoBehaviour
{
    private EquipmentManager equipmentManager;
    private WeaponSlotUI mainSlotUI;
    private WeaponSlotUI subSlotUI;
    private bool isSubscribed;
    private bool missingSlotRootLogged;

    [Header("Icon Displays")]
    [SerializeField] private Image mainWeaponIcon; // Active weapon visual
    [SerializeField] private Image subWeaponIcon;  // Inactive weapon visual

    [Header("Settings")]
    [SerializeField] private Sprite emptySlotIcon;

    private void OnEnable()
    {
        EnsureReferences();
        SubscribeEvents();
        RefreshAllIcons();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void EnsureReferences()
    {
        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
        }

        mainSlotUI = ResolveSlotUI(mainWeaponIcon);
        subSlotUI = ResolveSlotUI(subWeaponIcon);
    }

    private WeaponSlotUI ResolveSlotUI(Image icon)
    {
        if (icon == null || icon.transform.parent == null)
        {
            if (!missingSlotRootLogged)
            {
                Debug.LogWarning($"WeaponHUDUI on '{name}' is missing an icon reference or slot root parent.", this);
                missingSlotRootLogged = true;
            }

            return null;
        }

        WeaponSlotUI slotUI = icon.transform.parent.GetComponent<WeaponSlotUI>();
        if (slotUI == null && Application.isPlaying)
        {
            slotUI = icon.transform.parent.gameObject.AddComponent<WeaponSlotUI>();
        }

        return slotUI;
    }

    private void SubscribeEvents()
    {
        if (isSubscribed)
        {
            return;
        }

        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponChanged += HandleWeaponChanged;
            equipmentManager.OnActiveSlotChanged += HandleActiveSlotChanged;
        }

        if (mainSlotUI != null)
        {
            mainSlotUI.OnRightClick += HandleSlotRightClick;
        }

        if (subSlotUI != null)
        {
            subSlotUI.OnRightClick += HandleSlotRightClick;
        }

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponChanged -= HandleWeaponChanged;
            equipmentManager.OnActiveSlotChanged -= HandleActiveSlotChanged;
        }

        if (mainSlotUI != null)
        {
            mainSlotUI.OnRightClick -= HandleSlotRightClick;
        }

        if (subSlotUI != null)
        {
            subSlotUI.OnRightClick -= HandleSlotRightClick;
        }

        isSubscribed = false;
    }

    private void HandleWeaponChanged(EquipmentManager.WeaponSlot slot, WeaponDefinitionSO definition)
    {
        RefreshAllIcons();
    }

    private void HandleActiveSlotChanged(EquipmentManager.WeaponSlot slot)
    {
        RefreshAllIcons();
    }

    private void HandleSlotRightClick(WeaponSlotUI slotUI)
    {
        if (equipmentManager == null || slotUI == null)
        {
            Debug.Log("HandleSlotRightClick weapon failed");
            return;
        }

        EquipmentManager.WeaponSlot targetSlot = ResolveTargetSlot(slotUI);
        if (equipmentManager.GetWeaponDefinition(targetSlot) == null)
        {
            Debug.Log("HandleSlotRightClick GetWeaponDefinition failed");
            return;
        }

        equipmentManager.UnequipWeapon(targetSlot);
    }

    private EquipmentManager.WeaponSlot ResolveTargetSlot(WeaponSlotUI slotUI)
    {
        EquipmentManager.WeaponSlot activeSlot = equipmentManager.GetActiveSlot();
        EquipmentManager.WeaponSlot inactiveSlot = activeSlot == EquipmentManager.WeaponSlot.Main
            ? EquipmentManager.WeaponSlot.Sub
            : EquipmentManager.WeaponSlot.Main;

        return slotUI == mainSlotUI ? activeSlot : inactiveSlot;
    }

    private void RefreshAllIcons()
    {
        if (equipmentManager == null)
        {
            return;
        }

        EquipmentManager.WeaponSlot activeSlot = equipmentManager.GetActiveSlot();
        EquipmentManager.WeaponSlot inactiveSlot = activeSlot == EquipmentManager.WeaponSlot.Main
            ? EquipmentManager.WeaponSlot.Sub
            : EquipmentManager.WeaponSlot.Main;

        WeaponDefinitionSO activeDef = equipmentManager.GetWeaponDefinition(activeSlot);
        WeaponDefinitionSO inactiveDef = equipmentManager.GetWeaponDefinition(inactiveSlot);

        SetIcon(mainWeaponIcon, activeDef);
        SetIcon(subWeaponIcon, inactiveDef);
    }

    private void SetIcon(Image targetIcon, WeaponDefinitionSO weaponDef)
    {
        if (targetIcon == null)
        {
            return;
        }

        if (weaponDef != null && weaponDef.ItemImage != null)
        {
            targetIcon.sprite = weaponDef.ItemImage;
            targetIcon.color = Color.white;
        }
        else
        {
            targetIcon.sprite = emptySlotIcon;
            targetIcon.color = emptySlotIcon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
    }
}
