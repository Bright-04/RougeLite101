using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{   
    public bool FacingLeft { get { return facingLeft; } set { facingLeft = value; } }
    public static PlayerMovement Instance;

    [SerializeField] private float moveSpeed = 1f;
    
    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;
    private Knockback knockback;

    private bool facingLeft =false;
    private void Awake()
    {   
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();
        knockback = GetComponent<Knockback>();
    }

    private void Start()
    {
        // Đợi đến Start để đảm bảo InputManager đã Awake
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }

        playerControls = InputManager.Instance.Controls;
    }


    //private void OnEnable()
    //{
    //    playerControls.Movement.Enable();
    //}

    //private void OnDisable()
    //{
    //    playerControls.Movement.Disable();
    //}

    //private void OnDestroy()
    //{
    //    playerControls?.Dispose();
    //}

    private void Update()
    {
        PlayerInput();
    }

    private void FixedUpdate()
    {
        AdjustPlayerFacingDirection();
        
        // Don't move if being knocked back
        if (knockback != null && knockback.gettingKnockedBack)
            return;
            
        Move();
    }

    private void PlayerInput()
    {
        // Kiểm tra null trước khi đọc input
        if (playerControls == null)
        {
            movement = Vector2.zero;
            return;
        }

        movement = playerControls.Movement.Move.ReadValue<Vector2>();

        myAnimator.SetFloat("moveX", movement.x);
        myAnimator.SetFloat("moveY", movement.y);
    }

    private void Move()
    {
        rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
    }

    private void AdjustPlayerFacingDirection()
    {
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
