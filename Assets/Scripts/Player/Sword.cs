using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // important for the new Input System
using RougeLite.Events;
using RougeLite.Player;

namespace RougeLite.Player
{

public class Sword : EventBehaviour
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

    protected override void Awake()
    {
        // Call base class Awake to initialize event system
        base.Awake();
        
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

    protected override void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (playerControls != null)
        {
            playerControls.Combat.Attack.started -= _ => Attack();
            playerControls.Disable();
            playerControls.Dispose();
        }
        
        // Call base class OnDestroy for event system cleanup
        base.OnDestroy();
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
        MouseFollowWithOffset(); // Use mouse direction instead of player direction
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
            // Use pooled slash animation for lower allocation churn
            slashAnim = RougeLite.ObjectPooling.EffectPool.Get(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
            if (slashAnim != null)
            {
                // Parent under same parent as before to preserve layering
                if (this.transform.parent != null)
                {
                    slashAnim.transform.SetParent(this.transform.parent, true);
                }

                // Auto-return the effect after a short duration
                var auto = slashAnim.GetComponent<RougeLite.ObjectPooling.EffectAutoReturn>();
                if (auto == null) auto = slashAnim.AddComponent<RougeLite.ObjectPooling.EffectAutoReturn>();
                auto.Init(slashAnimPrefab, 0.6f);

                // Immediately position slash animation towards mouse
                PositionSlashAnimationTowardsMouse();
            }
        }
        else
        {
            Debug.LogWarning("Sword: SlashAnimPrefab or SlashAnimSpawnPoint is null, slash animation will not appear.");
        }
        
        // Broadcast attack event
        var attackData = new AttackData(
            attacker: playerController != null ? playerController.gameObject : gameObject,
            target: null, // No specific target for sword swing
            damage: 1f, // You can make this configurable
            position: slashAnimSpawnPoint != null ? slashAnimSpawnPoint.position : transform.position,
            type: "Sword",
            critical: false
        );
        
        var attackEvent = new AttackPerformedEvent(attackData, gameObject);
        
        BroadcastEvent(attackEvent);
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
            // Position and orient slash animation based on mouse direction
            PositionSlashAnimationTowardsMouse();
        }
    }

    private void PositionSlashAnimationTowardsMouse()
    {
        if (slashAnim == null) return;

        try
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3 playerPos = playerController.transform.position;
            
            // Calculate direction from player to mouse
            Vector2 direction = (mouseWorldPos - playerPos).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position slash animation slightly offset from player towards mouse
            Vector3 slashOffset = direction * 1.5f; // Adjust distance as needed
            slashAnim.transform.position = playerPos + slashOffset;

            // Rotate slash animation to face mouse direction
            if (direction.x < 0)
            {
                // Mouse is to the left - flip the slash
                slashAnim.transform.rotation = Quaternion.Euler(0, 180, -angle);
                var spriteRenderer = slashAnim.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = true;
                }
            }
            else
            {
                // Mouse is to the right - normal orientation
                slashAnim.transform.rotation = Quaternion.Euler(0, 0, angle);
                var spriteRenderer = slashAnim.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Sword: Failed to position slash animation: {e.Message}");
            // Fallback to old behavior
            FallbackSlashPositioning();
        }
    }

    private void FallbackSlashPositioning()
    {
        // Original behavior as fallback
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

    private Vector3 GetMouseWorldPosition()
    {
        // Try new Input System first
        if (Mouse.current != null && UnityEngine.Camera.main != null)
        {
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = UnityEngine.Camera.main.nearClipPlane;
            return UnityEngine.Camera.main.ScreenToWorldPoint(mouseScreenPos);
        }
        
        // Fallback to legacy input
        if (UnityEngine.Camera.main != null)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = UnityEngine.Camera.main.nearClipPlane;
            return UnityEngine.Camera.main.ScreenToWorldPoint(mouseScreenPos);
        }

        // Last resort - return player position
        return playerController.transform.position;
    }

    private void MouseFollowWithOffset()
    {
        // Try world space approach first
        if (TryMouseFollowWorldSpace())
            return;

        // Fallback to screen space approach
        MouseFollowScreenSpace();
    }

    private bool TryMouseFollowWorldSpace()
    {
        // Try new Input System first
        if (Mouse.current != null && UnityEngine.Camera.main != null && playerController != null && activeWeapon != null)
        {
            try
            {
                // Get mouse position in world space using new Input System
                Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
                mouseScreenPos.z = UnityEngine.Camera.main.nearClipPlane;
                Vector3 mouseWorldPos = UnityEngine.Camera.main.ScreenToWorldPoint(mouseScreenPos);

                ApplyMouseDirection(mouseWorldPos);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Sword: New Input System failed: {e.Message}, trying legacy input");
            }
        }

        // Fallback to legacy Input system
        if (UnityEngine.Camera.main != null && playerController != null && activeWeapon != null)
        {
            try
            {
                // Get mouse position using legacy Input system
                Vector3 mouseScreenPos = Input.mousePosition;
                mouseScreenPos.z = UnityEngine.Camera.main.nearClipPlane;
                Vector3 mouseWorldPos = UnityEngine.Camera.main.ScreenToWorldPoint(mouseScreenPos);

                ApplyMouseDirection(mouseWorldPos);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Sword: Legacy input also failed: {e.Message}");
            }
        }

        return false;
    }

    private void ApplyMouseDirection(Vector3 mouseWorldPos)
    {
        // Calculate direction from player to mouse in world space
        Vector2 direction = (mouseWorldPos - playerController.transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Debug log occasionally
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"Mouse World: {mouseWorldPos}, Player: {playerController.transform.position}, Direction: {direction}, Angle: {angle}");
        }

        // Apply rotation to weapon
        if (direction.x < 0)
        {
            // Mouse is on the left side of player
            activeWeapon.transform.rotation = Quaternion.Euler(0, 180, -angle);
            if (weaponCollider != null)
                weaponCollider.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            // Mouse is on the right side of player
            activeWeapon.transform.rotation = Quaternion.Euler(0, 0, angle);
            if (weaponCollider != null)
                weaponCollider.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    private void MouseFollowScreenSpace()
    {
        // Check if mouse input is available
        if (Mouse.current == null)
        {
            Debug.LogWarning("Sword: Mouse.current is null, falling back to player direction");
            FollowPlayerDirection();
            return;
        }

        // Check if camera is available
        if (UnityEngine.Camera.main == null)
        {
            Debug.LogWarning("Sword: UnityEngine.Camera.main is null, falling back to player direction");
            FollowPlayerDirection();
            return;
        }

        // Check if critical components exist
        if (playerController == null || activeWeapon == null)
        {
            return;
        }

        // Get mouse position and convert player position to screen space
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 playerScreenPoint = UnityEngine.Camera.main.WorldToScreenPoint(playerController.transform.position);

        // Calculate direction from player to mouse in screen space
        Vector2 direction = (mousePos - playerScreenPoint).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (mousePos.x < playerScreenPoint.x)
        {
            // Mouse is on the left side of player
            activeWeapon.transform.rotation = Quaternion.Euler(0, -180, -angle);
            if (weaponCollider != null)
                weaponCollider.transform.rotation = Quaternion.Euler(0, -180, 0);
        }
        else
        {
            // Mouse is on the right side of player
            activeWeapon.transform.rotation = Quaternion.Euler(0, 0, angle);
            if (weaponCollider != null)
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
}
