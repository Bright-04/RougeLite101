using UnityEngine;

public class ProjectileWeapon : Weapon
{
    [Header("Projectile")]
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform shootPoint;
    [SerializeField] private GameObject defaultProjectilePrefab;
    [SerializeField] private Transform defaultShootPoint;

    protected float projectileSpeed = 15f;
    protected int projectileDamage = 1;
    protected float projectileRange = 10f;
    protected int projectileCount = 1;
    protected float spreadAngle = 0f;

    private SpriteRenderer spriteRenderer;
    private WeaponController weaponController;
    private Quaternion aimRotation = Quaternion.identity;
    private Vector2 aimDirection = Vector2.right;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        weaponController = GetComponentInParent<WeaponController>();
        EnsureDefaults();
    }

    private void Update()
    {
        FollowPlayerDirection();
    }

    public override void Initialize(WeaponDefinitionSO definition)
    {
        EnsureDefaults();
        base.Initialize(definition);

        if (definition == null)
        {
            return;
        }

        if (definition.ProjectilePrefab != null)
        {
            projectilePrefab = definition.ProjectilePrefab;
        }

        projectileSpeed = definition.ProjectileSpeed;
        projectileDamage = definition.BaseDamage;
        projectileRange = definition.Range;
        projectileCount = Mathf.Max(1, definition.ProjectileCount);
        spreadAngle = definition.SpreadAngle;

        UpdateAimState();
    }

    public override void Use()
    {
        if (!CanShoot())
        {
            return;
        }

        SpawnProjectiles(spriteRenderer);
    }

    protected bool CanShoot()
    {
        if (Time.time < nextUseTime)
        {
            return false;
        }

        nextUseTime = Time.time + cooldown;
        return true;
    }

    protected void SpawnProjectiles(SpriteRenderer sortingSource = null)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{GetType().Name}: projectilePrefab is null!", this);
            return;
        }

        int count = Mathf.Max(1, projectileCount);
        float angleStep = count > 1 ? spreadAngle / (count - 1) : 0f;
        float startAngle = count > 1 ? -spreadAngle * 0.5f : 0f;

        for (int i = 0; i < count; i++)
        {
            Quaternion rotation = aimRotation * Quaternion.Euler(0f, 0f, startAngle + angleStep * i);
            if (projectilePrefab.GetComponent<FireballSpell>() != null)
            {
                rotation *= Quaternion.Euler(0f, 0f, 180f);
            }

            Vector3 spawnPos = GetProjectileSpawnPosition();
            GameObject projectile = ProjectilePool.Instance.Get(projectilePrefab, spawnPos, rotation);
            if (projectile == null)
            {
                continue;
            }

            InitializeProjectile(projectile);
            ApplyProjectileSorting(projectile, sortingSource);
            projectile.SetActive(true);
        }
    }

    private void InitializeProjectile(GameObject projectile)
    {
        WeaponProjectile weaponProjectile = projectile.GetComponent<WeaponProjectile>();
        if (weaponProjectile != null)
        {
            weaponProjectile.Initialize(projectileSpeed, projectileDamage, projectileRange);
            return;
        }

        FireballSpell fireballSpell = projectile.GetComponent<FireballSpell>();
        if (fireballSpell != null)
        {
            fireballSpell.Initialize(projectileSpeed, projectileDamage, projectileRange);
        }
    }

    private void ApplyProjectileSorting(GameObject projectile, SpriteRenderer sortingSource)
    {
        SpriteRenderer projectileSprite = projectile.GetComponentInChildren<SpriteRenderer>();
        if (projectileSprite == null)
        {
            return;
        }

        if (projectile.transform.localScale.magnitude < 0.5f)
        {
            projectile.transform.localScale = new Vector3(2f, 2f, 1f);
        }

        if (sortingSource != null)
        {
            projectileSprite.sortingLayerID = sortingSource.sortingLayerID;
            projectileSprite.sortingOrder = sortingSource.sortingOrder + 1;
        }
        else
        {
            projectileSprite.sortingOrder = 100;
        }
    }

    private void EnsureDefaults()
    {
        if (projectilePrefab == null)
        {
            projectilePrefab = defaultProjectilePrefab;
        }

        if (shootPoint == null)
        {
            shootPoint = defaultShootPoint;
        }
    }

    private void FollowPlayerDirection()
    {
        UpdateAimState();
    }

    private void UpdateAimState()
    {
        if (PlayerMovement.Instance == null)
        {
            return;
        }

        Vector2 currentAimDirection = PlayerMovement.Instance.LastAimDirection;
        if (currentAimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        aimDirection = currentAimDirection.normalized;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector3 GetProjectileSpawnPosition()
    {
        if (weaponDefinition != null)
        {
            if (weaponController == null)
            {
                weaponController = GetComponentInParent<WeaponController>();
            }

            if (weaponController != null)
            {
                return weaponController.GetProjectileSpawnPoint(weaponDefinition, aimDirection);
            }
        }

        if (shootPoint != null)
        {
            return new Vector3(shootPoint.position.x, shootPoint.position.y, 0f);
        }

        return transform.position;
    }
}
