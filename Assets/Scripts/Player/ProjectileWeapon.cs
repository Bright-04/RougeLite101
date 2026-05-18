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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (projectilePrefab == null || shootPoint == null)
        {
            Debug.LogWarning($"{GetType().Name}: projectilePrefab or shootPoint is null!", this);
            return;
        }

        int count = Mathf.Max(1, projectileCount);
        float angleStep = count > 1 ? spreadAngle / (count - 1) : 0f;
        float startAngle = count > 1 ? -spreadAngle * 0.5f : 0f;

        for (int i = 0; i < count; i++)
        {
            Quaternion rotation = shootPoint.rotation * Quaternion.Euler(0f, 0f, startAngle + angleStep * i);
            Vector3 spawnPos = new Vector3(shootPoint.position.x, shootPoint.position.y, 0f);
            GameObject projectile = Instantiate(projectilePrefab, spawnPos, rotation);

            InitializeProjectile(projectile);
            ApplyProjectileSorting(projectile, sortingSource);
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
        if (PlayerMovement.Instance == null)
        {
            return;
        }

        Vector2 aimDirection = PlayerMovement.Instance.LastAimDirection;
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle) * GetLocalRotationOffset();
        if (weaponDefinition != null)
        {
            transform.localPosition = Quaternion.Euler(0f, 0f, angle) * weaponDefinition.LocalPositionOffset;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipY = PlayerMovement.Instance.FacingLeft;
        }
    }
}
