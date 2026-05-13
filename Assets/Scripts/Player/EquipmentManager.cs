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
    [SerializeField] private WeaponPickupModalUI weaponPickupModalUI;

    [Header("Aim Hand Anchoring")]
    [SerializeField] private Vector3 rightHandLocalOffset = new Vector3(0.08f, -0.01f, 0f);
    [SerializeField] private Vector3 leftHandLocalOffset = new Vector3(-0.08f, -0.01f, 0f);
    [SerializeField] private float handSwitchDeadZoneX = 0.02f;

    [Header("Weapon Draw Order")]
    [SerializeField] private int frontOrderOffset = 1;
    [SerializeField] private int backOrderOffset = -1;

    [Header("Data")]
    [SerializeField] private WeaponRegistry weaponRegistry;
    [SerializeField] private WeaponDefinitionSO startingMainWeapon;
    [SerializeField] private WeaponDefinitionSO startingSubWeapon;

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
    private PlayerMovement playerMovement;
    private SpriteRenderer playerSpriteRenderer;

    private bool isUsingLeftHand;
    private int currentWeaponSortingOrder = int.MinValue;

    public event Action<WeaponSlot, WeaponDefinitionSO> OnWeaponChanged;
    public event Action<WeaponSlot> OnActiveSlotChanged;

    private Transform WeaponMount => aimPivot != null ? aimPivot : weaponHolder;

    private void Awake()
    {
        if (weaponRegistry != null)
        {
            weaponRegistry.Initialize();
        }

        playerMovement = GetComponent<PlayerMovement>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();

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

        if (mainWeaponDef == null && subWeaponDef == null)
        {
            if (startingMainWeapon != null) EquipIntoSlot(WeaponSlot.Main, startingMainWeapon);
            if (startingSubWeapon != null) EquipIntoSlot(WeaponSlot.Sub, startingSubWeapon);
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

        Transform mount = WeaponMount;
        if (mount == null)
        {
            Debug.LogError("EquipmentManager: Weapon mount is not assigned.", this);
            return;
        }

        ClearSlot(slot);

        // CREATE an Offset Container to protect position changes from the Animator!
        GameObject offsetContainer = new GameObject("OffsetContainer_" + definition.WeaponId);
        offsetContainer.transform.SetParent(mount, false);
        offsetContainer.transform.localPosition = definition.LocalPositionOffset;
        offsetContainer.transform.localRotation = Quaternion.Euler(definition.LocalRotationOffset);
        offsetContainer.transform.localScale = Vector3.one;

        GameObject weaponObject = Instantiate(definition.WeaponPrefab, offsetContainer.transform);
        weaponObject.transform.localPosition = definition.WeaponPrefab.transform.localPosition;
        weaponObject.transform.localRotation = definition.WeaponPrefab.transform.localRotation;
        weaponObject.transform.localScale = definition.WeaponPrefab.transform.localScale;

        Weapon weaponComponent = weaponObject.GetComponent<Weapon>();
        if (weaponComponent == null)
        {
            Destroy(offsetContainer); // Destroy wrapper if weapon fails
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
        UpdateWeaponSortingOrder(aimDirection.y);
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

    private void UpdateWeaponSortingOrder(float aimY)
    {
        int baseOrder = playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 0;
        int targetOrder = baseOrder + (aimY > 0f ? backOrderOffset : frontOrderOffset);

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

        SpriteRenderer baseRenderer = weapon.GetComponent<SpriteRenderer>();
        if (baseRenderer == null)
        {
            baseRenderer = renderers[0];
        }

        int delta = targetOrder - baseRenderer.sortingOrder;
        if (delta == 0)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder += delta;
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
            // Destroy the offset container (parent) instead of just the weapon!
            if (weapon.transform.parent != null && weapon.transform.parent.name.StartsWith("OffsetContainer_"))
            {
                Destroy(weapon.transform.parent.gameObject);
            }
            else
            {
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

        UpdatePlayerAnimationProfile();
    }

    private void UpdatePlayerAnimationProfile()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            return;
        }

        Weapon activeWeapon = GetWeaponInstance(activeSlot);
        if (activeWeapon == null)
        {
            activeWeapon = mainWeaponInstance != null ? mainWeaponInstance : subWeaponInstance;
        }

        if (activeWeapon is Sword)
        {
            playerMovement.SetAnimationProfile(PlayerMovement.AnimationProfile.Sword);
        }
        else if (activeWeapon is Bow)
        {
            playerMovement.SetAnimationProfile(PlayerMovement.AnimationProfile.Bow);
        }
        else
        {
            playerMovement.SetAnimationProfile(PlayerMovement.AnimationProfile.Default);
        }
    }
}
