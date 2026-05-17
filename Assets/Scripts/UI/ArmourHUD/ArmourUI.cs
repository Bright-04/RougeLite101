using UnityEngine;
using UnityEngine.UI;

public class ArmourUI : MonoBehaviour
{
    [SerializeField] private ArmourController armourController;

    [Header("Icon Displays")]
    [SerializeField] private Image helmetIcon; 
    [SerializeField] private Image chestplateIcon;
    [SerializeField] private Image leggingIcon;
    [SerializeField] private Image bootsIcon;

    [SerializeField] private Sprite emptyIcon;

    private void Start()
    {
        if (armourController == null)
        {
            armourController = FindFirstObjectByType<ArmourController>(FindObjectsInactive.Include);
        }
        armourController.OnArmourChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDestroy()
    {
        armourController.OnArmourChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        UpdateSlot(helmetIcon, armourController.Helmet);
        UpdateSlot(chestplateIcon, armourController.Chestplate);
        UpdateSlot(leggingIcon, armourController.Leggings);
        UpdateSlot(bootsIcon, armourController.Boots);
    }

    private void UpdateSlot(Image slot, ArmourItemSO armour)
    {
        if (armour == null)
        {
            slot.sprite = emptyIcon;
            return;
        }

        slot.sprite = armour.ItemImage;
    }
}
