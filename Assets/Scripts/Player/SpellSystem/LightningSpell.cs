using UnityEngine;
using RougeLite.Events;

public class LightningSpell : EventBehaviour
{
    [SerializeField] private float damage = 20f;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float duration = 0.5f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // Find enemies in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out SlimeHealth slimeHealth))
            {
                // Broadcast spell damage event for each enemy hit
                var attackData = new AttackData(
                    attacker: PlayerController.Instance != null ? PlayerController.Instance.gameObject : gameObject,
                    target: hit.gameObject,
                    damage: damage,
                    position: transform.position,
                    type: "Lightning",
                    critical: false
                );
                
                var damageEvent = new DamageDealtEvent(attackData, gameObject);
                BroadcastEvent(damageEvent);
                
                slimeHealth.TakeDamage((int)damage);
            }
        }

        Destroy(gameObject, duration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
