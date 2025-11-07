using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    [SerializeField] private int baseDamage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to get any component that implements IEnemy interface
        IEnemy enemy = other.gameObject.GetComponent<IEnemy>();
        
        if (enemy != null)
        {
            float finalDamage = baseDamage;

            // Get the player's stats for AD and Crit
            PlayerStats stats = PlayerController.Instance.GetComponent<PlayerStats>();
            if (stats != null)
            {
                finalDamage += stats.attackDamage;

                if (stats.TryCrit())
                {
                    finalDamage *= stats.GetCritMultiplier();
                    Debug.Log("Critical hit!");
                }
            }

            enemy.TakeDamage(Mathf.RoundToInt(finalDamage));
        }
    }
}
