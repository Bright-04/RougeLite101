using UnityEngine;

/// <summary>
/// Interface for any enemy that can take damage
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}
