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
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Enable();
        playerControls.Combat.Attack.started += OnAttackPerformed;
    }

    private void OnDisable()
    {
        playerControls.Combat.Attack.started -= OnAttackPerformed;
        playerControls.Disable();
    }

    private void Start()
    {
        if (startingWeaponPrefab != null)
        {
            EquipWeapon(startingWeaponPrefab);
        }
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
