using UnityEngine;

[CreateAssetMenu(fileName = "NewCritDamageModifier", menuName = "StatModifier/PlayerStatCritDamageModifierSO")]
public class PlayerStatCritDamageModifierSO : PlayerStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerStats playerStats = character.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.BuffCritDamage(val);
        }
    }
}