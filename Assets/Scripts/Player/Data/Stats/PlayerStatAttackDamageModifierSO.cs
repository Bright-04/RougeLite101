using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackDamageModifier", menuName = "StatModifier/PlayerStatAttackDamageModifierSO")]
public class PlayerStatAttackDamageModifierSO : PlayerStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerStats playerStats = character.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.BuffAttackDamage(val);
        }
    }
}