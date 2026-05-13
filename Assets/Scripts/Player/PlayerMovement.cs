using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{   
    private const string PlayerAnimatorControllerResourcePath = "Player";
    private const string SwordAnimatorControllerResourcePath = "Player_Sword";
    private const string BowAnimatorControllerResourcePath = "Player_Bow";

    public enum AnimationProfile
    {
        Default,
        Sword,
        Bow
    }

    public bool FacingLeft { get { return facingLeft; } set { facingLeft = value; } }
    public Vector2 LastAimDirection => lastAimDirection;
    public Vector2 LookDirection => lookDirection;
    public Transform AimPivot => aimPivot != null ? aimPivot : transform;

    public static PlayerMovement Instance;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private Transform aimPivot;
    [SerializeField] private RuntimeAnimatorController playerAnimatorController;
    [SerializeField] private RuntimeAnimatorController swordAnimatorController;
    [SerializeField] private RuntimeAnimatorController bowAnimatorController;

    [Header("Ground Shadow")]
    [SerializeField] private bool showGroundShadow = true;
    [SerializeField] private Vector3 groundShadowLocalPosition = new Vector3(0f, -0.145f, 0f);
    [SerializeField] private Vector3 groundShadowLocalScale = new Vector3(0.32f, 0.12f, 1f);
    [SerializeField] private Color groundShadowColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private int groundShadowOrderOffset = -1;
    
    private PlayerControls playerControls;
    private Vector2 movement;
    private Vector2 lastAimDirection = Vector2.right;
    private Vector2 lookDirection = Vector2.down;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;
    private Knockback knockback;
    private PlayerStats playerStats;
    private bool loggedMissingAnimatorController;
    private AnimationProfile animationProfile = AnimationProfile.Default;
    private SpriteRenderer groundShadowRenderer;
    private static Sprite groundShadowSprite;

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
        EnsureAnimatorController();
        mySpriteRender = GetComponent<SpriteRenderer>();
        knockback = GetComponent<Knockback>();
        playerStats = GetComponent<PlayerStats>();

        EnsureAimPivot();
        EnsureGroundShadow();
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
        if (showGroundShadow && groundShadowRenderer == null)
        {
            EnsureGroundShadow();
        }

        // 1. Cập nhật Input ngay lập tức để có hướng mới nhất cho Dash
        PlayerInput();
        UpdateAimDirection();
        UpdateGroundShadowSorting();

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

        if (CanPlayAnimator())
        {
            myAnimator.SetFloat("moveX", movement.x);
            myAnimator.SetFloat("moveY", movement.y);
            myAnimator.SetBool("isMoving", movement.sqrMagnitude > 0.01f);
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

    private void EnsureGroundShadow()
    {
        if (!showGroundShadow || mySpriteRender == null)
        {
            return;
        }

        Transform existingShadow = transform.Find("PlayerShadow");
        GameObject shadowObject = existingShadow != null ? existingShadow.gameObject : new GameObject("PlayerShadow");
        shadowObject.layer = gameObject.layer;
        shadowObject.transform.SetParent(transform, false);
        shadowObject.transform.localPosition = groundShadowLocalPosition;
        shadowObject.transform.localRotation = Quaternion.identity;
        shadowObject.transform.localScale = groundShadowLocalScale;

        groundShadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if (groundShadowRenderer == null)
        {
            groundShadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        }

        groundShadowRenderer.sprite = GetGroundShadowSprite();
        groundShadowRenderer.color = groundShadowColor;
        groundShadowRenderer.flipX = false;
        groundShadowRenderer.flipY = false;
        groundShadowRenderer.enabled = true;
        UpdateGroundShadowSorting();
    }

    private void UpdateGroundShadowSorting()
    {
        if (groundShadowRenderer == null || mySpriteRender == null)
        {
            return;
        }

        groundShadowRenderer.sortingLayerID = mySpriteRender.sortingLayerID;
        groundShadowRenderer.sortingOrder = mySpriteRender.sortingOrder + groundShadowOrderOffset;
    }

    private static Sprite GetGroundShadowSprite()
    {
        if (groundShadowSprite != null)
        {
            return groundShadowSprite;
        }

        const int width = 64;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Generated_PlayerShadow"
        };
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(1f, 1f, 1f, 0f);
        Color opaque = Color.white;
        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        float radiusX = width * 0.46f;
        float radiusY = height * 0.38f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - center.x) / radiusX;
                float dy = (y - center.y) / radiusY;
                float distance = dx * dx + dy * dy;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                texture.SetPixel(x, y, Color.Lerp(transparent, opaque, alpha));
            }
        }

        texture.Apply();
        groundShadowSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        groundShadowSprite.name = "Generated_PlayerShadow";
        return groundShadowSprite;
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
        lookDirection = QuantizeCardinalDirection(lastAimDirection);

        if (CanPlayAnimator())
        {
            myAnimator.SetFloat("lookX", lookDirection.x);
            myAnimator.SetFloat("lookY", lookDirection.y);
        }

        // Use stable root center for hand/weapon anchoring without flipping the player sprite.
        Vector2 stableAimDirection = mouseWorldPosition - transform.position;
        if (Mathf.Abs(stableAimDirection.x) >= 0.01f)
        {
            FacingLeft = stableAimDirection.x < 0f;
        }
    }

    private Vector2 QuantizeCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x < 0f ? Vector2.left : Vector2.right;
        }

        return direction.y < 0f ? Vector2.down : Vector2.up;
    }

    private void EnsureAnimatorController()
    {
        if (myAnimator == null)
        {
            return;
        }

        RuntimeAnimatorController targetController = GetTargetAnimatorController();
        if (targetController != null)
        {
            if (myAnimator.runtimeAnimatorController != targetController)
            {
                myAnimator.runtimeAnimatorController = targetController;
                ReapplyAnimatorParameters();
            }

            return;
        }

        if (myAnimator.runtimeAnimatorController != null)
        {
            return;
        }

        if (!loggedMissingAnimatorController)
        {
            loggedMissingAnimatorController = true;
            Debug.LogWarning("PlayerMovement: Animator has no controller assigned and Resources/Player.controller could not be loaded.", this);
        }
    }

    private bool CanPlayAnimator()
    {
        EnsureAnimatorController();
        return myAnimator != null && myAnimator.runtimeAnimatorController != null;
    }

    public void SetSwordAnimationProfile(bool useSwordProfile)
    {
        SetAnimationProfile(useSwordProfile ? AnimationProfile.Sword : AnimationProfile.Default);
    }

    public void SetAnimationProfile(AnimationProfile profile)
    {
        if (animationProfile == profile && CanPlayAnimator())
        {
            return;
        }

        animationProfile = profile;
        EnsureAnimatorController();
    }

    public void PlayAttackAnimation()
    {
        if (CanPlayAnimator() && HasAnimatorParameter("Attack", AnimatorControllerParameterType.Trigger))
        {
            myAnimator.ResetTrigger("Attack");
            myAnimator.SetTrigger("Attack");
        }
    }

    public void PlayShootAnimation()
    {
        if (CanPlayAnimator() && HasAnimatorParameter("Shoot", AnimatorControllerParameterType.Trigger))
        {
            myAnimator.ResetTrigger("Shoot");
            myAnimator.SetTrigger("Shoot");
        }
    }

    private RuntimeAnimatorController GetTargetAnimatorController()
    {
        RuntimeAnimatorController baseController = GetBaseAnimatorController();
        switch (animationProfile)
        {
            case AnimationProfile.Sword:
                if (swordAnimatorController == null)
                {
                    swordAnimatorController = Resources.Load<RuntimeAnimatorController>(SwordAnimatorControllerResourcePath);
                }

                return swordAnimatorController != null ? swordAnimatorController : baseController;

            case AnimationProfile.Bow:
                if (bowAnimatorController == null)
                {
                    bowAnimatorController = Resources.Load<RuntimeAnimatorController>(BowAnimatorControllerResourcePath);
                }

                return bowAnimatorController != null ? bowAnimatorController : baseController;

            default:
                return baseController;
        }
    }

    private RuntimeAnimatorController GetBaseAnimatorController()
    {
        if (playerAnimatorController != null)
        {
            return playerAnimatorController;
        }

        playerAnimatorController = Resources.Load<RuntimeAnimatorController>(PlayerAnimatorControllerResourcePath);
        if (playerAnimatorController != null)
        {
            return playerAnimatorController;
        }

        return myAnimator != null ? myAnimator.runtimeAnimatorController : null;
    }

    private void ReapplyAnimatorParameters()
    {
        if (myAnimator == null || myAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        if (HasAnimatorParameter("moveX", AnimatorControllerParameterType.Float))
        {
            myAnimator.SetFloat("moveX", movement.x);
        }

        if (HasAnimatorParameter("moveY", AnimatorControllerParameterType.Float))
        {
            myAnimator.SetFloat("moveY", movement.y);
        }

        if (HasAnimatorParameter("isMoving", AnimatorControllerParameterType.Bool))
        {
            myAnimator.SetBool("isMoving", movement.sqrMagnitude > 0.01f);
        }

        if (HasAnimatorParameter("lookX", AnimatorControllerParameterType.Float))
        {
            myAnimator.SetFloat("lookX", lookDirection.x);
        }

        if (HasAnimatorParameter("lookY", AnimatorControllerParameterType.Float))
        {
            myAnimator.SetFloat("lookY", lookDirection.y);
        }
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (myAnimator == null || myAnimator.runtimeAnimatorController == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in myAnimator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

}
