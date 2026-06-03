using UnityEngine;

[CreateAssetMenu(fileName = "NewCritChanceModifier", menuName = "StatModifier/PlayerStatCritChanceModifierSO")]
public class PlayerStatCritChanceModifierSO : PlayerStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerStats playerStats = character.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.BuffCritChance(val);
        }
    }
}