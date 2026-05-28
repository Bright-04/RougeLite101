using UnityEngine;

[CreateAssetMenu(fileName = "NewDefenseModifier", menuName = "StatModifier/PlayerStatDefenseModifierSO")]
public class PlayerStatDefenseModifierSO : PlayerStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerStats playerStats = character.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.BuffDefense(val);
        }
    }
}