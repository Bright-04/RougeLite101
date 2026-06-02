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
    private int currentFacing;
    private bool isDead;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        deathNotifier = GetComponent<EnemyDeathNotifier>();
        flash = GetComponent<Flash>();
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
        if (animator == null || rb == null)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        if (speed > movementThreshold)
        {
            currentFacing = ResolveFacing(velocity);
        }

        animator.SetFloat(SpeedParameter, speed);
        animator.SetInteger(FacingParameter, currentFacing);
        animator.SetBool(DeadParameter, isDead);
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

    private static int ResolveFacing(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x < 0f ? 1 : 2;
        }

        return direction.y < 0f ? 0 : 3;
    }
}
