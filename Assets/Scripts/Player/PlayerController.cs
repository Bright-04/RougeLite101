using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RougeLite.Events;

public class PlayerController : EventBehaviour
{   
    public bool FacingLeft { get { return facingLeft; } set { facingLeft = value; } }
    public static PlayerController Instance;

    [SerializeField] private float moveSpeed = 1f;
    
    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;

    private bool facingLeft =false;
    protected override void Awake()
    {   
        // Call base class Awake to initialize event system
        base.Awake();
        
        // Initialize singleton
        Instance = this;
        
        // Initialize input system
        playerControls = new PlayerControls();
        
        // Get and validate critical components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"PlayerController: Rigidbody2D component missing on {gameObject.name}! Player movement will not work.", this);
        }
        
        myAnimator = GetComponent<Animator>();
        if (myAnimator == null)
        {
            Debug.LogError($"PlayerController: Animator component missing on {gameObject.name}! Animation will not work.", this);
        }
        
        mySpriteRender = GetComponent<SpriteRenderer>();
        if (mySpriteRender == null)
        {
            Debug.LogError($"PlayerController: SpriteRenderer component missing on {gameObject.name}! Sprite flipping will not work.", this);
        }
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
        // Clean up singleton instance
        if (Instance == this)
        {
            Instance = null;
        }

        // Dispose of input system resources
        playerControls?.Disable();
        playerControls?.Dispose();
        
        // Call base class OnDestroy for event system cleanup
        base.OnDestroy();
    }

    private void Update()
    {
        PlayerInput();
    }

    private void FixedUpdate()
    {
        AdjustPlayerFacingDirection();
        Move();
    }

    private void PlayerInput()
    {
        if (playerControls == null)
        {
            Debug.LogWarning("PlayerController: PlayerControls is null, cannot read input.");
            return;
        }

        movement = playerControls.Movement.Move.ReadValue<Vector2>();

        // Update animator parameters if animator exists
        if (myAnimator != null)
        {
            myAnimator.SetFloat("moveX", movement.x);
            myAnimator.SetFloat("moveY", movement.y);
        }
    }

    private void Move()
    {
        if (rb == null)
        {
            Debug.LogWarning("PlayerController: Rigidbody2D is null, cannot move player.");
            return;
        }

        Vector2 previousPosition = rb.position;
        rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
        
        // Broadcast movement event if the player is actually moving
        if (movement.magnitude > 0.1f)
        {
            var movementData = new PlayerMovementData(
                player: gameObject,
                velocity: movement * moveSpeed,
                position: transform.position,
                previousPosition: previousPosition
            );
            
            var movementEvent = new PlayerMovementEvent(movementData, gameObject);
            
            BroadcastEvent(movementEvent);
        }
    }

    private void AdjustPlayerFacingDirection()
    {
        if (mySpriteRender == null)
        {
            Debug.LogWarning("PlayerController: SpriteRenderer is null, cannot flip sprite.");
            return;
        }

        // Use mouse-based facing for responsive combat controls
        TryFaceMouseDirection();
    }

    private bool TryFaceMouseDirection()
    {
        try
        {
            Vector3 mouseWorldPos = Vector3.zero;
            
            // Try new Input System first
            if (Mouse.current != null && Camera.main != null)
            {
                Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
                mouseScreenPos.z = Camera.main.nearClipPlane;
                mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            }
            // Fallback to legacy input
            else if (Camera.main != null)
            {
                Vector3 mouseScreenPos = Input.mousePosition;
                mouseScreenPos.z = Camera.main.nearClipPlane;
                mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            }
            else
            {
                return false; // No camera available
            }

            // Calculate if mouse is to the left or right of player
            float mouseX = mouseWorldPos.x;
            float playerX = transform.position.x;

            // Update character facing based on mouse position
            if (mouseX < playerX)
            {
                // Mouse is to the left - face left
                mySpriteRender.flipX = true;
                FacingLeft = true;
            }
            else if (mouseX > playerX)
            {
                // Mouse is to the right - face right  
                mySpriteRender.flipX = false;
                FacingLeft = false;
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"TryFaceMouseDirection failed: {e.Message}");
            return false;
        }
    }

}
