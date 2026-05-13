using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Sword : Weapon //  inherits Weapon so EquipmentManager works
{
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float colliderDistance = 0.15f;
    [SerializeField] private Vector3 weaponColliderScale = new Vector3(2f, 2f, 1f);
    private float nextAttackTime = 0f;

    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private Transform weaponCollider;

    
    private Animator myAnimator;
    private PlayerMovement playerMovement;

    private GameObject slashAnim;

    [SerializeField] private Transform weaponHolder;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        myAnimator = GetComponent<Animator>();

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
    //add vì weaponCollider bị duplicate mỗi lần equip sword
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

    public override void Use()
    {
        Attack();
    }

    private void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        if (myAnimator == null || weaponCollider == null)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;

        myAnimator.SetTrigger("Attack");
        weaponCollider.gameObject.SetActive(true);

        if (slashAnimPrefab != null && slashAnimSpawnPoint != null)
        {
            slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
            slashAnim.transform.parent = this.transform.parent;
            SyncSlashSortingWithWeapon();
        }
    }

    private void SyncSlashSortingWithWeapon()
    {
        if (slashAnim == null)
        {
            return;
        }

        SpriteRenderer swordRenderer = GetComponent<SpriteRenderer>();
        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (swordRenderer == null || slashRenderer == null)
        {
            return;
        }

        slashRenderer.sortingLayerID = swordRenderer.sortingLayerID;
        slashRenderer.sortingOrder = swordRenderer.sortingOrder;
    }

    public void DoneAttackingAnimEvent()
    {
        // [CẬP NHẬT] Chúng ta không gọi ManualHitDetection đè vào nữa vì đã dùng DamageSource.cs gắn trên Weapon Collider.
        // Việc gọi song song 2 hàm sẽ gây lỗi 1-Hit Slime (Double Damage). Đã comment block hàm bên dưới để giữ tóm tắt logic nếu về sau cần dùng.
        // ManualHitDetection();

        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(false);
        }
        
    }
    
    /* [Vô Hiệu Hoá Block Code] 
    private void ManualHitDetection()
    {
        // CRITICAL: Check if PlayerMovement instance is valid
        if (playerMovement == null || playerMovement.transform == null)
        {
            Debug.LogWarning("PlayerMovement.Instance is null or destroyed. Skipping hit detection.");
            return;
        }

        // Check for enemies in a small radius around the player
        float hitRadius = 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerMovement.transform.position, hitRadius);
        
        // Get player's last movement direction to determine attack direction
        Vector2 attackDirection = GetAttackDirection();
        
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // Check if enemy is in the general direction of the attack
                Vector2 toEnemy = (hit.transform.position - playerMovement.transform.position).normalized;
                float dotProduct = Vector2.Dot(attackDirection, toEnemy);
                
                // Only hit if enemy is in front (dot > 0 means same general direction)
                // Use a threshold of -0.3 to allow hitting slightly off to the side
                if (dotProduct > -0.3f)
                {
                    if (hit.TryGetComponent(out IDamageable damageable))
                    {
                        // Calculate damage same as DamageSource
                        float finalDamage = 1; // baseDamage
                        PlayerStats stats = playerMovement.GetComponent<PlayerStats>();
                        if (stats != null)
                        {
                            finalDamage += stats.attackDamage;
                            if (stats.TryCrit())
                            {
                                finalDamage *= stats.GetCritMultiplier();
                            }
                        }
                        
                        damageable.TakeDamage(Mathf.RoundToInt(finalDamage));
                    }
                }
            }
        }
    }
    */
    
    private Vector2 GetAttackDirection()
    {
        if (playerMovement == null)
        {
            return Vector2.right;
        }

        Vector2 aimDirection = playerMovement.LastAimDirection;
        if (aimDirection.sqrMagnitude > 0.0001f)
        {
            return aimDirection.normalized;
        }

        return playerMovement.FacingLeft ? Vector2.left : Vector2.right;
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
        if (playerMovement == null || weaponCollider == null)
        {
            return;
        }

        Vector2 aimDirection = playerMovement.LastAimDirection;
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float facingMultiplier = playerMovement.FacingLeft ? -1f : 1f;

        if (weaponDefinition != null && transform.parent != null && transform.parent.name.StartsWith("OffsetContainer_"))
        {
            // 1. Flip the container's scale. 
            // This ensures ANY inner prefab offsets (like m_LocalPosition: -0.3) 
            // correctly mirror across the hand point instead of remaining fixed!
            Vector3 containerScale = transform.parent.localScale;
            containerScale.x = facingMultiplier * Mathf.Abs(containerScale.x);
            transform.parent.localScale = containerScale;

            // Reset sword scale just in case it was flipped previously
            Vector3 sScale = transform.localScale;
            sScale.x = Mathf.Abs(sScale.x);
            transform.localScale = sScale;

            // 2. Flip the SO offset dynamically
            Vector3 offset = weaponDefinition.LocalPositionOffset;
            offset.x *= facingMultiplier;
            transform.parent.localPosition = offset;
            
            // 3. Flip the SO rotation dynamically
            Vector3 rotOffset = weaponDefinition.LocalRotationOffset;
            rotOffset.z *= facingMultiplier;
            transform.parent.localRotation = Quaternion.Euler(rotOffset);
        }
        else
        {
            // Fallback if there is no wrapper
            Vector3 swordScale = transform.localScale;
            swordScale.x = facingMultiplier * Mathf.Abs(swordScale.x);
            transform.localScale = swordScale;
        }

        Vector2 normalizedAim = aimDirection.normalized;
        weaponCollider.localPosition = normalizedAim * colliderDistance;
        float aimAngle = Mathf.Atan2(normalizedAim.y, normalizedAim.x) * Mathf.Rad2Deg;
        weaponCollider.localRotation = Quaternion.Euler(0f, 0f, aimAngle);
        weaponCollider.localScale = weaponColliderScale;
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
