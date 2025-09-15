using UnityEngine;
using RougeLite.Combat;

namespace RougeLite.Examples
{
    /// <summary>
    /// Simple example showing how to use projectile pooling in a real game scenario
    /// Add this to any GameObject that needs to fire projectiles
    /// </summary>
    public class SimpleProjectileExample : MonoBehaviour
    {
        [Header("Simple Projectile Setup")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private string poolName = "SimpleProjectile";
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 2f;
        [SerializeField] private KeyCode fireKey = KeyCode.Space;

        private ProjectileLauncher launcher;
        private float lastFireTime;

        private void Start()
        {
            // Set up the projectile launcher
            launcher = gameObject.AddComponent<ProjectileLauncher>();
            
            // Register our custom projectile pool if we have a prefab
            if (projectilePrefab != null)
            {
                ProjectilePoolManager.Instance.RegisterPool(poolName, projectilePrefab, 15, 50);
                launcher.SetProjectilePool(poolName);
            }
            
            // Configure fire point
            if (firePoint != null)
            {
                // If you have a specific fire point, you can set it via script
                // The ProjectileLauncher will use its own transform if none is set
            }

            Debug.Log($"SimpleProjectileExample: Ready to fire {poolName} projectiles!");
        }

        private void Update()
        {
            // Simple input handling
            if (Input.GetKeyDown(fireKey) && CanFire())
            {
                FireProjectile();
            }

            // Automatic firing example (uncomment if needed)
            // if (Input.GetKey(KeyCode.LeftShift) && CanFire())
            // {
            //     FireProjectile();
            // }
        }

        private bool CanFire()
        {
            return Time.time >= lastFireTime + (1f / fireRate);
        }

        private void FireProjectile()
        {
            // Method 1: Fire in the direction this object is facing
            Vector2 direction = transform.right; // or transform.up, depending on your setup
            
            // Method 2: Fire towards mouse position (for player control)
            if (Input.GetKey(KeyCode.LeftControl))
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                direction = (mousePos - transform.position).normalized;
            }

            // Method 3: Fire in a random direction (for testing)
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                direction = Random.insideUnitCircle.normalized;
            }

            // Launch the projectile using the pool system
            var projectile = launcher.Fire(direction);
            
            if (projectile != null)
            {
                lastFireTime = Time.time;
                Debug.Log($"Fired {poolName} projectile in direction {direction}");
            }
            else
            {
                Debug.LogWarning("Failed to fire projectile - pool may be exhausted");
            }
        }

        // Context menu for testing in editor
        [ContextMenu("Fire Test Projectile")]
        private void TestFire()
        {
            if (Application.isPlaying && launcher != null)
            {
                launcher.Fire(Vector2.right);
            }
        }

        [ContextMenu("Fire Spread Test")]
        private void TestSpread()
        {
            if (Application.isPlaying && launcher != null)
            {
                launcher.FireSpread(5, 45f);
            }
        }

        [ContextMenu("Fire Burst Test")]
        private void TestBurst()
        {
            if (Application.isPlaying && launcher != null)
            {
                launcher.FireBurst(8);
            }
        }

        // Display instructions in the editor
        private void OnDrawGizmosSelected()
        {
            // Draw fire direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right * 2f);
            
            // Draw fire point if assigned
            if (firePoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(firePoint.position, 0.2f);
            }
        }
    }

    /// <summary>
    /// Example of a player controller using projectile pooling
    /// </summary>
    public class PooledPlayerShooter : MonoBehaviour
    {
        [Header("Player Shooting")]
        [SerializeField] private float shootSpeed = 12f;
        [SerializeField] private float shootDamage = 30f;
        [SerializeField] private float shootRate = 3f;

        private ProjectileLauncher launcher;
        private Camera playerCamera;

        private void Start()
        {
            launcher = GetComponent<ProjectileLauncher>();
            if (launcher == null)
            {
                launcher = gameObject.AddComponent<ProjectileLauncher>();
            }

            playerCamera = Camera.main;
            
            // Configure launcher
            launcher.SetSpeed(shootSpeed);
            launcher.SetDamage(shootDamage);
            launcher.SetFireRate(shootRate);

            Debug.Log("PooledPlayerShooter: Ready to shoot!");
        }

        private void Update()
        {
            if (Input.GetMouseButton(0)) // Left mouse button
            {
                ShootAtMouse();
            }

            if (Input.GetKeyDown(KeyCode.Q)) // Spread shot
            {
                launcher.FireSpread(3, 30f);
            }

            if (Input.GetKeyDown(KeyCode.E)) // Burst shot
            {
                launcher.FireBurst(6);
            }
        }

        private void ShootAtMouse()
        {
            Vector3 mousePosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0; // Ensure 2D
            
            Vector2 direction = (mousePosition - transform.position).normalized;
            launcher.Fire(direction);
        }
    }

    /// <summary>
    /// Example of an enemy using projectile pooling
    /// </summary>
    public class PooledEnemyShooter : MonoBehaviour
    {
        [Header("Enemy Shooting")]
        [SerializeField] private float attackRange = 8f;
        [SerializeField] private float attackRate = 1f;
        [SerializeField] private string enemyProjectilePool = "EnemyProjectile";

        private ProjectileLauncher launcher;
        private Transform player;
        private float lastAttackTime;

        private void Start()
        {
            launcher = GetComponent<ProjectileLauncher>();
            if (launcher == null)
            {
                launcher = gameObject.AddComponent<ProjectileLauncher>();
            }

            // Find player
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }

            // Configure for enemy use
            launcher.SetProjectilePool(enemyProjectilePool);
            launcher.SetSpeed(8f);
            launcher.SetDamage(15f);
        }

        private void Update()
        {
            if (player == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= attackRange && CanAttack())
            {
                AttackPlayer();
            }
        }

        private bool CanAttack()
        {
            return Time.time >= lastAttackTime + (1f / attackRate);
        }

        private void AttackPlayer()
        {
            Vector2 direction = (player.position - transform.position).normalized;
            var projectile = launcher.Fire(direction);
            
            if (projectile != null)
            {
                lastAttackTime = Time.time;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw line to player if in range
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.position);
                Gizmos.color = distance <= attackRange ? Color.red : Color.yellow;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
    }
}