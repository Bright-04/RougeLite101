using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
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
    private void Awake()
    {   
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

    private void OnDestroy()
    {
        // Clean up singleton instance
        if (Instance == this)
        {
            Instance = null;
        }

        // Dispose of input system resources
        playerControls?.Disable();
        playerControls?.Dispose();
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

        rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
    }

    private void AdjustPlayerFacingDirection()
    {
        if (mySpriteRender == null)
        {
            Debug.LogWarning("PlayerController: SpriteRenderer is null, cannot flip sprite.");
            return;
        }

        if (movement.x < 0)
        {
            mySpriteRender.flipX = true;
            FacingLeft = true;
        }
        else if (movement.x > 0)
        {
            mySpriteRender.flipX = false;
            FacingLeft = false;
        }
    }

}
