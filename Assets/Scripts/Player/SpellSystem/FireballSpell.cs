using UnityEngine;
using RougeLite.Events;

public class FireballSpell : EventBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float damage = 10f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        Debug.Log("Fireball instantiated at " + transform.position);
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out SlimeHealth slimeHealth))
        {
            // Broadcast spell damage event
            var attackData = new AttackData(
                attacker: PlayerController.Instance != null ? PlayerController.Instance.gameObject : gameObject,
                target: other.gameObject,
                damage: damage,
                position: transform.position,
                type: "Fireball",
                critical: false
            );
            
            var damageEvent = new DamageDealtEvent(attackData, gameObject);
            BroadcastEvent(damageEvent);
            
            slimeHealth.TakeDamage((int)damage);
            Destroy(gameObject);
        }
    }
}
