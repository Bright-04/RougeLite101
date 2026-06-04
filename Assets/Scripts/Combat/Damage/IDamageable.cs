using UnityEngine;

/// <summary>
/// Interface for any enemy that can take damage
/// </summary>
namespace RougeLite.Combat.Damage
{
    public interface IDamageable
    {
        void TakeDamage(int damage);
    }
}
