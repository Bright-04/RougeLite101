using UnityEngine;

public abstract class PlayerStatModifierSO : ScriptableObject
{
    public abstract void AffectCharacter(GameObject character, float val);
    [SerializeField] private string modifierName;

    public string ModifierName => modifierName;
}
