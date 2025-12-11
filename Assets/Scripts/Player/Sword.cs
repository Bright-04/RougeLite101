using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Sword : Weapon //  inherits Weapon so EquipmentManager works
{
    [SerializeField] private float attackCooldown = 0.5f;
    private float nextAttackTime = 0f;

    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private Transform weaponCollider;

    [Header("Hit Settings")]
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private float hitRadius = 1.5f;
    [SerializeField] private float dotThreshold = -0.3f; // how far to the side can still be hit
    [SerializeField] private LayerMask enemyLayerMask;

    [Header("Collider Positioning")]
    [SerializeField] private Vector3 defaultColliderLocalPosition = new Vector3(0.5f, 0f, 0f);
    [SerializeField] private Vector3 leftColliderLocalPosition = new Vector3(-0.1f, 0f, 0f);
    [SerializeField] private Vector3 rightColliderLocalPosition = new Vector3(0.1f, 0f, 0f);
    [SerializeField] private Vector3 colliderLocalScale = new Vector3(2f, 2f, 1f);

    private Animator myAnimator;
    private PlayerMovement playerMovement;

    private GameObject slashAnim;

    [SerializeField] private Transform weaponHolder;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        myAnimator = GetComponent<Animator>();
        

        weaponHolder = transform.parent; // because Sword is instantiated under WeaponHolder

        // TEMPORARY FIX: Reset weapon collider transform to reasonable values
        if (weaponCollider != null)
        {
            // Prefer making collider a child of the weapon holder so it follows the weapon
            if (weaponHolder != null)
            {
                weaponCollider.SetParent(weaponHolder, false);
            }
            else if (PlayerMovement.Instance != null)
            {
                // fallback to player transform if no weapon holder
                weaponCollider.SetParent(PlayerMovement.Instance.transform, false);
            }

            // Apply configurable defaults
            weaponCollider.localPosition = defaultColliderLocalPosition;
            weaponCollider.localScale = Vector3.one;
            weaponCollider.localRotation = Quaternion.identity;
        }

        // If no explicit enemy layer was set, try to resolve the "Enemy" layer once
        if (enemyLayerMask == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                enemyLayerMask = 1 << enemyLayer;
        }
    }

    private void Update()
    {
        FollowPlayerDirection();
    }

    public override void Use()
    {
        Attack();
    }

    private void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        myAnimator.SetTrigger("Attack");
        if (weaponCollider != null)
            weaponCollider.gameObject.SetActive(true);

        if (slashAnimPrefab != null && slashAnimSpawnPoint != null)
        {
            slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
            // Parent to weaponHolder when possible so it follows the weapon
            if (weaponHolder != null)
                slashAnim.transform.SetParent(weaponHolder, true);
            else if (transform.parent != null)
                slashAnim.transform.SetParent(transform.parent, true);
        }
    }

    public void DoneAttackingAnimEvent()
    {
        // BACKUP SOLUTION: If trigger didn't work, manually check for nearby enemies
        ManualHitDetection();

        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(false);
        }
        
    }
    
    private void ManualHitDetection()
    {
        // CRITICAL: Check if PlayerMovement instance is valid
        if (PlayerMovement.Instance == null || PlayerMovement.Instance.transform == null)
        {
            Debug.LogWarning("PlayerMovement.Instance is null or destroyed. Skipping hit detection.");
            return;
        }

        // Check for enemies in a small radius around the player
        Vector2 origin = PlayerMovement.Instance.transform.position;

        int layerMask = enemyLayerMask != 0 ? enemyLayerMask.value : 0;

        Collider2D[] hits;
        if (layerMask != 0)
            hits = Physics2D.OverlapCircleAll(origin, hitRadius, layerMask);
        else
            hits = Physics2D.OverlapCircleAll(origin, hitRadius);

        // Get player's last movement direction to determine attack direction
        Vector2 attackDirection = GetAttackDirection();

        // Avoid hitting the same damageable more than once
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        foreach (var hit in hits)
        {
            // double-guard: ensure layer matches if we didn't pass layerMask
            if (enemyLayerMask == 0 && hit.gameObject.layer != LayerMask.NameToLayer("Enemy"))
                continue;

            Vector2 toEnemy = (hit.transform.position - (Vector3)origin).normalized;
            float dotProduct = Vector2.Dot(attackDirection, toEnemy);

            if (dotProduct <= dotThreshold)
                continue;

            if (hit.TryGetComponent(out IDamageable damageable))
            {
                if (damaged.Contains(damageable))
                    continue;

                // Calculate damage using configurable base and player stats
                float finalDamage = baseDamage;
                PlayerStats stats = PlayerMovement.Instance.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    finalDamage += stats.attackDamage;
                    if (stats.TryCrit())
                    {
                        finalDamage *= stats.GetCritMultiplier();
                    }
                }

                damageable.TakeDamage(Mathf.RoundToInt(finalDamage));
                damaged.Add(damageable);
            }
        }
    }
    
    private Vector2 GetAttackDirection()
    {
        // Try to get the player's movement direction from the player controller
        // This requires reading the movement input
        PlayerMovement pc = PlayerMovement.Instance;
        if (pc == null)
            return Vector2.right;

        // Check the player's last movement direction from animator when available
        Animator anim = pc.GetComponent<Animator>();
        if (anim != null)
        {
            bool hasMoveX = false;
            bool hasMoveY = false;
            foreach (var p in anim.parameters)
            {
                if (p.name == "moveX") hasMoveX = true;
                if (p.name == "moveY") hasMoveY = true;
            }

            float moveX = 0f;
            float moveY = 0f;
            if (hasMoveX)
            {
                try { moveX = anim.GetFloat("moveX"); } catch { }
            }
            if (hasMoveY)
            {
                try { moveY = anim.GetFloat("moveY"); } catch { }
            }

            if (Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveY) > 0.01f)
            {
                return new Vector2(moveX, moveY).normalized;
            }
        }

        // Fallback: use facing direction (left/right only)
        return playerMovement != null && playerMovement.FacingLeft ? Vector2.left : Vector2.right;
    }

    public void SwingUpFlipAnimEvent()
    {
        if (slashAnim == null) return;

        slashAnim.transform.rotation = Quaternion.Euler(-180, 0, 0);
        if (playerMovement.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void SwingDownFlipAnimEvent()
    {
        if (slashAnim == null) return;

        slashAnim.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (playerMovement.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    private void FollowPlayerDirection()
    {
        if (playerMovement == null)
        {
            Debug.LogWarning("Sword: playerMovement is NULL!", this);
            return;
        }

        if (weaponHolder == null)
        {
            Debug.LogWarning("Sword: weaponHolder is NULL!", this);
        }

        if (playerMovement.FacingLeft)
        {
            if (weaponHolder != null)
            {
                Vector3 newScale = weaponHolder.localScale;
                newScale.x = -Mathf.Abs(newScale.x);
                weaponHolder.localScale = newScale;
            }

            if (weaponCollider != null)
            {
                weaponCollider.transform.localPosition = leftColliderLocalPosition;
                weaponCollider.transform.localRotation = Quaternion.identity;
                weaponCollider.transform.localScale = colliderLocalScale;
            }
        }
        else
        {
            if (weaponHolder != null)
            {
                Vector3 newScale = weaponHolder.localScale;
                newScale.x = Mathf.Abs(newScale.x);
                weaponHolder.localScale = newScale;
            }

            if (weaponCollider != null)
            {
                weaponCollider.transform.localPosition = rightColliderLocalPosition;
                weaponCollider.transform.localRotation = Quaternion.identity;
                weaponCollider.transform.localScale = colliderLocalScale;
            }
        }
    }

    // GIZMOS: Visualize the weapon collider range in Scene view
    private void OnDrawGizmos()
    {
        if (weaponCollider == null) return;

        // Get the collider component
        Collider2D col = weaponCollider.GetComponent<Collider2D>();
        if (col == null) return;

        // Draw in different colors based on if it's active
        Gizmos.color = weaponCollider.gameObject.activeSelf ? Color.red : Color.yellow;

        // Draw the collider bounds
        Gizmos.matrix = weaponCollider.transform.localToWorldMatrix;
        
        if (col is PolygonCollider2D polyCol)
        {
            // Draw polygon collider
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
            // Draw bounds as a box for other collider types
            Gizmos.DrawWireCube(col.offset, col.bounds.size);
        }

        // Reset matrix
        Gizmos.matrix = Matrix4x4.identity;
        
        // Draw a sphere at weapon collider position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(weaponCollider.position, 0.1f);
    }
}
