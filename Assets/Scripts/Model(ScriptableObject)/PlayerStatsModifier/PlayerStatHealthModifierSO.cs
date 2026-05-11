using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHealthModifier", menuName = "StatModifier/PlayerStatHealthModifierSO")]
public class PlayerStatHealthModifierSO : PlayerStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerStats playerStats = character.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.HealthRestore(val);
        }
            
    }
}
