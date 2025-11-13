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

    private PlayerControls playerControls;
    private Animator myAnimator;
    private PlayerController playerController;

    private GameObject slashAnim;

    [SerializeField] private Transform weaponHolder;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        myAnimator = GetComponent<Animator>();
        playerControls = new PlayerControls();

        weaponHolder = transform.parent; // because Sword is instantiated under WeaponHolder
        
        // TEMPORARY FIX: Reset weapon collider transform to reasonable values
        if (weaponCollider != null)
        {
            Debug.LogWarning("[SWORD] Applying TEMPORARY FIX to weapon collider position/scale!");
            // Make the collider a direct child of the player for simpler positioning
            weaponCollider.SetParent(PlayerController.Instance.transform);
            weaponCollider.localPosition = new Vector3(0.5f, 0f, 0f); // 0.5 units in front of player
            weaponCollider.localScale = Vector3.one; // Normal scale
            weaponCollider.localRotation = Quaternion.identity; // No rotation
            Debug.Log($"[SWORD] Fixed weapon collider - Local Position: {weaponCollider.localPosition}, Local Scale: {weaponCollider.localScale}");
        }
    }


    private void OnEnable() => playerControls.Enable();
    private void OnDisable() => playerControls.Disable();

    private void Start()
    {
        playerControls.Combat.Attack.started += _ => Attack();
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

        Debug.Log($"[SWORD] Attack initiated at {Time.time}");
        Debug.Log($"[SWORD] Player position: {PlayerController.Instance.transform.position}");
        
        myAnimator.SetTrigger("Attack");
        weaponCollider.gameObject.SetActive(true);
        
        Debug.Log($"[SWORD] Weapon collider WORLD position: {weaponCollider.position}");
        Debug.Log($"[SWORD] Weapon collider LOCAL position: {weaponCollider.localPosition}");
        Debug.Log($"[SWORD] Weapon collider LOCAL scale: {weaponCollider.localScale}");
        
        // Check for nearby enemies to help debug
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(PlayerController.Instance.transform.position, 5f);
        Debug.Log($"[SWORD] Found {nearbyColliders.Length} colliders within 5 units of player");
        foreach (var col in nearbyColliders)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                float distance = Vector2.Distance(PlayerController.Instance.transform.position, col.transform.position);
                Debug.Log($"[SWORD] Enemy '{col.gameObject.name}' at position {col.transform.position}, distance: {distance:F2}");
            }
        }

        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
        slashAnim.transform.parent = this.transform.parent;
    }

    public void DoneAttackingAnimEvent()
    {
        Debug.Log($"[SWORD] Weapon collider DISABLED");
        
        // BACKUP SOLUTION: If trigger didn't work, manually check for nearby enemies
        ManualHitDetection();
        
        weaponCollider.gameObject.SetActive(false);
    }
    
    private void ManualHitDetection()
    {
        // Check for enemies in a small radius around the player
        float hitRadius = 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(PlayerController.Instance.transform.position, hitRadius);
        
        // Get player's last movement direction to determine attack direction
        Vector2 attackDirection = GetAttackDirection();
        
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // Check if enemy is in the general direction of the attack
                Vector2 toEnemy = (hit.transform.position - PlayerController.Instance.transform.position).normalized;
                float dotProduct = Vector2.Dot(attackDirection, toEnemy);
                
                // Only hit if enemy is in front (dot > 0 means same general direction)
                // Use a threshold of -0.3 to allow hitting slightly off to the side
                if (dotProduct > -0.3f)
                {
                    if (hit.TryGetComponent(out SlimeHealth slimeHealth))
                    {
                        Debug.Log($"<color=cyan>[SWORD] 🎯 MANUAL HIT DETECTION triggered on {hit.gameObject.name} (direction: {attackDirection}, dot: {dotProduct:F2})</color>");
                        
                        // Calculate damage same as DamageSource
                        float finalDamage = 1; // baseDamage
                        PlayerStats stats = PlayerController.Instance.GetComponent<PlayerStats>();
                        if (stats != null)
                        {
                            finalDamage += stats.attackDamage;
                            if (stats.TryCrit())
                            {
                                finalDamage *= stats.GetCritMultiplier();
                                Debug.Log("<color=orange>[SWORD] 💥 CRITICAL HIT!</color>");
                            }
                        }
                        
                        Debug.Log($"<color=red>[SWORD] 💀 Dealing {finalDamage} damage via manual detection</color>");
                        slimeHealth.TakeDamage(Mathf.RoundToInt(finalDamage));
                    }
                }
                else
                {
                    Debug.Log($"<color=yellow>[SWORD] ❌ {hit.gameObject.name} is behind player (dot: {dotProduct:F2}), not hitting</color>");
                }
            }
        }
    }
    
    private Vector2 GetAttackDirection()
    {
        // Try to get the player's movement direction from the player controller
        // This requires reading the movement input
        PlayerController pc = PlayerController.Instance;
        
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
        return playerController.FacingLeft ? Vector2.left : Vector2.right;
    }

    public void SwingUpFlipAnimEvent()
    {
        if (slashAnim == null) return;

        slashAnim.transform.rotation = Quaternion.Euler(-180, 0, 0);
        if (playerController.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void SwingDownFlipAnimEvent()
    {
        if (slashAnim == null) return;

        slashAnim.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (playerController.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    private void FollowPlayerDirection()
    {
        if (playerController == null) Debug.LogError("Sword: playerController is NULL!", this);
        if (weaponCollider == null) Debug.LogError("Sword: weaponCollider is NULL!", this);

        if (playerController != null && playerController.FacingLeft)
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
