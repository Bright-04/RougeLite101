using UnityEngine;

/// <summary>
/// This struct holds momentary data for the current room/frame.
/// It is used by the AI Director to calculate the intensity.
/// </summary>
public struct RuntimeCombatData
{
    public float damageTakenInLastXSeconds;
    public float damageDealtInLastXSeconds;
    public int enemiesKilledRecently;
    public float playerMovementActivity; // How much the player is running/dashing

    public void Reset()
    {
        damageTakenInLastXSeconds = 0;
        damageDealtInLastXSeconds = 0;
        enemiesKilledRecently = 0;
        playerMovementActivity = 0;
    }
}
