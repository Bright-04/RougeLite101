using UnityEngine;

/// <summary>
/// Interface that all enemies must implement to take damage
/// </summary>
public interface IEnemy
{
    void TakeDamage(int damage);
}
