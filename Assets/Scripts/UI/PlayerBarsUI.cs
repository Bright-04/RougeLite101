using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private PlayerStats playerStats;
    private EquipmentManager equipmentManager;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text manaText;

    [Header("Weapon HUD")]
    [SerializeField] private Image mainWeaponIcon;
    [SerializeField] private Image subWeaponIcon;
    [SerializeField] private GameObject mainSlotHighlight;
    [SerializeField] private GameObject subSlotHighlight;
    [SerializeField] private Sprite emptyWeaponIcon;

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        equipmentManager = FindAnyObjectByType<EquipmentManager>();

        if (playerStats == null)
        {
            Debug.LogError("PlayerBarUI: Could not find PlayerStats!");
        }

        if (equipmentManager == null)
        {
            Debug.LogWarning("PlayerBarUI: Could not find EquipmentManager!");
        }
        else
        {
            equipmentManager.OnWeaponChanged += OnWeaponChanged;
            equipmentManager.OnActiveSlotChanged += OnActiveSlotChanged;

            UpdateWeaponSlotVisual(EquipmentManager.WeaponSlot.Main, equipmentManager.GetWeaponDefinition(EquipmentManager.WeaponSlot.Main));
            UpdateWeaponSlotVisual(EquipmentManager.WeaponSlot.Sub, equipmentManager.GetWeaponDefinition(EquipmentManager.WeaponSlot.Sub));
            UpdateActiveSlotVisual(equipmentManager.GetActiveSlot());
        }
    }

    private void OnDestroy()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponChanged -= OnWeaponChanged;
            equipmentManager.OnActiveSlotChanged -= OnActiveSlotChanged;
        }
    }

    private void Update()
    {
        UpdateHealthUI();
        UpdateManaUI();     
    }

    private void UpdateHealthUI()
    {
        if (playerStats == null) return;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = playerStats.maxHP;
            healthSlider.value = playerStats.currentHP;
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.FloorToInt(playerStats.currentHP)} / {playerStats.maxHP}";
        }
            
    }

    private void UpdateManaUI()
    {
        if (playerStats == null) return;

        if (manaSlider != null)
        {
            manaSlider.maxValue = playerStats.maxMana;
            manaSlider.value = playerStats.currentMana;
        }

        if (manaText != null)
        {
            manaText.text = $"{Mathf.FloorToInt(playerStats.currentMana)} / {playerStats.maxMana}";
        }
            
    }

    private void OnWeaponChanged(EquipmentManager.WeaponSlot slot, WeaponDefinitionSO weaponDef)
    {
        UpdateWeaponSlotVisual(slot, weaponDef);
    }

    private void OnActiveSlotChanged(EquipmentManager.WeaponSlot slot)
    {
        UpdateActiveSlotVisual(slot);
    }

    private void UpdateWeaponSlotVisual(EquipmentManager.WeaponSlot slot, WeaponDefinitionSO weaponDef)
    {
        Image targetImage = slot == EquipmentManager.WeaponSlot.Main ? mainWeaponIcon : subWeaponIcon;
        if (targetImage == null)
        {
            return;
        }

        if (weaponDef != null && weaponDef.Icon != null)
        {
            targetImage.sprite = weaponDef.Icon;
            targetImage.color = Color.white;
            return;
        }

        targetImage.sprite = emptyWeaponIcon;
        targetImage.color = emptyWeaponIcon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
    }

    private void UpdateActiveSlotVisual(EquipmentManager.WeaponSlot activeSlot)
    {
        if (mainSlotHighlight != null)
        {
            mainSlotHighlight.SetActive(activeSlot == EquipmentManager.WeaponSlot.Main);
        }

        if (subSlotHighlight != null)
        {
            subSlotHighlight.SetActive(activeSlot == EquipmentManager.WeaponSlot.Sub);
        }
    }
}
