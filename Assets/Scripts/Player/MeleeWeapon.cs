using UnityEngine;

public class MeleeWeapon : Weapon
{
    [Header("Melee")]
    [SerializeField] protected float attackCooldown = 0.5f;
    [SerializeField] protected float colliderDistance = 0.15f;
    [SerializeField] protected Vector3 weaponColliderScale = new Vector3(2f, 2f, 1f);
    [SerializeField] protected Transform weaponCollider;
    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private Transform weaponHolder;

    protected float nextAttackTime = 0f;

    private Animator animator;
    private PlayerMovement playerMovement;
    private GameObject slashAnim;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        animator = GetComponent<Animator>();

        if (weaponHolder == null)
        {
            weaponHolder = transform.parent;
        }

        if (weaponCollider != null && playerMovement != null)
        {
            weaponCollider.SetParent(playerMovement.transform);
            weaponCollider.localScale = weaponColliderScale;
            weaponCollider.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (weaponCollider != null)
        {
            Destroy(weaponCollider.gameObject);
        }
    }

    private void Update()
    {
        FollowPlayerDirection();
    }

    public override void Initialize(WeaponDefinitionSO definition)
    {
        base.Initialize(definition);

        if (definition == null)
        {
            return;
        }

        attackCooldown = definition.Cooldown;
        colliderDistance = definition.HitboxDistance;
        weaponColliderScale = definition.HitboxScale;
        ApplyHitboxDefinition(definition);
    }

    public override void Use()
    {
        Attack();
    }

    private void Attack()
    {
        if (!CanAttack())
        {
            return;
        }

        if (animator == null || weaponCollider == null)
        {
            return;
        }

        animator.SetTrigger("Attack");
        weaponCollider.gameObject.SetActive(true);

        if (slashAnimPrefab != null && slashAnimSpawnPoint != null)
        {
            slashAnim = SlashEffectPool.Instance.Get(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity, transform.parent);
            SyncSlashSortingWithWeapon();
        }
    }

    public void DoneAttackingAnimEvent()
    {
        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(false);
        }
    }

    public void SwingUpFlipAnimEvent()
    {
        if (slashAnim == null)
        {
            return;
        }

        slashAnim.transform.rotation = Quaternion.Euler(-180f, 0f, 0f);
        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (slashRenderer != null)
        {
            slashRenderer.flipX = playerMovement != null && playerMovement.FacingLeft;
        }
    }

    public void SwingDownFlipAnimEvent()
    {
        if (slashAnim == null)
        {
            return;
        }

        slashAnim.transform.rotation = Quaternion.identity;
        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (slashRenderer != null)
        {
            slashRenderer.flipX = playerMovement != null && playerMovement.FacingLeft;
        }
    }

    protected bool CanAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        nextAttackTime = Time.time + attackCooldown;
        return true;
    }

    protected void ApplyHitboxDefinition(WeaponDefinitionSO definition)
    {
        if (weaponCollider == null || definition == null)
        {
            return;
        }

        weaponCollider.localScale = weaponColliderScale;

        DamageSource damageSource = weaponCollider.GetComponent<DamageSource>();
        if (damageSource != null)
        {
            damageSource.SetBaseDamage(definition.BaseDamage);
        }
    }

    private void SyncSlashSortingWithWeapon()
    {
        if (slashAnim == null)
        {
            return;
        }

        SpriteRenderer weaponRenderer = GetComponent<SpriteRenderer>();
        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (weaponRenderer == null || slashRenderer == null)
        {
            return;
        }

        slashRenderer.sortingLayerID = weaponRenderer.sortingLayerID;
        slashRenderer.sortingOrder = weaponRenderer.sortingOrder;
    }

    private void FollowPlayerDirection()
    {
        if (playerMovement == null || weaponCollider == null)
        {
            return;
        }

        Vector2 aimDirection = playerMovement.LastAimDirection;
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 normalizedAim = aimDirection.normalized;
        weaponCollider.localPosition = normalizedAim * colliderDistance;
        float aimAngle = Mathf.Atan2(normalizedAim.y, normalizedAim.x) * Mathf.Rad2Deg;
        weaponCollider.localRotation = Quaternion.Euler(0f, 0f, aimAngle);
        weaponCollider.localScale = weaponColliderScale;
    }

    private void OnDrawGizmos()
    {
        if (weaponCollider == null)
        {
            return;
        }

        Collider2D col = weaponCollider.GetComponent<Collider2D>();
        if (col == null)
        {
            return;
        }

        Gizmos.color = weaponCollider.gameObject.activeSelf ? Color.red : Color.yellow;
        Gizmos.matrix = weaponCollider.transform.localToWorldMatrix;

        if (col is PolygonCollider2D polyCol)
        {
            Vector2[] points = polyCol.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[(i + 1) % points.Length];
                Gizmos.DrawLine(start, end);
            }
        }
        else
        {
            Gizmos.DrawWireCube(col.offset, col.bounds.size);
        }

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(weaponCollider.position, 0.1f);
    }
}
