using UnityEngine;

public abstract class PlayerStatModifierSO : ScriptableObject
{
    public abstract void AffectCharacter(GameObject character, float val);
    public string ModifierName { get; set; }
}
