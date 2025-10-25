using UnityEngine;
using RougeLite.Events;
using RougeLite.Player;
using RougeLite.Enemies;

public class FireballSpell : EventBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float damage = 10f;
    
    private Vector2 direction = Vector2.right; // Default direction

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
        // Move in world space using the set direction
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }
    
    /// <summary>
    /// Set the direction for the fireball to travel
    /// </summary>
    /// <param name="dir">Normalized direction vector</param>
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
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
