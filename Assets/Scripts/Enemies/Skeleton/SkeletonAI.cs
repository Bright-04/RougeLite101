using System.Collections;
using UnityEngine;

public class SkeletonAI : MonoBehaviour
{
    private enum State
    {
        Idle,
        Walk,
        Attack
    }

    [Header("Detection")]
    [SerializeField] private float detectRange = 6f;

    [Header("Routine Time")]
    [SerializeField] private float idleTime = 1f;
    [SerializeField] private float walkTime = 2f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [Header("Test")]
    [SerializeField] private Transform bow;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private SkeletonAttackBase attackBehaviour;
    private Animator bowAnimator;
    private Vector2 moveDirection;
    private bool playerDetected;
    private State currentState;

    public bool IsAttacking => currentState == State.Attack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        attackBehaviour = GetComponent<SkeletonAttackBase>();
        if(bow != null)
        {
            bowAnimator = bow.GetComponent<Animator>();
            Debug.Log("Bố mày gay" +  bowAnimator);
        }
    }

    private void Start()
    {
        player = PlayerMovement.Instance.transform;
        StartCoroutine(AIRoutine());
    }

    private void Update()
    {
        if (player == null) return;

        if (!playerDetected)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= detectRange)
                playerDetected = true;
        }

        if (playerDetected)
            FacePlayer();
    }

    private void FixedUpdate()
    {
        if (currentState == State.Walk)
        {
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private IEnumerator AIRoutine()
    {
        while (true)
        {
            currentState = State.Idle;
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            if (bowAnimator != null)
            {
                bowAnimator.SetBool("IsMoving", false);
            }
            yield return new WaitForSeconds(idleTime);

            currentState = State.Walk;
            moveDirection = Random.insideUnitCircle.normalized;
            animator.SetBool("IsMoving", true);
            if (bowAnimator != null)
            {
                bowAnimator.SetBool("IsMoving", true);
            }

            yield return new WaitForSeconds(walkTime);

            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);

            if (playerDetected && player != null)
            {
                currentState = State.Attack;

                yield return StartCoroutine(attackBehaviour.AttackRoutine(player));

                attackBehaviour.FinishAttackAnimation();

                yield return new WaitForSeconds(attackBehaviour.AttackCooldown);
            }
        }
    }

    private void FacePlayer()
    {
        if (player == null || spriteRenderer == null) return;

        spriteRenderer.flipX = player.position.x < transform.position.x;
    }
}