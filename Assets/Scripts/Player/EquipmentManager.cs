using UnityEngine;
using UnityEngine.InputSystem;

public class EquipmentManager : MonoBehaviour
{
    public Transform weaponHolder; // empty transform on Player where weapons get attached
    private Weapon equippedWeapon;
    public GameObject startingWeaponPrefab;

    private PlayerControls playerControls;

    private void Awake()
    {
        // Chỉ khởi tạo components local ở đây
    }

    private void Start()
    {
        // Lấy InputManager.Instance ở Start
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }

        playerControls = InputManager.Instance.Controls;
        playerControls.Combat.Attack.started += OnAttackPerformed;

        // Equip starting weapon
        if (startingWeaponPrefab != null)
        {
            EquipWeapon(startingWeaponPrefab);
        }
    }

    //private void OnEnable()
    //{
    //    if (playerControls != null)
    //    {
    //        playerControls.Combat.Attack.started += OnAttackPerformed;
    //    }

    //}

    //private void OnDisable()
    //{
    //    if (playerControls != null)
    //    {
    //        playerControls.Combat.Attack.started -= OnAttackPerformed;
    //    }

    //}

    private void OnDestroy()
    {
        if (playerControls != null)
            playerControls.Combat.Attack.started -= OnAttackPerformed;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.Use();
        }
    }


    public void EquipWeapon(GameObject weaponPrefab)
    {
        // Destroy old weapon if exists
        if (equippedWeapon != null)
        {
            Destroy(equippedWeapon.gameObject);
        }

        // Instantiate new weapon under the weapon holder
        GameObject weaponGO = Instantiate(weaponPrefab, weaponHolder.position, Quaternion.identity, weaponHolder);
        equippedWeapon = weaponGO.GetComponent<Weapon>();

        if (equippedWeapon == null)
        {
            Debug.LogError("Equipped prefab does not have a Weapon component!");
        }
    }
}
