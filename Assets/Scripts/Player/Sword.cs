using System.Collections;
using System.Collections.Generic;
using RougeLite.System;
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

    private HashSet<Collider2D> enemiesHitThisSwing = new HashSet<Collider2D>();
    private bool isAttacking = false;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        myAnimator = GetComponent<Animator>();
        
        weaponHolder = transform.parent; // because Sword is instantiated under WeaponHolder
    }

    private void Update()
    {
        FollowPlayerDirection();
        if (isAttacking)
        {
            ManualHitDetection();
        }
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
        isAttacking = true;
        enemiesHitThisSwing.Clear();

        // Track swing for Accuracy Adaptive Setting
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.RecordSwing();
        }

        myAnimator.SetTrigger("Attack");
        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(true);
        }

        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
        slashAnim.transform.parent = this.transform.parent;
    }

    public void DoneAttackingAnimEvent()
    {
        isAttacking = false;
        
        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(false);
        }
    }
    
    private void ManualHitDetection()
    {
        // Require active collider
        if (weaponCollider == null || !weaponCollider.gameObject.activeInHierarchy) return;

        Collider2D col = weaponCollider.GetComponent<Collider2D>();
        if (col == null) return;

        // Use the collider's precise geometry to hit enemies
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = LayerMask.GetMask("Enemy");
        filter.useTriggers = true; // Enemies use Trigger colliders

        List<Collider2D> hits = new List<Collider2D>();
        col.Overlap(filter, hits);

        foreach (var hit in hits)
        {
            if (!enemiesHitThisSwing.Contains(hit))
            {
                enemiesHitThisSwing.Add(hit);
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    float finalDamage = 2; // baseDamage fallback
                    PlayerStats stats = PlayerMovement.Instance != null ? PlayerMovement.Instance.GetComponent<PlayerStats>() : null;
                    if (stats != null)
                    {
                        finalDamage += stats.attackDamage;
                        if (stats.TryCrit())
                        {
                            finalDamage *= stats.GetCritMultiplier();
                        }
                    }
                    
                    damageable.TakeDamage(Mathf.RoundToInt(finalDamage));
                    
                    if (DifficultyManager.Instance != null)
                    {
                        DifficultyManager.Instance.RecordHit();
                    }
                }
            }
        }
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
        if (playerMovement == null) Debug.LogError("Sword: playerMovement is NULL!", this);
        if (weaponCollider == null) Debug.LogError("Sword: weaponCollider is NULL!", this);

        if (playerMovement != null && playerMovement.FacingLeft)
        {
            // Flip the weapon holder on X scale instead of rotating on Y axis
            // This maintains position while flipping the sprite
            Vector3 newScale = weaponHolder.localScale;
            newScale.x = -Mathf.Abs(newScale.x); // Ensure it's negative (flipped)
            weaponHolder.localScale = newScale;
        }
        else
        {
            // Reset to normal scale
            Vector3 newScale = weaponHolder.localScale;
            newScale.x = Mathf.Abs(newScale.x); // Ensure it's positive (not flipped)
            weaponHolder.localScale = newScale;
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
