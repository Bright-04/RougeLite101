using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    [SerializeField] private int baseDamage = 1;

    private void OnEnable()
    {
        Debug.Log("DamageSource ENABLED!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"DamageSource hit: {other.gameObject.name}");
        
        if (other.gameObject.TryGetComponent(out SlimeHealth slimeHealth))
        {
            Debug.Log("SlimeHealth component found!");
            
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

            Debug.Log($"Dealing {finalDamage} damage to {other.gameObject.name}");
            slimeHealth.TakeDamage(Mathf.RoundToInt(finalDamage));
        }
        else
        {
            Debug.LogWarning($"No SlimeHealth component on {other.gameObject.name}");
        }
    }
}
