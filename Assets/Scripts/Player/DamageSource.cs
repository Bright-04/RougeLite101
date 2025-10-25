using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RougeLite.Events;
using RougeLite.Enemies;
using RougeLite.Player;

public class DamageSource : EventBehaviour
{
    [SerializeField] private int baseDamage = 1;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent(out SlimeHealth slimeHealth))
        {
            float finalDamage = baseDamage;

            // Get the player's stats for AD and Crit
            PlayerStats stats = PlayerController.Instance?.GetComponent<PlayerStats>();
            if (stats != null)
            {
                finalDamage += stats.attackDamage;

                bool isCrit = stats.TryCrit();
                if (isCrit)
                {
                    finalDamage *= stats.GetCritMultiplier();
                    Debug.Log("Critical hit!");
                }
                
                // Broadcast damage dealt event
                var attackData = new AttackData(
                    attacker: PlayerController.Instance.gameObject,
                    target: other.gameObject,
                    damage: finalDamage,
                    position: transform.position,
                    type: "Melee",
                    critical: isCrit
                );
                
                var damageEvent = new DamageDealtEvent(attackData, gameObject);
                BroadcastEvent(damageEvent);
            }

            slimeHealth.TakeDamage(Mathf.RoundToInt(finalDamage));
        }
    }
}
