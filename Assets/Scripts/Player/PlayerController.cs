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
        Debug.Log("PlayerController: Awake() called - PlayerController is initializing");
        
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
        if (Time.frameCount % 60 == 0) Debug.Log("Update() called");
        PlayerInput();
    }

    private void FixedUpdate()
    {
        if (Time.frameCount % 60 == 0) Debug.Log("FixedUpdate() called");
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
        if (Time.frameCount % 60 == 0) Debug.Log("AdjustPlayerFacingDirection() called");
        
        if (mySpriteRender == null)
        {
            Debug.LogWarning("PlayerController: SpriteRenderer is null, cannot flip sprite.");
            return;
        }

        if (Time.frameCount % 60 == 0) Debug.Log("SpriteRenderer found, calling TryFaceMouseDirection()");
        
        // TEMPORARILY: Only use mouse-based facing, no movement fallback
        TryFaceMouseDirection();
        
        // DISABLED: Fallback to movement-based facing
        /*
        if (movement.x < 0)
        {
            mySpriteRender.flipX = true;
            FacingLeft = true;
            if (Time.frameCount % 120 == 0) Debug.Log("Player facing LEFT based on movement");
        }
        else if (movement.x > 0)
        {
            mySpriteRender.flipX = false;
            FacingLeft = false;
            if (Time.frameCount % 120 == 0) Debug.Log("Player facing RIGHT based on movement");
        }
        */
    }

    private bool TryFaceMouseDirection()
    {
        if (Time.frameCount % 60 == 0) Debug.Log("TryFaceMouseDirection() STARTED");
        
        try
        {
            Vector3 mouseWorldPos = Vector3.zero;
            bool foundMouse = false;
            
            // Try new Input System first
            if (Mouse.current != null && Camera.main != null)
            {
                Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
                mouseScreenPos.z = Camera.main.nearClipPlane;
                mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                foundMouse = true;
                if (Time.frameCount % 60 == 0) Debug.Log($"NEW Input System: Screen {mouseScreenPos} -> World {mouseWorldPos}");
            }
            // Fallback to legacy input
            else if (Camera.main != null)
            {
                Vector3 mouseScreenPos = Input.mousePosition;
                mouseScreenPos.z = Camera.main.nearClipPlane;
                mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                foundMouse = true;
                if (Time.frameCount % 60 == 0) Debug.Log($"LEGACY Input: Screen {mouseScreenPos} -> World {mouseWorldPos}");
            }
            else
            {
                Debug.LogWarning("No camera available for mouse world position calculation");
                return false; // No camera available
            }

            if (!foundMouse)
            {
                Debug.LogWarning("Could not get mouse position");
                return false;
            }

            // Calculate if mouse is to the left or right of player
            float mouseX = mouseWorldPos.x;
            float playerX = transform.position.x;
            float difference = mouseX - playerX;

            // ALWAYS log the current sprite state and calculations
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"CURRENT STATE: Player X: {playerX:F2}, Mouse X: {mouseX:F2}, Diff: {difference:F2}");
                Debug.Log($"SPRITE STATE BEFORE: flipX = {mySpriteRender.flipX}, FacingLeft = {FacingLeft}");
            }

            // Force update every frame - no minimum distance check
            if (mouseX < playerX)
            {
                // Mouse is to the left - face left
                mySpriteRender.flipX = true;
                FacingLeft = true;
                if (Time.frameCount % 60 == 0) Debug.Log("FORCING LEFT: flipX = true");
            }
            else if (mouseX > playerX)
            {
                // Mouse is to the right - face right  
                mySpriteRender.flipX = false;
                FacingLeft = false;
                if (Time.frameCount % 60 == 0) Debug.Log("FORCING RIGHT: flipX = false");
            }
            
            // Log sprite state after change
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"SPRITE STATE AFTER: flipX = {mySpriteRender.flipX}, FacingLeft = {FacingLeft}");
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TryFaceMouseDirection failed: {e.Message}\nStack: {e.StackTrace}");
            return false; // Fall back to movement-based facing
        }
    }

}
