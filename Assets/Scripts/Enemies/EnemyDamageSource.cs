using UnityEngine;
using RougeLite.Events;

public class EnemyDamageSource : EventBehaviour
{
    [SerializeField] private float damageAmount = 3;
    [SerializeField] private float damageCooldown = 0.5f;
    private float damageTimer = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if (damageTimer > 0)
            damageTimer -= Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerStats playerStats) && damageTimer <= 0)
        {
            // Broadcast damage dealt to player event
            var attackData = new AttackData(
                attacker: gameObject,
                target: other.gameObject,
                damage: damageAmount,
                position: transform.position,
                type: "Enemy Contact",
                critical: false
            );
            
            var damageEvent = new DamageDealtEvent(attackData, gameObject);
            BroadcastEvent(damageEvent);
            
            playerStats.TakeDamage(damageAmount);
            damageTimer = damageCooldown;
        }
    }

}
