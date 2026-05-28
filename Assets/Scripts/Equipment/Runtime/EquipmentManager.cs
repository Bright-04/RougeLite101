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
    [SerializeField] private Transform aimPivot;
    [SerializeField] private WeaponController weaponController;

    [Header("Aim Hand Anchoring")]
    [SerializeField] private Vector3 rightHandLocalOffset = new Vector3(0.08f, -0.01f, 0f);
    [SerializeField] private Vector3 leftHandLocalOffset = new Vector3(-0.08f, -0.01f, 0f);
    [SerializeField] private float handSwitchDeadZoneX = 0.02f;

    [Header("Weapon Draw Order")]
    [SerializeField] private int frontOrderOffset = 1;

    [Header("Data")]
    [SerializeField] private WeaponRegistry weaponRegistry;
    [SerializeField] private WeaponDefinitionSO startingMainWeapon;
    [SerializeField] private WeaponDefinitionSO startingSubWeapon;
    [SerializeField] private InventoryController inventoryController;

    private WeaponDefinitionSO mainWeaponDef;
    private WeaponDefinitionSO subWeaponDef;
    private Weapon mainWeaponInstance;
    private Weapon subWeaponInstance;
    private WeaponSlot activeSlot = WeaponSlot.Main;
    private bool testingWeaponOverrideActive;
    private UnityEngine.Object testingWeaponOverrideOwner;

    private PlayerControls playerControls;
    private InputAction attackAction;
    private InputAction swapWeaponAction;
    private PlayerMovement playerMovement;
    private SpriteRenderer playerSpriteRenderer;

    private bool isUsingLeftHand;
    private int currentWeaponSortingOrder = int.MinValue;

    public event Action<WeaponSlot, WeaponDefinitionSO> OnWeaponChanged;
    public event Action<WeaponSlot> OnActiveSlotChanged;
    public bool TestingWeaponOverrideActive => testingWeaponOverrideActive;

    private Transform WeaponMount => aimPivot != null ? aimPivot : weaponHolder;

    // EquipmentManager remains the serialized MonoBehaviour entry point.
    // Pure loadout decisions are delegated to WeaponLoadoutRules; this class still owns Unity object orchestration.

    private void Awake()
    {
        if (weaponRegistry != null)
        {
            weaponRegistry.Initialize();
        }

        // read inspector dead-zone to avoid unused-field warning
        var _deadZoneUse = handSwitchDeadZoneX;

        playerMovement = GetComponent<PlayerMovement>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }
        if (weaponController == null)
        {
            weaponController = GetComponent<WeaponController>();
            if (weaponController == null)
            {
                weaponController = gameObject.AddComponent<WeaponController>();
            }
        }

        if (playerSpriteRenderer != null)
        {
            if ((playerSpriteRenderer.sortingLayerName == "Default" || string.IsNullOrEmpty(playerSpriteRenderer.sortingLayerName)) && playerSpriteRenderer.sortingOrder < 10)
            {
                playerSpriteRenderer.sortingOrder = 10; // Ensure player and weapons stay above map floor
            }
        }
    }

    private void Start()
    {
        EnsureWeaponMount();
        UpdateVisualMountFromAim();

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

        if (mainWeaponDef == null && subWeaponDef == null)
        {
            if (startingMainWeapon != null) EquipIntoSlot(WeaponSlot.Main, startingMainWeapon);
            if (startingSubWeapon != null) EquipIntoSlot(WeaponSlot.Sub, startingSubWeapon);
        }

        SetActiveSlot(activeSlot, true);
    }

    private void Update()
    {
        UpdateVisualMountFromAim();
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

    public void ReplaceWeapon(WeaponSlot targetSlot, WeaponDefinitionSO newWeaponDef)
    {
        ReplaceWeaponInternal(targetSlot, newWeaponDef, bypassTestingOverride: false);
    }

    public void ReplaceWeaponAndActivate(WeaponSlot targetSlot, WeaponDefinitionSO newWeaponDef)
    {
        ReplaceWeaponAndActivateInternal(targetSlot, newWeaponDef, bypassTestingOverride: false);
    }

    public void ReplaceWeaponAndActivateForTesting(WeaponSlot targetSlot, WeaponDefinitionSO newWeaponDef, UnityEngine.Object overrideOwner)
    {
        SetTestingWeaponOverride(true, overrideOwner);
        ReplaceWeaponAndActivateInternal(targetSlot, newWeaponDef, bypassTestingOverride: true);
    }

    public bool EquipWeapon(WeaponDefinitionSO newWeaponDef)
    {
        if (!WeaponLoadoutRules.CanAcceptPickup(newWeaponDef, testingWeaponOverrideActive))
        {
            return false;
        }

        if (WeaponLoadoutRules.CanAutoEquipMain(mainWeaponDef))
        {
            EquipIntoSlot(WeaponSlot.Main, newWeaponDef);
            SetActiveSlot(WeaponSlot.Main, true);
            return true;
        }

        if (WeaponLoadoutRules.CanAutoEquipSub(subWeaponDef))
        {
            EquipIntoSlot(WeaponSlot.Sub, newWeaponDef);
            return true;
        }

        WeaponDefinitionSO previousWeapon = GetWeaponDefinition(activeSlot);
        if (!TryReturnWeaponToInventory(previousWeapon))
        {
            return false;
        }

        ReplaceWeapon(activeSlot, newWeaponDef);
        SetActiveSlot(activeSlot, true);
        return true;
    }

    public void RefreshEquippedWeapons(WeaponDefinitionSO changedDefinition)
    {
        if (changedDefinition == null)
        {
            return;
        }

        bool refreshedMain = RefreshEquippedWeapon(WeaponSlot.Main, changedDefinition);
        bool refreshedSub = RefreshEquippedWeapon(WeaponSlot.Sub, changedDefinition);
        bool refreshedActiveSlot = activeSlot == WeaponSlot.Main ? refreshedMain : refreshedSub;

        if (!refreshedMain && !refreshedSub)
        {
            return;
        }

        RefreshWeaponObjectVisibility();

        if (refreshedActiveSlot)
        {
            Weapon activeWeapon = GetWeaponInstance(activeSlot);
            WeaponDefinitionSO activeDefinition = GetWeaponDefinition(activeSlot);
            if (activeWeapon != null && activeDefinition != null)
            {
                weaponController?.SetCurrentWeapon(activeWeapon, activeDefinition);
            }
        }
    }

    public void SetTestingWeaponOverride(bool enabled, UnityEngine.Object overrideOwner = null)
    {
        if (enabled)
        {
            testingWeaponOverrideActive = true;
            testingWeaponOverrideOwner = overrideOwner;
            return;
        }

        if (overrideOwner != null && testingWeaponOverrideOwner != null && overrideOwner != testingWeaponOverrideOwner)
        {
            return;
        }

        testingWeaponOverrideActive = false;
        testingWeaponOverrideOwner = null;
    }

    public void ClearTestingWeaponOverride(UnityEngine.Object overrideOwner = null)
    {
        SetTestingWeaponOverride(false, overrideOwner);
    }

    private void ReplaceWeaponInternal(WeaponSlot targetSlot, WeaponDefinitionSO newWeaponDef, bool bypassTestingOverride)
    {
        if (!WeaponLoadoutRules.CanAcceptPickup(newWeaponDef, testingWeaponOverrideActive) && !bypassTestingOverride)
        {
            return;
        }

        if (newWeaponDef == null)
        {
            return;
        }

        if (testingWeaponOverrideActive && !bypassTestingOverride)
        {
            return;
        }

        EquipIntoSlot(targetSlot, newWeaponDef);

        if (GetWeaponInstance(activeSlot) == null)
        {
            SetActiveSlot(WeaponLoadoutRules.ResolveFallbackActiveSlot(mainWeaponDef, subWeaponDef), true);
        }
    }

    private void ReplaceWeaponAndActivateInternal(WeaponSlot targetSlot, WeaponDefinitionSO newWeaponDef, bool bypassTestingOverride)
    {
        ReplaceWeaponInternal(targetSlot, newWeaponDef, bypassTestingOverride);

        if (GetWeaponInstance(targetSlot) != null)
        {
            SetActiveSlot(targetSlot, true);
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

        if (mainWeaponDef == null && subWeaponDef == null)
        {
            if (startingMainWeapon != null) EquipIntoSlot(WeaponSlot.Main, startingMainWeapon);
            if (startingSubWeapon != null) EquipIntoSlot(WeaponSlot.Sub, startingSubWeapon);
        }

        WeaponSlot finalActiveSlot = WeaponLoadoutRules.ResolveLoadedActiveSlot(loadedActiveSlot, mainWeaponDef, subWeaponDef);

        SetActiveSlot(finalActiveSlot, true);
    }

    public string GetMainWeaponId()
    {
        return WeaponLoadoutRules.GetStableWeaponId(mainWeaponDef);
    }

    public string GetSubWeaponId()
    {
        return WeaponLoadoutRules.GetStableWeaponId(subWeaponDef);
    }

    public WeaponSlot GetActiveSlot()
    {
        return activeSlot;
    }

    public WeaponDefinitionSO GetWeaponDefinition(WeaponSlot slot)
    {
        return slot == WeaponSlot.Main ? mainWeaponDef : subWeaponDef;
    }

    public bool UnequipWeapon(WeaponSlot slot)
    {
        WeaponDefinitionSO removedWeapon = GetWeaponDefinition(slot);
        if (removedWeapon == null)
        {
            return false;
        }

        if (!TryReturnWeaponToInventory(removedWeapon))
        {
            return false;
        }

        bool removedActiveSlot = activeSlot == slot;
        ClearSlot(slot);

        if (removedActiveSlot)
        {
            WeaponSlot nextActiveSlot = mainWeaponDef != null
                ? WeaponSlot.Main
                : subWeaponDef != null
                    ? WeaponSlot.Sub
                    : slot;
            SetActiveSlot(nextActiveSlot, true);
        }

        return true;
    }

    private void EquipIntoSlot(WeaponSlot slot, WeaponDefinitionSO definition)
    {
        if (definition == null || definition.WeaponPrefab == null)
        {
            Debug.LogError("EquipmentManager: Invalid WeaponDefinitionSO or missing prefab.", this);
            return;
        }

        if (weaponController == null)
        {
            Debug.LogError("EquipmentManager: WeaponController is not available.", this);
            return;
        }

        // Unity orchestration stays here: destroy the old instance, instantiate the prefab, and wire the runtime component.
        ClearSlot(slot);

        Transform visualRoot = weaponController.CreateWeaponVisualRoot(definition.WeaponId);

        GameObject weaponObject = Instantiate(definition.WeaponPrefab, visualRoot);
        ResetWeaponVisualTransform(weaponObject.transform);

        Weapon weaponComponent = weaponObject.GetComponent<Weapon>();
        if (weaponComponent == null)
        {
            Destroy(visualRoot.gameObject); // Destroy wrapper if weapon fails
            Debug.LogError("EquipmentManager: Equipped prefab does not have a Weapon component!", this);
            return;
        }

        weaponComponent.Initialize(definition);

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
        if (activeSlot == slot)
        {
            weaponController.SetCurrentWeapon(weaponComponent, definition);
        }
        if (currentWeaponSortingOrder == int.MinValue)
        {
            currentWeaponSortingOrder = (playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 0) + frontOrderOffset;
        }
        ApplySortingToWeapon(weaponComponent, currentWeaponSortingOrder);
        OnWeaponChanged?.Invoke(slot, definition);
    }

    private void UpdateVisualMountFromAim()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                return;
            }
        }

        Vector2 aimDirection = playerMovement.LastAimDirection;
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        UpdateHandAnchor(aimDirection.x);
        UpdateWeaponSortingOrder();
    }

    private void UpdateHandAnchor(float aimX)
    {
        if (aimPivot == null)
        {
            return;
        }

        // Rely on central stable body facing direction to prevent AimPivot vibration
        bool shouldUseLeftHand = PlayerMovement.Instance != null ? PlayerMovement.Instance.FacingLeft : isUsingLeftHand;

        Vector3 targetOffset = shouldUseLeftHand ? leftHandLocalOffset : rightHandLocalOffset;
        if (aimPivot.localPosition != targetOffset)
        {
            aimPivot.localPosition = targetOffset;
        }

        isUsingLeftHand = shouldUseLeftHand;
    }

    private void UpdateWeaponSortingOrder()
    {
        int baseOrder = playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 0;
        int targetOrder = baseOrder + frontOrderOffset;

        if (targetOrder == currentWeaponSortingOrder)
        {
            return;
        }

        currentWeaponSortingOrder = targetOrder;
        ApplySortingToWeapon(mainWeaponInstance, targetOrder);
        ApplySortingToWeapon(subWeaponInstance, targetOrder);
    }

    private void ApplySortingToWeapon(Weapon weapon, int targetOrder)
    {
        if (weapon == null || targetOrder == int.MinValue)
        {
            return;
        }

        SpriteRenderer[] renderers = weapon.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        int sortingLayerId = playerSpriteRenderer != null ? playerSpriteRenderer.sortingLayerID : renderers[0].sortingLayerID;
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerID = sortingLayerId;
            renderers[i].sortingOrder = targetOrder;
        }
    }

    private void EnsureWeaponMount()
    {
        if (aimPivot != null)
        {
            return;
        }

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null && playerMovement.AimPivot != null)
        {
            aimPivot = playerMovement.AimPivot;
            return;
        }

        if (weaponHolder == null)
        {
            return;
        }

        Transform existingPivot = weaponHolder.Find("AimPivot");
        if (existingPivot != null)
        {
            aimPivot = existingPivot;
            return;
        }

        GameObject pivotObject = new GameObject("AimPivot");
        pivotObject.transform.SetParent(weaponHolder, false);
        pivotObject.transform.localPosition = Vector3.zero;
        pivotObject.transform.localRotation = Quaternion.identity;
        pivotObject.transform.localScale = Vector3.one;
        aimPivot = pivotObject.transform;
    }

    private void ClearSlot(WeaponSlot slot)
    {
        Weapon weapon = GetWeaponInstance(slot);
        if (weapon != null)
        {
            // Unity orchestration stays here: clear the runtime weapon instance and its visual root.
            weaponController?.ClearCurrentWeapon(weapon);

            // Destroy the visual root instead of just the weapon.
            if (weapon.transform.parent != null && weapon.transform.parent.name.StartsWith("CurrentWeaponVisual_"))
            {
                weapon.transform.parent.gameObject.SetActive(false);
                Destroy(weapon.transform.parent.gameObject);
            }
            else
            {
                weapon.gameObject.SetActive(false);
                Destroy(weapon.gameObject);
            }
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

    private bool RefreshEquippedWeapon(WeaponSlot slot, WeaponDefinitionSO changedDefinition)
    {
        WeaponDefinitionSO currentDefinition = GetWeaponDefinition(slot);
        Weapon currentWeapon = GetWeaponInstance(slot);

        if (!WeaponLoadoutRules.ShouldRefreshEquippedWeapon(currentDefinition, changedDefinition, currentWeapon))
        {
            return false;
        }

        currentWeapon.Initialize(currentDefinition);
        OnWeaponChanged?.Invoke(slot, currentDefinition);

        if (slot == activeSlot && weaponController != null)
        {
            weaponController.SetCurrentWeapon(currentWeapon, currentDefinition);
        }

        return true;
    }

    private void SetActiveSlot(WeaponSlot slot, bool forceEvent)
    {
        if (!forceEvent && activeSlot == slot)
        {
            return;
        }

        activeSlot = slot;
        RefreshWeaponObjectVisibility();
        Weapon activeWeapon = GetWeaponInstance(activeSlot);
        WeaponDefinitionSO activeDefinition = GetWeaponDefinition(activeSlot);
        if (weaponController != null)
        {
            weaponController.SetCurrentWeapon(activeWeapon, activeDefinition);
        }
        OnActiveSlotChanged?.Invoke(activeSlot);
    }

    private void RefreshWeaponObjectVisibility()
    {
        if (mainWeaponInstance != null)
        {
            SetWeaponVisualActive(mainWeaponInstance, activeSlot == WeaponSlot.Main);
        }

        if (subWeaponInstance != null)
        {
            SetWeaponVisualActive(subWeaponInstance, activeSlot == WeaponSlot.Sub);
        }

        if (mainWeaponInstance == null && subWeaponInstance != null)
        {
            SetWeaponVisualActive(subWeaponInstance, true);
        }

        if (subWeaponInstance == null && mainWeaponInstance != null)
        {
            SetWeaponVisualActive(mainWeaponInstance, true);
        }
    }

    private static void SetWeaponVisualActive(Weapon weapon, bool active)
    {
        if (weapon == null)
        {
            return;
        }

        Transform visualRoot = weapon.transform.parent != null && weapon.transform.parent.name.StartsWith("CurrentWeaponVisual_")
            ? weapon.transform.parent
            : weapon.transform;
        visualRoot.gameObject.SetActive(active);
    }

    private static void ResetWeaponVisualTransform(Transform target)
    {
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
    }

    private bool TryReturnWeaponToInventory(WeaponDefinitionSO weapon)
    {
        if (weapon == null)
        {
            return true;
        }

        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }

        InventorySO targetInventory = inventoryController != null ? inventoryController.CurrentInventoryData : null;
        if (targetInventory == null)
        {
            return false;
        }

        return targetInventory.AddItem(weapon, 1) == 0;
    }
}
