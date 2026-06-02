using UnityEngine;

public class SlimeAI : MonoBehaviour
{
    private enum State
    {
        Roaming,
        Chasing,
        AttackWindup
    }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float loseInterestRange = 7f;
    [SerializeField] private float attackRange = 0.9f;

    [Header("Roaming")]
    [SerializeField] private float roamRadius = 2.5f;
    [SerializeField] private float roamDecisionIntervalMin = 1.5f;
    [SerializeField] private float roamDecisionIntervalMax = 3f;

    [Header("Attack")]
    [SerializeField] private float attackWindupDuration = 0.35f;
    [SerializeField] private float attackCooldown = 1.25f;

    private State state;
    private SlimePathFinding slimePathFinding;
    private SlimeForestAnimatorDriver animatorDriver;
    private Transform playerTransform;
    private Vector2 roamAnchor;
    private float nextStateDecisionTime;
    private float attackWindupEndTime;
    private float nextAttackTime;

    private void Awake()
    {
        slimePathFinding = GetComponent<SlimePathFinding>();
        animatorDriver = GetComponent<SlimeForestAnimatorDriver>();
        roamAnchor = transform.position;
        state = State.Roaming;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        PickRoamTarget();
    }

    private void Update()
    {
        if (slimePathFinding == null)
        {
            return;
        }

        if (playerTransform == null)
        {
            TryFindPlayer();
            HandleRoaming(true);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        switch (state)
        {
            case State.Roaming:
                if (distanceToPlayer <= detectionRange)
                {
                    state = State.Chasing;
                }
                else
                {
                    HandleRoaming(false);
                }
                break;

            case State.Chasing:
                if (distanceToPlayer > loseInterestRange)
                {
                    state = State.Roaming;
                    roamAnchor = transform.position;
                    PickRoamTarget();
                }
                else if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
                {
                    state = State.AttackWindup;
                    attackWindupEndTime = Time.time + attackWindupDuration;
                    nextAttackTime = attackWindupEndTime + attackCooldown;
                    slimePathFinding.StopMoving();
                    animatorDriver?.TriggerAttack();
                }
                else
                {
                    slimePathFinding.MoveTo(playerTransform.position);
                }
                break;

            case State.AttackWindup:
                slimePathFinding.StopMoving();
                if (distanceToPlayer > loseInterestRange)
                {
                    state = State.Roaming;
                    roamAnchor = transform.position;
                    PickRoamTarget();
                }
                else if (Time.time >= attackWindupEndTime)
                {
                    state = distanceToPlayer <= detectionRange ? State.Chasing : State.Roaming;
                    if (state == State.Roaming)
                    {
                        roamAnchor = transform.position;
                        PickRoamTarget();
                    }
                }
                break;
        }
    }

    private void HandleRoaming(bool forceUpdate)
    {
        if (forceUpdate || Time.time >= nextStateDecisionTime || !slimePathFinding.HasTarget)
        {
            PickRoamTarget();
        }
    }

    private void PickRoamTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        Vector2 targetPosition = roamAnchor + randomOffset;
        slimePathFinding.MoveTo(targetPosition);
        nextStateDecisionTime = Time.time + Random.Range(roamDecisionIntervalMin, roamDecisionIntervalMax);
    }

    private void TryFindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }
}
