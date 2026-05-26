using UnityEngine;

public class ArmourUI : MonoBehaviour
{
    [SerializeField] private EquipmentController equipmentController;

    [Header("Icon Displays")]
    [SerializeField] private ArmourSlotUI shieldIcon;
    [SerializeField] private ArmourSlotUI helmetIcon;
    [SerializeField] private ArmourSlotUI greavesIcon;
    [SerializeField] private ArmourSlotUI bootsIcon;

    [SerializeField] private Sprite emptyIcon;

    private void Start()
    {
        if (equipmentController == null)
        {
            equipmentController = FindFirstObjectByType<EquipmentController>(FindObjectsInactive.Include);
        }

        if (equipmentController == null)
        {
            Debug.LogError("ArmourUI: EquipmentController not found.");
            return;
        }

        equipmentController.OnArmorEquipped += OnArmorEquipped;
        RegisterSlotEvents();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (equipmentController != null)
        {
            equipmentController.OnArmorEquipped -= OnArmorEquipped;
        }

        UnregisterSlotEvents();
    }

    private void OnArmorEquipped(EquipmentController.ArmorSlot slot, ArmorDefinitionSO previousArmor, ArmorDefinitionSO newArmor)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        UpdateSlot(shieldIcon, equipmentController.GetArmor(EquipmentController.ArmorSlot.Shield));
        UpdateSlot(helmetIcon, equipmentController.GetArmor(EquipmentController.ArmorSlot.Helmet));
        UpdateSlot(greavesIcon, equipmentController.GetArmor(EquipmentController.ArmorSlot.Greaves));
        UpdateSlot(bootsIcon, equipmentController.GetArmor(EquipmentController.ArmorSlot.Boots));
    }

    private void UpdateSlot(ArmourSlotUI slotUI, ArmorDefinitionSO armor)
    {
        if (slotUI == null || slotUI.ItemImage == null)
        {
            return;
        }

        slotUI.ItemImage.sprite = armor != null ? armor.ItemImage : emptyIcon;
    }

    private void HandleRightClick(ArmourSlotUI slotUI)
    {
        if (equipmentController != null && slotUI != null)
        {
            equipmentController.UnequipArmor(slotUI.ArmorSlot);
        }
    }

    private void RegisterSlotEvents()
    {
        Subscribe(shieldIcon);
        Subscribe(helmetIcon);
        Subscribe(greavesIcon);
        Subscribe(bootsIcon);
    }

    private void UnregisterSlotEvents()
    {
        Unsubscribe(shieldIcon);
        Unsubscribe(helmetIcon);
        Unsubscribe(greavesIcon);
        Unsubscribe(bootsIcon);
    }

    private void Subscribe(ArmourSlotUI slotUI)
    {
        if (slotUI != null)
        {
            slotUI.OnRightMouseBtnClickArmourSlot += HandleRightClick;
        }
    }

    private void Unsubscribe(ArmourSlotUI slotUI)
    {
        if (slotUI != null)
        {
            slotUI.OnRightMouseBtnClickArmourSlot -= HandleRightClick;
        }
    }
}
