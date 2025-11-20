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
            // Make the collider a direct child of the player for simpler positioning
            weaponCollider.SetParent(PlayerMovement.Instance.transform);
            weaponCollider.localPosition = new Vector3(0.5f, 0f, 0f); // 0.5 units in front of player
            weaponCollider.localScale = Vector3.one; // Normal scale
            weaponCollider.localRotation = Quaternion.identity; // No rotation
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
        weaponCollider.gameObject.SetActive(true);

        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
        slashAnim.transform.parent = this.transform.parent;
    }

    public void DoneAttackingAnimEvent()
    {
        // BACKUP SOLUTION: If trigger didn't work, manually check for nearby enemies
        ManualHitDetection();
        
        weaponCollider.gameObject.SetActive(false);
    }
    
    private void ManualHitDetection()
    {
        // Check for enemies in a small radius around the player
        float hitRadius = 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(PlayerMovement.Instance.transform.position, hitRadius);
        
        // Get player's last movement direction to determine attack direction
        Vector2 attackDirection = GetAttackDirection();
        
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // Check if enemy is in the general direction of the attack
                Vector2 toEnemy = (hit.transform.position - PlayerMovement.Instance.transform.position).normalized;
                float dotProduct = Vector2.Dot(attackDirection, toEnemy);
                
                // Only hit if enemy is in front (dot > 0 means same general direction)
                // Use a threshold of -0.3 to allow hitting slightly off to the side
                if (dotProduct > -0.3f)
                {
                    if (hit.TryGetComponent(out IDamageable damageable))
                    {
                        // Calculate damage same as DamageSource
                        float finalDamage = 1; // baseDamage
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
                    }
                }
            }
        }
    }
    
    private Vector2 GetAttackDirection()
    {
        // Try to get the player's movement direction from the player controller
        // This requires reading the movement input
        PlayerMovement pc = PlayerMovement.Instance;
        
        // Check the player's last movement direction from animator
        Animator anim = pc.GetComponent<Animator>();
        if (anim != null)
        {
            float moveX = anim.GetFloat("moveX");
            float moveY = anim.GetFloat("moveY");
            
            // If player was moving, use that direction
            if (Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveY) > 0.01f)
            {
                return new Vector2(moveX, moveY).normalized;
            }
        }
        
        // Fallback: use facing direction (left/right only)
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
        if (playerMovement == null) Debug.LogError("Sword: playerController is NULL!", this);
        if (weaponCollider == null) Debug.LogError("Sword: weaponCollider is NULL!", this);

        if (playerMovement != null && playerMovement.FacingLeft)
        {
            // Flip the weapon holder on X scale instead of rotating on Y axis
            // This maintains position while flipping the sprite
            Vector3 newScale = weaponHolder.localScale;
            newScale.x = -Mathf.Abs(newScale.x); // Ensure it's negative (flipped)
            weaponHolder.localScale = newScale;
            
            // Position weapon collider to the LEFT of player (closer range to hit nearby enemies)
            weaponCollider.transform.localPosition = new Vector3(-0.1f, 0f, 0f);
            weaponCollider.transform.localRotation = Quaternion.identity;
            weaponCollider.transform.localScale = new Vector3(2f, 2f, 1f); // Larger hitbox
        }
        else
        {
            // Reset to normal scale
            Vector3 newScale = weaponHolder.localScale;
            newScale.x = Mathf.Abs(newScale.x); // Ensure it's positive (not flipped)
            weaponHolder.localScale = newScale;
            
            // Position weapon collider to the RIGHT of player (closer range to hit nearby enemies)
            weaponCollider.transform.localPosition = new Vector3(0.1f, 0f, 0f);
            weaponCollider.transform.localRotation = Quaternion.identity;
            weaponCollider.transform.localScale = new Vector3(2f, 2f, 1f); // Larger hitbox
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
