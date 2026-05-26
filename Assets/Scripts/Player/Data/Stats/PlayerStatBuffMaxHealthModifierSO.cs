using UnityEngine;

[CreateAssetMenu(fileName = "NewMaxHealthModifier", menuName = "StatModifier/PlayerStatBuffMaxHealthModifierSO")]
public class PlayerStatBuffMaxHealthModifierSO : PlayerStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerStats playerStats = character.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.BuffMaxHealth(val);
        }
    }
}
