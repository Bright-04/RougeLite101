using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponPickupModalUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject modalRoot;

    [Header("New Weapon")]
    [SerializeField] private Image newWeaponIcon;
    [SerializeField] private TMP_Text newWeaponNameText;

    [Header("Main Slot")]
    [SerializeField] private Image mainWeaponIcon;
    [SerializeField] private TMP_Text mainWeaponNameText;
    [SerializeField] private Button replaceMainButton;

    [Header("Sub Slot")]
    [SerializeField] private Image subWeaponIcon;
    [SerializeField] private TMP_Text subWeaponNameText;
    [SerializeField] private Button replaceSubButton;

    [Header("Common")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Sprite emptySlotIcon;

    private Action<EquipmentManager.WeaponSlot?> resolveCallback;

    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

    private void Awake()
    {
        if (replaceMainButton != null)
        {
            replaceMainButton.onClick.AddListener(OnReplaceMainClicked);
        }

        if (replaceSubButton != null)
        {
            replaceSubButton.onClick.AddListener(OnReplaceSubClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (replaceMainButton != null)
        {
            replaceMainButton.onClick.RemoveListener(OnReplaceMainClicked);
        }

        if (replaceSubButton != null)
        {
            replaceSubButton.onClick.RemoveListener(OnReplaceSubClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipClicked);
        }
    }

    public void Show(
        WeaponDefinitionSO newWeapon,
        WeaponDefinitionSO currentMain,
        WeaponDefinitionSO currentSub,
        Action<EquipmentManager.WeaponSlot?> callback)
    {
        resolveCallback = callback;

        ApplyWeaponView(newWeaponIcon, newWeaponNameText, newWeapon);
        ApplyWeaponView(mainWeaponIcon, mainWeaponNameText, currentMain);
        ApplyWeaponView(subWeaponIcon, subWeaponNameText, currentSub);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.EnableUIMap();
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
        }
    }

    private void OnReplaceMainClicked()
    {
        Resolve(EquipmentManager.WeaponSlot.Main);
    }

    private void OnReplaceSubClicked()
    {
        Resolve(EquipmentManager.WeaponSlot.Sub);
    }

    private void OnSkipClicked()
    {
        Resolve(null);
    }

    private void Resolve(EquipmentManager.WeaponSlot? slot)
    {
        Action<EquipmentManager.WeaponSlot?> callback = resolveCallback;
        resolveCallback = null;

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.DisableUIMap();
        }

        callback?.Invoke(slot);
    }

    private void ApplyWeaponView(Image iconImage, TMP_Text nameText, WeaponDefinitionSO definition)
    {
        if (iconImage != null)
        {
            if (definition != null && definition.Icon != null)
            {
                iconImage.sprite = definition.Icon;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.sprite = emptySlotIcon;
                iconImage.color = emptySlotIcon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }

        if (nameText != null)
        {
            nameText.text = definition != null ? definition.DisplayName : "Empty";
        }
    }
}
