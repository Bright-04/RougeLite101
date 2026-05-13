using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewManaModifier", menuName = "StatModifier/PlayerStatManaModifierSO")]
public class PlayerStatManaModifierSO : PlayerStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerStats playerStats = character.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.ManaRestore(val);
        }

    }
}
