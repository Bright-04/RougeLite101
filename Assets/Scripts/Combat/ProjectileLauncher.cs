using UnityEngine;
using RougeLite.Events;
using RougeLite.Combat;

namespace RougeLite.Combat
{
    /// <summary>
    /// Component for launching projectiles using the object pool system
    /// Can be used by players, enemies, or spell systems
    /// </summary>
    public class ProjectileLauncher : EventBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private string defaultPoolName = "BasicProjectile";
        [SerializeField] private Transform firePoint;
        [SerializeField] private float defaultSpeed = 10f;
        [SerializeField] private float defaultDamage = 25f;
        [SerializeField] private float fireRate = 2f;
        [SerializeField] private bool autoFire = false;

        [Header("Audio")]
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private float fireSoundVolume = 0.7f;

        [Header("Effects")]
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField] private float muzzleFlashDuration = 0.1f;

        // Runtime state
        private float lastFireTime;
        private AudioSource audioSource;

        // Public properties
        public bool CanFire => Time.time >= lastFireTime + (1f / fireRate);
        public Transform FirePoint => firePoint != null ? firePoint : transform;

        protected override void Awake()
        {
            base.Awake();
            
            // Get or create audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = fireSoundVolume;
            }

            // Set fire point if not assigned
            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        private void Update()
        {
            if (autoFire && CanFire)
            {
                Fire();
            }
        }

        /// <summary>
        /// Fire a projectile using default settings
        /// </summary>
        public Projectile Fire()
        {
            Vector2 direction = FirePoint.right; // Default to right direction
            return Fire(direction);
        }

        /// <summary>
        /// Fire a projectile in specified direction
        /// </summary>
        public Projectile Fire(Vector2 direction)
        {
            return Fire(defaultPoolName, direction, defaultSpeed, defaultDamage);
        }

        /// <summary>
        /// Fire a projectile towards a target
        /// </summary>
        public Projectile FireAt(Transform target)
        {
            if (target == null) return null;
            
            Vector2 direction = (target.position - FirePoint.position).normalized;
            return Fire(direction);
        }

        /// <summary>
        /// Fire a projectile towards a position
        /// </summary>
        public Projectile FireAt(Vector2 targetPosition)
        {
            Vector2 direction = (targetPosition - (Vector2)FirePoint.position).normalized;
            return Fire(direction);
        }

        /// <summary>
        /// Fire a projectile with custom parameters
        /// </summary>
        public Projectile Fire(string poolName, Vector2 direction, float speed, float damage, string projectileType = "")
        {
            if (!CanFire)
            {
                return null;
            }

            // Launch projectile from pool
            var projectile = ProjectilePoolManager.Instance.LaunchProjectile(
                poolName,
                FirePoint.position,
                direction,
                speed,
                damage,
                gameObject,
                projectileType
            );

            if (projectile != null)
            {
                lastFireTime = Time.time;
                
                // Play effects
                PlayFireEffects();
                
                // Broadcast event
                var projectileLaunchedEvent = new ProjectileLaunchedEvent(projectile, gameObject, gameObject);
                BroadcastEvent(projectileLaunchedEvent);
            }

            return projectile;
        }

        /// <summary>
        /// Fire multiple projectiles in a spread pattern
        /// </summary>
        public Projectile[] FireSpread(int projectileCount, float spreadAngle, string poolName = null)
        {
            if (projectileCount <= 0) return new Projectile[0];
            
            poolName = poolName ?? defaultPoolName;
            var projectiles = new Projectile[projectileCount];
            
            // Calculate spread angles
            float angleStep = spreadAngle / (projectileCount - 1);
            float startAngle = -spreadAngle / 2f;
            
            Vector2 baseDirection = FirePoint.right;
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = baseAngle + startAngle + (angleStep * i);
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );
                
                projectiles[i] = Fire(poolName, direction, defaultSpeed, defaultDamage, "Spread");
            }

            return projectiles;
        }

        /// <summary>
        /// Fire projectiles in a circular burst pattern
        /// </summary>
        public Projectile[] FireBurst(int projectileCount, string poolName = null)
        {
            if (projectileCount <= 0) return new Projectile[0];
            
            poolName = poolName ?? defaultPoolName;
            var projectiles = new Projectile[projectileCount];
            
            float angleStep = 360f / projectileCount;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = angleStep * i;
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );
                
                projectiles[i] = Fire(poolName, direction, defaultSpeed, defaultDamage, "Burst");
            }

            return projectiles;
        }

        private void PlayFireEffects()
        {
            // Play fire sound
            if (fireSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fireSound, fireSoundVolume);
            }

            // Show muzzle flash
            if (muzzleFlash != null)
            {
                ShowMuzzleFlash();
            }
        }

        private void ShowMuzzleFlash()
        {
            var flash = Instantiate(muzzleFlash, FirePoint.position, FirePoint.rotation);
            Destroy(flash, muzzleFlashDuration);
        }

        /// <summary>
        /// Set the pool name for projectiles
        /// </summary>
        public void SetProjectilePool(string poolName)
        {
            defaultPoolName = poolName;
        }

        /// <summary>
        /// Set fire rate (shots per second)
        /// </summary>
        public void SetFireRate(float shotsPerSecond)
        {
            fireRate = Mathf.Max(0.1f, shotsPerSecond);
        }

        /// <summary>
        /// Set default damage for projectiles
        /// </summary>
        public void SetDamage(float damage)
        {
            defaultDamage = Mathf.Max(0f, damage);
        }

        /// <summary>
        /// Set default speed for projectiles
        /// </summary>
        public void SetSpeed(float speed)
        {
            defaultSpeed = Mathf.Max(0.1f, speed);
        }

        // Context menu methods for testing
        [ContextMenu("Test Fire")]
        private void TestFire()
        {
            Fire();
        }

        [ContextMenu("Test Fire Spread")]
        private void TestFireSpread()
        {
            FireSpread(5, 45f);
        }

        [ContextMenu("Test Fire Burst")]
        private void TestFireBurst()
        {
            FireBurst(8);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw fire point and direction
            if (FirePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(FirePoint.position, 0.2f);
                Gizmos.DrawRay(FirePoint.position, FirePoint.right * 2f);
                
                // Draw fire rate indicator
                Gizmos.color = CanFire ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(FirePoint.position + Vector3.up * 0.5f, Vector3.one * 0.1f);
            }
        }
    }

    /// <summary>
    /// Spell-specific projectile launcher with mana costs
    /// </summary>
    public class SpellProjectileLauncher : ProjectileLauncher
    {
        [Header("Spell Settings")]
        [SerializeField] private float manaCost = 10f;
        [SerializeField] private string spellName = "Magic Missile";
        [SerializeField] private bool requiresTarget = false;

        private PlayerStats playerStats;

        protected override void Awake()
        {
            base.Awake();
            playerStats = GetComponent<PlayerStats>();
        }

        /// <summary>
        /// Cast spell projectile with mana cost
        /// </summary>
        public bool CastSpell(Vector2 direction)
        {
            // Check mana
            if (playerStats != null && playerStats.currentMana < manaCost)
            {
                Debug.Log($"Not enough mana to cast {spellName}");
                return false;
            }

            // Fire projectile
            var projectile = Fire(direction);
            if (projectile != null)
            {
                // Consume mana
                if (playerStats != null)
                {
                    playerStats.UseMana(manaCost);
                }

                // Broadcast spell cast event
                var spellData = new SpellCastData
                {
                    spellName = spellName,
                    manaCost = manaCost,
                    caster = gameObject,
                    targetPosition = (Vector2)FirePoint.position + direction * 10f
                };
                BroadcastEvent(new SpellCastEvent(spellData, gameObject));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Cast spell at target
        /// </summary>
        public bool CastSpellAt(Transform target)
        {
            if (target == null && requiresTarget) return false;
            
            Vector2 direction = target != null 
                ? (target.position - FirePoint.position).normalized 
                : FirePoint.right;
                
            return CastSpell(direction);
        }
    }
}