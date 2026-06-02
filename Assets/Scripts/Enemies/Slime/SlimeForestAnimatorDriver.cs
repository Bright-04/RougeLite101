using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyDeathNotifier))]
public class SlimeForestAnimatorDriver : MonoBehaviour
{
    private const string SpeedParameter = "Speed";
    private const string FacingParameter = "Facing";
    private const string AttackParameter = "Attack";
    private const string HurtParameter = "Hurt";
    private const string DeadParameter = "Dead";

    [SerializeField] private float movementThreshold = 0.05f;

    private Animator animator;
    private Rigidbody2D rb;
    private EnemyDeathNotifier deathNotifier;
    private Flash flash;
    private SlimePathFinding slimePathFinding;
    private Vector3 previousPosition;
    private int currentFacing;
    private bool isDead;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        deathNotifier = GetComponent<EnemyDeathNotifier>();
        flash = GetComponent<Flash>();
        slimePathFinding = GetComponent<SlimePathFinding>();
        previousPosition = transform.position;
    }

    private void OnEnable()
    {
        if (deathNotifier != null)
        {
            deathNotifier.Died += OnDied;
        }

        if (flash != null)
        {
            flash.StartedFlashing += OnStartedFlashing;
        }

        previousPosition = transform.position;
    }

    private void OnDisable()
    {
        if (deathNotifier != null)
        {
            deathNotifier.Died -= OnDied;
        }

        if (flash != null)
        {
            flash.StartedFlashing -= OnStartedFlashing;
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        Vector2 motion = GetMotionVector();
        float speed = GetSpeedFromMotion(motion);
        if (speed > movementThreshold)
        {
            currentFacing = ResolveFacing(motion);
        }

        animator.SetFloat(SpeedParameter, speed);
        animator.SetInteger(FacingParameter, currentFacing);
        animator.SetBool(DeadParameter, isDead);
        previousPosition = transform.position;
    }

    public void TriggerAttack()
    {
        if (animator == null || isDead)
        {
            return;
        }

        animator.ResetTrigger(HurtParameter);
        animator.SetTrigger(AttackParameter);
    }

    private void OnStartedFlashing()
    {
        if (animator == null || isDead)
        {
            return;
        }

        animator.ResetTrigger(AttackParameter);
        animator.SetTrigger(HurtParameter);
    }

    private void OnDied(EnemyDeathNotifier _)
    {
        isDead = true;
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(AttackParameter);
        animator.ResetTrigger(HurtParameter);
        animator.SetBool(DeadParameter, true);
    }

    private Vector2 GetMotionVector()
    {
        if (slimePathFinding != null && slimePathFinding.IsMoving)
        {
            return slimePathFinding.CurrentMoveDirection * slimePathFinding.CurrentMoveSpeed;
        }

        if (rb != null && rb.linearVelocity.sqrMagnitude > movementThreshold * movementThreshold)
        {
            return rb.linearVelocity;
        }

        Vector2 positionDelta = ((Vector2)transform.position - (Vector2)previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        if (positionDelta.sqrMagnitude > movementThreshold * movementThreshold)
        {
            return positionDelta;
        }

        return Vector2.zero;
    }

    private float GetSpeedFromMotion(Vector2 motion)
    {
        if (slimePathFinding != null && slimePathFinding.IsMoving)
        {
            return slimePathFinding.CurrentMoveSpeed;
        }

        return motion.magnitude;
    }

    private static int ResolveFacing(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x < 0f ? 1 : 2;
        }

        return direction.y < 0f ? 0 : 3;
    }
}
