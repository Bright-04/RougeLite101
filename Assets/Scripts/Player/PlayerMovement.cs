using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{   
    private const string FallbackAnimatorControllerResourcePath = "Player_Rework_4Dir_Walk";

    public bool FacingLeft { get { return facingLeft; } set { facingLeft = value; } }
    public Vector2 LastAimDirection => lastAimDirection;
    public Transform AimPivot => aimPivot != null ? aimPivot : transform;

    public static PlayerMovement Instance;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private Transform aimPivot;
    [SerializeField] private bool flipSpriteWithAim = true;
    [SerializeField] private RuntimeAnimatorController fallbackAnimatorController;
    
    private PlayerControls playerControls;
    private Vector2 movement;
    private Vector2 lastAimDirection = Vector2.right;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;
    private Knockback knockback;
    private PlayerStats playerStats;
    private bool hasLastMoveXParameter;
    private bool hasLastMoveYParameter;
    private bool warnedMissingAnimatorController;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 200f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector2 dashDirection;

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
        playerStats = GetComponent<PlayerStats>();
        EnsureAnimatorController();
        CacheAnimatorParameters();

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
        // 1. Cập nhật Input ngay lập tức để có hướng mới nhất cho Dash
        PlayerInput();
        UpdateAimDirection();

        // 2. Kiểm tra Dash dựa trên hướng vừa cập nhật
        HandleDashInput();

        if (isDashing) return;

        if (dashTimer > 0)
            dashTimer -= Time.deltaTime;
    }

    private void HandleDashInput()
    {
        if (playerControls == null) return;

        // Note: You must add a "Dash" action to the Movement map in PlayerControls asset
        if (dashTimer <= 0 && playerControls.Movement.Dash.WasPressedThisFrame())
        {
            StartCoroutine(PerformDash());
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        dashTimer = dashCooldown;

        // Lưu lại lực cản gốc và tạm thời đưa về 0 để lướt không bị "phanh"
        float originalDrag = rb.linearDamping;
        rb.linearDamping = 0f;

        // Tắt Root Motion tạm thời để tránh Animator khóa vị trí nhân vật
        bool originalRootMotion = false;
        if (myAnimator != null)
        {
            originalRootMotion = myAnimator.applyRootMotion;
            myAnimator.applyRootMotion = false;
        }

        // Ưu tiên hướng di chuyển (8 hướng), nếu không bấm phím thì lướt theo hướng chuột
        dashDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : lastAimDirection;

        // Áp dụng vận tốc bùng nổ ngay lập tức
        rb.linearVelocity = dashDirection * dashSpeed;

        if (playerStats != null) playerStats.TriggerInvincibility(dashDuration);

        // Safe Animator Trigger
        if (myAnimator != null)
        {
            foreach (AnimatorControllerParameter param in myAnimator.parameters)
            {
                if (param.name == "Dash")
                {
                    myAnimator.SetTrigger("Dash");
                    break;
                }
            }
        }

        // Hiện hiệu ứng bóng ma (Afterimage)
        float startTime = Time.time;
        float ghostCooldown = 0.03f;
        float lastGhostTime = 0f;

        Vector2 startPos = rb.position;
        while (Time.time < startTime + dashDuration)
        {
            if (Time.time > lastGhostTime + ghostCooldown)
            {
                SpawnGhost();
                lastGhostTime = Time.time;
            }
            yield return null;
        }
        
        // Trả lại các thông số vật lý bình thường
        if (myAnimator != null) myAnimator.applyRootMotion = originalRootMotion;
        rb.linearDamping = originalDrag;
        isDashing = false;
        
        // Dừng hẳn vận tốc sau khi lướt xong để tránh trôi
        rb.linearVelocity = Vector2.zero;
    }

    private void SpawnGhost()
    {
        if (mySpriteRender == null) return;

        // Tạo 1 GameObject rỗng làm bóng ma
        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        // Cấu hình hình ảnh
        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = mySpriteRender.sprite;
        sr.flipX = mySpriteRender.flipX;
        sr.color = new Color(1f, 1f, 1f, 0.6f); // Trắng trong suốt
        sr.sortingLayerID = mySpriteRender.sortingLayerID;
        sr.sortingOrder = mySpriteRender.sortingOrder - 1; // Nằm dưới nhân vật chính

        // Bắt đầu làm mờ
        StartCoroutine(FadeGhost(ghost, sr));
    }

    private IEnumerator FadeGhost(GameObject ghost, SpriteRenderer sr)
    {
        float fadeTime = 0.3f; // Thời gian bóng mờ dần rồi biến mất
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            if (sr == null) break;
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.6f, 0f, elapsed / fadeTime);
            sr.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        Destroy(ghost);
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            // ÉP TỌA ĐỘ TRỰC TIẾP: Đây là cách mạnh nhất để vượt qua mọi rào cản tốc độ
            rb.position += dashDirection * (dashSpeed * Time.fixedDeltaTime);
            return;
        }

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

        // Cố định hoạt ảnh: Nếu đang lướt, không cập nhật moveX/moveY
        if (isDashing) return;

        if (!CanUseAnimatorController())
        {
            return;
        }

        myAnimator.SetFloat("moveX", movement.x);
        myAnimator.SetFloat("moveY", movement.y);

        if (movement.sqrMagnitude > 0.01f)
        {
            if (hasLastMoveXParameter) myAnimator.SetFloat("lastMoveX", movement.x);
            if (hasLastMoveYParameter) myAnimator.SetFloat("lastMoveY", movement.y);
        }
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
            if (flipSpriteWithAim && mySpriteRender != null)
            {
                mySpriteRender.flipX = FacingLeft;
            }
        }
    }

    private void CacheAnimatorParameters()
    {
        if (myAnimator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in myAnimator.parameters)
        {
            if (parameter.name == "lastMoveX")
            {
                hasLastMoveXParameter = true;
            }
            else if (parameter.name == "lastMoveY")
            {
                hasLastMoveYParameter = true;
            }
        }
    }

    private void EnsureAnimatorController()
    {
        if (myAnimator == null || myAnimator.runtimeAnimatorController != null)
        {
            return;
        }

        if (fallbackAnimatorController == null)
        {
            fallbackAnimatorController = Resources.Load<RuntimeAnimatorController>(FallbackAnimatorControllerResourcePath);
        }

        if (fallbackAnimatorController == null)
        {
            return;
        }

        myAnimator.runtimeAnimatorController = fallbackAnimatorController;
        CacheAnimatorParameters();
    }

    private bool CanUseAnimatorController()
    {
        EnsureAnimatorController();

        if (myAnimator == null || myAnimator.runtimeAnimatorController == null)
        {
            if (!warnedMissingAnimatorController)
            {
                string reason = myAnimator == null
                    ? "Animator component is missing"
                    : $"AnimatorController is missing and Resources.Load('{FallbackAnimatorControllerResourcePath}') returned null";
                Debug.LogWarning($"PlayerMovement: {reason}. Movement animation parameters will be skipped.", this);
                warnedMissingAnimatorController = true;
            }

            return false;
        }

        return true;
    }

}
