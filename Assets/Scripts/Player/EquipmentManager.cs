using UnityEngine;
using UnityEngine.InputSystem;

public class EquipmentManager : MonoBehaviour
{
    public Transform weaponHolder; // empty transform on Player where weapons get attached
    private Weapon equippedWeapon;
    public GameObject startingWeaponPrefab;

    private void Start()
    {
        if (startingWeaponPrefab != null)
        {
            EquipWeapon(startingWeaponPrefab);
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

    private void Update()
    {
        if (equippedWeapon != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            equippedWeapon.Use();
        }
    }
}
