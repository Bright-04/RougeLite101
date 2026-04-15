using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{   
    public bool FacingLeft { get { return facingLeft; } set { facingLeft = value; } }
    public Vector2 LastAimDirection => lastAimDirection;
    public Transform AimPivot => aimPivot != null ? aimPivot : transform;

    public static PlayerMovement Instance;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private Transform aimPivot;
    
    private PlayerControls playerControls;
    private Vector2 movement;
    private Vector2 lastAimDirection = Vector2.right;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;
    private Knockback knockback;

    private bool facingLeft =false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Đã có Instance khác rồi, destroy object này
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();
        knockback = GetComponent<Knockback>();

        EnsureAimPivot();
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

    private void OnDestroy()
    {      
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        PlayerInput();
        UpdateAimDirection();
    }

    private void FixedUpdate()
    {
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

    private void EnsureAimPivot()
    {
        if (aimPivot != null)
        {
            return;
        }

        Transform existingPivot = transform.Find("AimPivot");
        if (existingPivot != null)
        {
            aimPivot = existingPivot;
            return;
        }

        GameObject pivotObject = new GameObject("AimPivot");
        pivotObject.transform.SetParent(transform, false);
        pivotObject.transform.localPosition = Vector3.zero;
        pivotObject.transform.localRotation = Quaternion.identity;
        pivotObject.transform.localScale = Vector3.one;
        aimPivot = pivotObject.transform;
    }

    private void UpdateAimDirection()
    {
        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        mouseWorldPosition.z = 0f;

        Vector3 aimOrigin = AimPivot.position;
        Vector2 newAimDirection = mouseWorldPosition - aimOrigin;

        if (newAimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        lastAimDirection = newAimDirection.normalized;

        // Use stable root center for flipping logic to avoid the AimPivot feedback loop vibration!
        Vector2 stableAimDirection = mouseWorldPosition - transform.position;
        if (Mathf.Abs(stableAimDirection.x) >= 0.01f)
        {
            FacingLeft = stableAimDirection.x < 0f;
            if (mySpriteRender != null)
            {
                mySpriteRender.flipX = FacingLeft;
            }
        }
    }

}
