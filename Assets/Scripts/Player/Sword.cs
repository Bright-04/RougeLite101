using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // important for the new Input System

public class Sword : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 0.5f; // seconds
    private float nextAttackTime = 0f;

    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private Transform weaponCollider;
    private PlayerControls playerControls;
    private Animator myAnimator;
    private PlayerController playerController;
    private ActiveWeapon activeWeapon;

    private GameObject slashAnim;

    private void Awake()
    {
        // Get and validate critical components
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError($"Sword: PlayerController component missing in parent of {gameObject.name}! Sword functionality will not work.", this);
        }
        
        activeWeapon = GetComponentInParent<ActiveWeapon>();
        if (activeWeapon == null)
        {
            Debug.LogError($"Sword: ActiveWeapon component missing in parent of {gameObject.name}! Weapon positioning will not work.", this);
        }
        
        myAnimator = GetComponent<Animator>();
        if (myAnimator == null)
        {
            Debug.LogError($"Sword: Animator component missing on {gameObject.name}! Attack animations will not work.", this);
        }
        
        // Initialize input system
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (playerControls != null)
        {
            playerControls.Combat.Attack.started -= _ => Attack();
            playerControls.Disable();
            playerControls.Dispose();
        }
    }

    void Start()
    {
        if (playerControls != null)
        {
            playerControls.Combat.Attack.started += _ => Attack();
        }
        else
        {
            Debug.LogError("Sword: PlayerControls is null, attack input will not work.");
        }
    }

    private void Update()
    {
        FollowPlayerDirection();
    }

    private void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        // Validate components before using them
        if (myAnimator != null)
        {
            myAnimator.SetTrigger("Attack");
        }
        
        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(true);
        }
        
        if (slashAnimPrefab != null && slashAnimSpawnPoint != null)
        {
            slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
            if (slashAnim != null && this.transform.parent != null)
            {
                slashAnim.transform.parent = this.transform.parent;
            }
        }
        else
        {
            Debug.LogWarning("Sword: SlashAnimPrefab or SlashAnimSpawnPoint is null, slash animation will not appear.");
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
        if (slashAnim != null && playerController != null)
        {
            slashAnim.gameObject.transform.rotation = Quaternion.Euler(-180, 0, 0);
            if (playerController.FacingLeft)
            {
                var spriteRenderer = slashAnim.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }
    }

    public void SwingDownFlipAnimEvent()
    {
        if (slashAnim != null && playerController != null)
        {
            slashAnim.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
            if (playerController.FacingLeft)
            {
                var spriteRenderer = slashAnim.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }
    }

    private void MouseFollowWithOffset()
    {
        // Using new Input System
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 playerScreenPoint = Camera.main.WorldToScreenPoint(playerController.transform.position);


        float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;

        if (mousePos.x < playerScreenPoint.x)
        {
            activeWeapon.transform.rotation = Quaternion.Euler(0, -180, angle);
            weaponCollider.transform.rotation = Quaternion.Euler(0, -180, 0);
        }
        else
        {
            activeWeapon.transform.rotation = Quaternion.Euler(0, 0, angle);
            weaponCollider.transform.rotation = Quaternion.Euler(0, 0, 0);

        }
    }
    private void FollowPlayerDirection()
    {
        if (playerController == null || activeWeapon == null || weaponCollider == null)
        {
            return; // Skip if critical components are missing
        }

        if (playerController.FacingLeft)
        {
            activeWeapon.transform.rotation = Quaternion.Euler(0, -180, 0);
            weaponCollider.transform.rotation = Quaternion.Euler(0, -180, 0);
        }
        else
        {
            activeWeapon.transform.rotation = Quaternion.Euler(0, 0, 0);
            weaponCollider.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

}
