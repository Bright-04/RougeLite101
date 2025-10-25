using UnityEngine;
using RougeLite.Events;
using RougeLite.ObjectPooling;

namespace RougeLite.Combat
{
    /// <summary>
    /// Base projectile class with object pooling support
    /// Handles movement, collision, damage, and pool lifecycle
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] protected float damage = 25f;
        [SerializeField] private bool piercing = false;
        [SerializeField] private int maxTargets = 1;
        [SerializeField] private LayerMask targetLayers = -1;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private GameObject trailEffect;
        [SerializeField] private bool destroyOnHit = true;

        [Header("Physics")]
        [SerializeField] private bool useGravity = false;
        [SerializeField] private float gravityScale = 1f;

        // Components
        private Rigidbody2D rb;
        private Collider2D col;
        private TrailRenderer trail;

        // Runtime data
        private Vector2 direction;
        private float currentLifetime;
        private int targetsHit;
        private GameObject shooter;
        private string projectileType;

        // IPoolable implementation
        public bool IsActive { get; private set; }
        public GameObject GameObject => gameObject;

        // Events
        public System.Action<Projectile, Collision2D> OnProjectileHit;
        public System.Action<Projectile> OnProjectileDestroyed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            trail = GetComponent<TrailRenderer>();
            
            // Set up physics
            rb.gravityScale = useGravity ? gravityScale : 0f;
        }

        private void Update()
        {
            if (!IsActive) return;

            // Update lifetime
            currentLifetime -= Time.deltaTime;
            if (currentLifetime <= 0f)
            {
                DestroyProjectile();
            }
        }

        /// <summary>
        /// Initialize projectile with launch parameters
        /// </summary>
        public void Initialize(Vector2 startPosition, Vector2 launchDirection, float projectileSpeed, 
                              float projectileDamage, GameObject projectileShooter, string type = "")
        {
            transform.position = startPosition;
            direction = launchDirection.normalized;
            speed = projectileSpeed;
            damage = projectileDamage;
            shooter = projectileShooter;
            projectileType = type;
            
            // Set rotation to face direction
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            
            // Apply velocity
            rb.linearVelocity = direction * speed;
            
            // Reset counters
            currentLifetime = lifetime;
            targetsHit = 0;
            
            // Broadcast projectile launched event
            BroadcastProjectileEvent(new ProjectileLaunchedEvent(this, shooter));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.gameObject);
            OnProjectileHit?.Invoke(this, collision);
        }

        private void HandleCollision(GameObject target)
        {
            // Check if target is valid
            if (target == shooter) return; // Don't hit shooter
            if (!IsTargetValid(target)) return;

            // Apply damage
            ApplyDamage(target);
            
            // Call virtual method for derived class-specific hit handling
            HandleHit(target);
            
            // Spawn hit effect
            SpawnHitEffect(transform.position);
            
            // Broadcast hit event
            BroadcastProjectileEvent(new ProjectileHitEvent(this, target, damage));
            
            targetsHit++;
            
            // Check if projectile should be destroyed
            if (destroyOnHit && (!piercing || targetsHit >= maxTargets))
            {
                DestroyProjectile();
            }
        }

        /// <summary>
        /// Virtual method for derived classes to implement custom hit behavior
        /// </summary>
        protected virtual void HandleHit(GameObject target)
        {
            // Base implementation does nothing - derived classes can override
        }

        private bool IsTargetValid(GameObject target)
        {
            // Check layer mask
            int targetLayer = 1 << target.layer;
            return (targetLayers & targetLayer) != 0;
        }

        private void ApplyDamage(GameObject target)
        {
            // Use generic damage interface for loose coupling
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, shooter);
                return;
            }

            Debug.Log($"Projectile hit {target.name} but no damage component found");
        }

        private void SpawnHitEffect(Vector3 position)
        {
            if (hitEffect != null)
            {
                var effect = Instantiate(hitEffect, position, Quaternion.identity);
                Destroy(effect, 2f); // Auto-cleanup effect
            }
        }

        private void DestroyProjectile()
        {
            if (!IsActive) return;
            
            // Broadcast destroyed event
            BroadcastProjectileEvent(new ProjectileDestroyedEvent(this, targetsHit, currentLifetime));
            
            OnProjectileDestroyed?.Invoke(this);
            
            // Return to pool instead of destroying
            ProjectilePoolManager.Instance.ReturnProjectile(this);
        }

        private void BroadcastProjectileEvent(GameEvent eventInstance)
        {
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                eventManager.Broadcast(eventInstance);
            }
        }

        #region IPoolable Implementation

        public void OnGetFromPool()
        {
            IsActive = true;
            gameObject.SetActive(true);
            
            // Reset state
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            targetsHit = 0;
            currentLifetime = lifetime;
            
            // Clear trail if exists
            if (trail != null)
            {
                trail.Clear();
            }
            
            // Enable trail effect if configured
            if (trailEffect != null && !trailEffect.activeInHierarchy)
            {
                trailEffect.SetActive(true);
            }
        }

        public void OnReturnToPool()
        {
            IsActive = false;
            
            // Stop all movement
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            
            // Disable trail effect
            if (trailEffect != null)
            {
                trailEffect.SetActive(false);
            }
            
            // Clear trail
            if (trail != null)
            {
                trail.Clear();
            }
            
            // Reset callbacks
            OnProjectileHit = null;
            OnProjectileDestroyed = null;
            
            gameObject.SetActive(false);
        }

        #endregion

        #region Public Accessors

        public float Speed => speed;
        public float Damage => damage;
        public Vector2 Direction => direction;
        public GameObject Shooter => shooter;
        public string ProjectileType => projectileType;
        public int TargetsHit => targetsHit;
        public float RemainingLifetime => currentLifetime;
        public bool IsPiercing => piercing;

        #endregion

        #region Editor/Debug

        private void OnDrawGizmosSelected()
        {
            // Draw velocity direction
            if (Application.isPlaying && IsActive)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
            }
        }

        #endregion
    }

    // Specialized projectile types moved to separate files:
    // - FireProjectile.cs
    // - IceProjectile.cs
    // - LightningProjectile.cs
}
