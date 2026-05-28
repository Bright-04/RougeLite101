using UnityEngine;

[CreateAssetMenu(fileName = "NewAbilityPowerModifier", menuName = "StatModifier/PlayerStatAbilityPowerModifierSO")]
public class PlayerStatAbilityPowerModifierSO : PlayerStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerStats playerStats = character.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.BuffAbilityPower(val);
        }
    }
}