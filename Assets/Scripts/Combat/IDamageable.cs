using UnityEngine;

namespace RougeLite.Combat
{
    /// <summary>
    /// Interface for objects that can take damage from projectiles or other sources.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage, GameObject source = null);
    }
}

