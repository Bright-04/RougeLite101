using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public float cooldown = 0.5f;
    protected float nextUseTime = 0f;
    protected WeaponDefinitionSO weaponDefinition;

    public virtual void Initialize(WeaponDefinitionSO definition)
    {
        weaponDefinition = definition;
        if (definition != null)
        {
            cooldown = definition.Cooldown;
        }
    }

    protected Quaternion GetLocalRotationOffset()
    {
        return weaponDefinition != null ? Quaternion.Euler(weaponDefinition.LocalRotationOffset) : Quaternion.identity;
    }

    public abstract void Use();
}
