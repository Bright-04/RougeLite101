using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EquipmentManager : MonoBehaviour
{
    public enum WeaponSlot
    {
        Main = 0,
        Sub = 1
    }

    [Header("Runtime References")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponPickupModalUI weaponPickupModalUI;

    [Header("Data")]
    [SerializeField] private WeaponRegistry weaponRegistry;
    [SerializeField] private WeaponDefinitionSO startingWeaponDef;

    private WeaponDefinitionSO mainWeaponDef;
    private WeaponDefinitionSO subWeaponDef;
    private Weapon mainWeaponInstance;
    private Weapon subWeaponInstance;
    private WeaponSlot activeSlot = WeaponSlot.Main;

    private WeaponDefinitionSO pendingPickupDefinition;
    private WeaponPickup pendingPickupSource;

    private PlayerControls playerControls;
    private InputAction attackAction;
    private InputAction swapWeaponAction;

    public event Action<WeaponSlot, WeaponDefinitionSO> OnWeaponChanged;
    public event Action<WeaponSlot> OnActiveSlotChanged;

    private void Awake()
    {
        if (weaponRegistry != null)
        {
            weaponRegistry.Initialize();
        }
    }

    private void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }

        playerControls = InputManager.Instance.Controls;
        attackAction = playerControls.asset.FindAction("Combat/Attack");
        swapWeaponAction = playerControls.asset.FindAction("Combat/SwapWeapon");

        if (attackAction != null)
        {
            attackAction.started += OnAttackPerformed;
        }
        else
        {
            Debug.LogWarning("EquipmentManager: Combat/Attack action not found.", this);
        }

        if (swapWeaponAction != null)
        {
            swapWeaponAction.performed += OnSwapWeaponPerformed;
        }

        if (mainWeaponDef == null && subWeaponDef == null && startingWeaponDef != null)
        {
            EquipIntoSlot(WeaponSlot.Main, startingWeaponDef);
        }

        SetActiveSlot(activeSlot, true);
    }

    private void OnDestroy()
    {
        if (attackAction != null)
        {
            attackAction.started -= OnAttackPerformed;
        }

        if (swapWeaponAction != null)
        {
            swapWeaponAction.performed -= OnSwapWeaponPerformed;
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        GetWeaponInstance(activeSlot)?.Use();
    }

    private void OnSwapWeaponPerformed(InputAction.CallbackContext context)
    {
        SwapActiveSlot();
    }

    public bool TryPickupWeapon(WeaponDefinitionSO newWeaponDef, WeaponPickup sourcePickup = null)
    {
        if (newWeaponDef == null)
        {
            return false;
        }

        if (mainWeaponDef == null)
        {
            EquipIntoSlot(WeaponSlot.Main, newWeaponDef);
            SetActiveSlot(WeaponSlot.Main, true);
            return true;
        }

        if (subWeaponDef == null)
        {
            EquipIntoSlot(WeaponSlot.Sub, newWeaponDef);
            return true;
        }

        if (weaponPickupModalUI != null)
        {
            pendingPickupDefinition = newWeaponDef;
            pendingPickupSource = sourcePickup;
            weaponPickupModalUI.Show(newWeaponDef, mainWeaponDef, subWeaponDef, OnPickupResolved);
            return false;
        }

        ReplaceWeapon(activeSlot, newWeaponDef);
        return true;
    }

    public void ReplaceWeapon(WeaponSlot targetSlot, WeaponDefinitionSO newWeaponDef)
    {
        if (newWeaponDef == null)
        {
            return;
        }

        EquipIntoSlot(targetSlot, newWeaponDef);

        if (GetWeaponInstance(activeSlot) == null)
        {
            if (mainWeaponDef != null)
            {
                SetActiveSlot(WeaponSlot.Main, true);
            }
            else if (subWeaponDef != null)
            {
                SetActiveSlot(WeaponSlot.Sub, true);
            }
        }
    }

    public void SwapActiveSlot()
    {
        WeaponSlot targetSlot = activeSlot == WeaponSlot.Main ? WeaponSlot.Sub : WeaponSlot.Main;
        if (GetWeaponInstance(targetSlot) == null)
        {
            return;
        }

        SetActiveSlot(targetSlot, false);
    }

    public void LoadWeapons(string mainWeaponId, string subWeaponId, WeaponSlot loadedActiveSlot)
    {
        ClearSlot(WeaponSlot.Main);
        ClearSlot(WeaponSlot.Sub);

        WeaponDefinitionSO loadedMain = weaponRegistry != null ? weaponRegistry.GetById(mainWeaponId) : null;
        WeaponDefinitionSO loadedSub = weaponRegistry != null ? weaponRegistry.GetById(subWeaponId) : null;

        if (loadedMain != null)
        {
            EquipIntoSlot(WeaponSlot.Main, loadedMain);
        }

        if (loadedSub != null)
        {
            EquipIntoSlot(WeaponSlot.Sub, loadedSub);
        }

        if (mainWeaponDef == null && subWeaponDef == null && startingWeaponDef != null)
        {
            EquipIntoSlot(WeaponSlot.Main, startingWeaponDef);
        }

        WeaponSlot finalActiveSlot = loadedActiveSlot;
        if (GetWeaponInstance(finalActiveSlot) == null)
        {
            finalActiveSlot = mainWeaponDef != null ? WeaponSlot.Main : WeaponSlot.Sub;
        }

        SetActiveSlot(finalActiveSlot, true);
    }

    public string GetMainWeaponId()
    {
        return mainWeaponDef != null ? mainWeaponDef.WeaponId : string.Empty;
    }

    public string GetSubWeaponId()
    {
        return subWeaponDef != null ? subWeaponDef.WeaponId : string.Empty;
    }

    public WeaponSlot GetActiveSlot()
    {
        return activeSlot;
    }

    public WeaponDefinitionSO GetWeaponDefinition(WeaponSlot slot)
    {
        return slot == WeaponSlot.Main ? mainWeaponDef : subWeaponDef;
    }

    private void OnPickupResolved(WeaponSlot? selectedSlot)
    {
        if (pendingPickupDefinition == null)
        {
            return;
        }

        if (selectedSlot.HasValue)
        {
            ReplaceWeapon(selectedSlot.Value, pendingPickupDefinition);
            pendingPickupSource?.ConsumeAfterSuccessfulPickup();
        }

        pendingPickupDefinition = null;
        pendingPickupSource = null;
    }

    private void EquipIntoSlot(WeaponSlot slot, WeaponDefinitionSO definition)
    {
        if (definition == null || definition.WeaponPrefab == null)
        {
            Debug.LogError("EquipmentManager: Invalid WeaponDefinitionSO or missing prefab.", this);
            return;
        }

        if (weaponHolder == null)
        {
            Debug.LogError("EquipmentManager: WeaponHolder is not assigned.", this);
            return;
        }

        ClearSlot(slot);

        GameObject weaponObject = Instantiate(definition.WeaponPrefab, weaponHolder);
        weaponObject.transform.localPosition = definition.WeaponPrefab.transform.localPosition;
        weaponObject.transform.localRotation = definition.WeaponPrefab.transform.localRotation;
        weaponObject.transform.localScale = definition.WeaponPrefab.transform.localScale;

        Weapon weaponComponent = weaponObject.GetComponent<Weapon>();
        if (weaponComponent == null)
        {
            Destroy(weaponObject);
            Debug.LogError("EquipmentManager: Equipped prefab does not have a Weapon component!", this);
            return;
        }

        if (slot == WeaponSlot.Main)
        {
            mainWeaponDef = definition;
            mainWeaponInstance = weaponComponent;
        }
        else
        {
            subWeaponDef = definition;
            subWeaponInstance = weaponComponent;
        }

        RefreshWeaponObjectVisibility();
        OnWeaponChanged?.Invoke(slot, definition);
    }

    private void ClearSlot(WeaponSlot slot)
    {
        Weapon weapon = GetWeaponInstance(slot);
        if (weapon != null)
        {
            Destroy(weapon.gameObject);
        }

        if (slot == WeaponSlot.Main)
        {
            mainWeaponDef = null;
            mainWeaponInstance = null;
        }
        else
        {
            subWeaponDef = null;
            subWeaponInstance = null;
        }

        OnWeaponChanged?.Invoke(slot, null);
        RefreshWeaponObjectVisibility();
    }

    private Weapon GetWeaponInstance(WeaponSlot slot)
    {
        return slot == WeaponSlot.Main ? mainWeaponInstance : subWeaponInstance;
    }

    private void SetActiveSlot(WeaponSlot slot, bool forceEvent)
    {
        if (!forceEvent && activeSlot == slot)
        {
            return;
        }

        activeSlot = slot;
        RefreshWeaponObjectVisibility();
        OnActiveSlotChanged?.Invoke(activeSlot);
    }

    private void RefreshWeaponObjectVisibility()
    {
        if (mainWeaponInstance != null)
        {
            mainWeaponInstance.gameObject.SetActive(activeSlot == WeaponSlot.Main);
        }

        if (subWeaponInstance != null)
        {
            subWeaponInstance.gameObject.SetActive(activeSlot == WeaponSlot.Sub);
        }

        if (mainWeaponInstance == null && subWeaponInstance != null)
        {
            subWeaponInstance.gameObject.SetActive(true);
        }

        if (subWeaponInstance == null && mainWeaponInstance != null)
        {
            mainWeaponInstance.gameObject.SetActive(true);
        }
    }
}
