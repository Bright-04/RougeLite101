using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    [SerializeField] private int baseDamage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent(out SlimeHealth slimeHealth))
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

            slimeHealth.TakeDamage(Mathf.RoundToInt(finalDamage));
        }
    }
}
