using UnityEngine;

/// <summary>
/// Manages the currently active weapon for the player
/// TODO: Implement weapon switching and active weapon management
/// </summary>
public class ActiveWeapon : MonoBehaviour
{
    [SerializeField] private Weapon currentWeapon;
    
    private void Start()
    {
        // Initialize with default weapon if available
        if (currentWeapon == null)
        {
            currentWeapon = GetComponentInChildren<Weapon>();
        }
    }
    
    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }
    
    public void SetActiveWeapon(Weapon weapon)
    {
        currentWeapon = weapon;
    }
}
