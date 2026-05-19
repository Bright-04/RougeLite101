using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("Testing/WeaponTester")]
public class WeaponTester : MonoBehaviour
{
    [SerializeField] private WeaponRegistry weaponRegistry;
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private float autoCycleInterval = 3f;
    [SerializeField] private GameObject testDummyPrefab;
    [Header("Input Actions (optional)")]
    [SerializeField] private InputActionReference nextActionRef;
    [SerializeField] private InputActionReference prevActionRef;
    [SerializeField] private InputActionReference toggleAutoActionRef;
    [SerializeField] private InputActionReference useActionRef;
    [SerializeField] private InputActionReference spawnDummyActionRef;

    private List<WeaponDefinitionSO> weapons = new List<WeaponDefinitionSO>();
    private int index = 0;
    private float timer = 0f;
    private bool autoCycle = false;

    private void Start()
    {
        if (weaponRegistry == null)
            weaponRegistry = ScriptableObject.CreateInstance<WeaponRegistry>();

        // Try find existing registry in scene or project by asking EquipmentManager
        if (weaponRegistry == null && equipmentManager != null)
        {
            // nothing to do; EquipmentManager holds a registry reference
        }

        if (weaponRegistry == null)
        {
            // Fallback: try to find any WeaponRegistry asset via Resources (best-effort)
            WeaponRegistry[] regs = Resources.FindObjectsOfTypeAll<WeaponRegistry>();
            if (regs != null && regs.Length > 0) weaponRegistry = regs[0];
        }

        if (weaponRegistry == null)
        {
            Debug.LogWarning("WeaponTester: No WeaponRegistry found. Cannot auto-populate weapon list.");
            return;
        }

        // Try to populate weapons via reflection of private field; if fails, require manual assignment
        var soType = weaponRegistry.GetType();
        var listField = soType.GetField("allWeapons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (listField != null)
        {
            var val = listField.GetValue(weaponRegistry) as System.Collections.IEnumerable;
            if (val != null)
            {
                foreach (var item in val)
                {
                    if (item is WeaponDefinitionSO w) weapons.Add(w);
                }
            }
        }

        // If still empty, warn
        if (weapons.Count == 0)
        {
            Debug.LogWarning("WeaponTester: Weapon list empty. Assign weapons manually or ensure WeaponRegistry is populated.");
        }

        if (equipmentManager == null)
        {
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
            if (equipmentManager == null)
            {
                Debug.LogWarning("WeaponTester: EquipmentManager not found in scene.");
            }
        }

        timer = autoCycleInterval;
    }

    private void OnEnable()
    {
        RegisterAction(nextActionRef, () => NextWeapon());
        RegisterAction(prevActionRef, () => PrevWeapon());
        RegisterAction(toggleAutoActionRef, () => ToggleAutoCycle());
        RegisterAction(useActionRef, () => UseCurrentWeapon());
        RegisterAction(spawnDummyActionRef, () => SpawnDummy());
    }

    private void OnDisable()
    {
        UnregisterAction(nextActionRef, () => NextWeapon());
        UnregisterAction(prevActionRef, () => PrevWeapon());
        UnregisterAction(toggleAutoActionRef, () => ToggleAutoCycle());
        UnregisterAction(useActionRef, () => UseCurrentWeapon());
        UnregisterAction(spawnDummyActionRef, () => SpawnDummy());
    }

    private void RegisterAction(InputActionReference reference, System.Action callback)
    {
        if (reference == null || reference.action == null) return;
        reference.action.performed += ctx => callback();
        reference.action.Enable();
    }

    private void UnregisterAction(InputActionReference reference, System.Action callback)
    {
        if (reference == null || reference.action == null) return;
        // Best-effort removal: remove all performed handlers (cannot remove lambda directly)
        reference.action.performed -= ctx => callback();
        reference.action.Disable();
    }

    private void Update()
    {
        if (weapons.Count == 0 || equipmentManager == null) return;

        if (autoCycle)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                NextWeapon();
                timer = autoCycleInterval;
            }
        }

        // If Input System actions are not assigned, provide keyboard fallbacks.
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f5Key.wasPressedThisFrame) NextWeapon();      // F5: Next weapon
            if (kb.f6Key.wasPressedThisFrame) PrevWeapon();      // F6: Previous weapon
            if (kb.f7Key.wasPressedThisFrame) ToggleAutoCycle(); // F7: Toggle auto-cycle
            if (kb.f8Key.wasPressedThisFrame) UseCurrentWeapon(); // F8: Use current weapon
            if (kb.f9Key.wasPressedThisFrame) SpawnDummy();      // F9: Spawn dummy
            return;
        }

        // Legacy fallback if Input System not present/initialized
        #if UNITY_OLD_INPUT
        if (Input.GetKeyDown(KeyCode.F5)) NextWeapon();
        if (Input.GetKeyDown(KeyCode.F6)) PrevWeapon();
        if (Input.GetKeyDown(KeyCode.F7)) ToggleAutoCycle();
        if (Input.GetKeyDown(KeyCode.F8)) UseCurrentWeapon();
        if (Input.GetKeyDown(KeyCode.F9)) SpawnDummy();
        #endif
    }

    public void NextWeapon()
    {
        if (weapons.Count == 0) return;
        index = (index + 1) % weapons.Count;
        EquipCurrent();
    }

    public void PrevWeapon()
    {
        if (weapons.Count == 0) return;
        index = (index - 1 + weapons.Count) % weapons.Count;
        EquipCurrent();
    }

    public void EquipCurrent()
    {
        if (weapons.Count == 0 || equipmentManager == null) return;
        var def = weapons[index];
        if (def == null) return;
        equipmentManager.ReplaceWeapon(EquipmentManager.WeaponSlot.Main, def);
        Debug.Log($"WeaponTester: Equipped [{index}] {def.name} (id={def.WeaponId})");
    }

    public void UseCurrentWeapon()
    {
        var em = equipmentManager;
        if (em == null) return;
        em.GetType();
        // Trigger attack on active slot
        var slot = em.GetActiveSlot();
        var inst = em.GetType().GetMethod("GetWeaponInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (inst != null)
        {
            var weaponObj = inst.Invoke(em, new object[] { slot }) as Weapon;
            if (weaponObj != null)
            {
                weaponObj.Use();
                Debug.Log("WeaponTester: Used current weapon.");
                return;
            }
        }

        Debug.LogWarning("WeaponTester: Could not invoke weapon Use().");
    }

    public void ToggleAutoCycle()
    {
        autoCycle = !autoCycle;
        Debug.Log("WeaponTester: AutoCycle " + (autoCycle ? "ON" : "OFF"));
    }

    public void SpawnDummy()
    {
        if (testDummyPrefab == null)
        {
            Debug.LogWarning("WeaponTester: testDummyPrefab not assigned.");
            return;
        }

        Instantiate(testDummyPrefab, transform.position + transform.right * 2f, Quaternion.identity);
    }
}
