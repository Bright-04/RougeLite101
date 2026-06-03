using UnityEngine;

public class SlimeAI : MonoBehaviour
{
    private enum State
    {
        Idle,
        Wander,
        Alert,
        Chase,
        Reposition,
        AttackWindup,
        AttackCommit,
        AttackRecover,
        HurtStunned,
        Dead
    }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float loseInterestRange = 7f;
    [SerializeField] private float attackRange = 0.9f;
    [SerializeField] private float attackExitRange = 1.3f;

    [Header("Thinking")]
    [SerializeField] private float minThinkInterval = 0.35f;
    [SerializeField] private float maxThinkInterval = 0.7f;
    [SerializeField] private float pathRefreshIntervalMin = 0.25f;
    [SerializeField] private float pathRefreshIntervalMax = 0.45f;
    [SerializeField] private float facingLockDuration = 0.22f;
    [SerializeField] private float moveIntentLockDuration = 0.28f;

    [Header("Idle / Wander")]
    [SerializeField] private float roamRadius = 2.5f;
    [SerializeField] private float idleDurationMin = 1f;
    [SerializeField] private float idleDurationMax = 3f;
    [SerializeField] private float wanderDurationMin = 1.5f;
    [SerializeField] private float wanderDurationMax = 4f;
    [SerializeField] private float wanderSpeedMultiplier = 0.62f;

    [Header("Alert / Chase")]
    [SerializeField] private float minAggroReactionDelay = 0.35f;
    [SerializeField] private float maxAggroReactionDelay = 0.8f;
    [SerializeField] private float chaseSpeedMultiplier = 0.82f;

    [Header("Reposition")]
    [SerializeField] private float repositionDurationMin = 0.55f;
    [SerializeField] private float repositionDurationMax = 1.1f;
    [SerializeField] private float repositionDistanceMin = 0.75f;
    [SerializeField] private float repositionDistanceMax = 1.65f;
    [SerializeField] private float repositionSpeedMultiplier = 0.72f;
    [SerializeField] private float crowdingRange = 0.7f;
    [SerializeField] private float blockedDuration = 0.35f;
    [SerializeField] private float blockedMoveThreshold = 0.025f;
    [SerializeField] private float chaseRepositionChance = 0.18f;
    [SerializeField] private float hurtRepositionChance = 0.7f;

    [Header("Attack")]
    [SerializeField] private float attackWindupDuration = 0.35f;
    [SerializeField] private float attackCommitDuration = 0.2f;
    [SerializeField] private float attackRecoverDuration = 0.65f;
    [SerializeField] private float attackCooldown = 1.25f;

    [Header("Hurt")]
    [SerializeField] private float hurtStunDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private State state;
    private SlimePathFinding slimePathFinding;
    private SlimeForestAnimatorDriver animatorDriver;
    private EnemyDeathNotifier deathNotifier;
    private Flash flash;
    private Transform playerTransform;
    private Vector2 roamAnchor;
    private Vector2 moveIntentDirection = Vector2.down;
    private Vector2 facingDirection = Vector2.down;
    private Vector2 previousPosition;
    private float stateEndTime;
    private float nextThinkTime;
    private float nextPathRefreshTime;
    private float nextAttackTime;
    private float facingLockEndTime;
    private float moveIntentLockEndTime;
    private float blockedTimer;

    public Vector2 MoveIntentDirection => moveIntentDirection;
    public Vector2 FacingDirection => facingDirection;
    public int FacingIndex => ResolveFacingIndex(facingDirection);
    public float AnimationSpeed => GetAnimationSpeed();
    public bool HasStableFacing => facingDirection.sqrMagnitude > 0.0001f;
    public bool IsDead => state == State.Dead;

    private void Awake()
    {
        slimePathFinding = GetComponent<SlimePathFinding>();
        animatorDriver = GetComponent<SlimeForestAnimatorDriver>();
        deathNotifier = GetComponent<EnemyDeathNotifier>();
        flash = GetComponent<Flash>();
        roamAnchor = transform.position;
        previousPosition = transform.position;
        state = State.Idle;
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

    private void Start()
    {
        TryFindPlayer();
        EnterIdle();
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
        if (slimePathFinding == null || state == State.Dead)
        {
            return;
        }

        if (playerTransform == null)
        {
            TryFindPlayer();
        }

        UpdateBlockedStatus();

        float distanceToPlayer = GetDistanceToPlayer();

        switch (state)
        {
            case State.Idle:
                UpdateIdle(distanceToPlayer);
                break;
            case State.Wander:
                UpdateWander(distanceToPlayer);
                break;
            case State.Alert:
                UpdateAlert(distanceToPlayer);
                break;
            case State.Chase:
                UpdateChase(distanceToPlayer);
                break;
            case State.Reposition:
                UpdateReposition(distanceToPlayer);
                break;
            case State.AttackWindup:
                UpdateAttackWindup(distanceToPlayer);
                break;
            case State.AttackCommit:
                UpdateAttackCommit(distanceToPlayer);
                break;
            case State.AttackRecover:
                UpdateAttackRecover(distanceToPlayer);
                break;
            case State.HurtStunned:
                UpdateHurt(distanceToPlayer);
                break;
        }

        previousPosition = transform.position;
    }

    private void UpdateIdle(float distanceToPlayer)
    {
        StopMoving();

        if (CanSeePlayer(distanceToPlayer))
        {
            EnterAlert();
            return;
        }

        if (Time.time >= nextThinkTime)
        {
            ScheduleNextThink();
            MaybeTurnIdleFacing();
        }

        if (Time.time >= stateEndTime)
        {
            EnterWander();
        }
    }

    private void UpdateWander(float distanceToPlayer)
    {
        if (CanSeePlayer(distanceToPlayer))
        {
            EnterAlert();
            return;
        }

        if (Time.time >= nextThinkTime)
        {
            ScheduleNextThink();
            if (IsBlocked())
            {
                EnterIdle();
                return;
            }
        }

        if (Time.time >= nextPathRefreshTime || !slimePathFinding.HasTarget)
        {
            PickWanderTarget();
        }

        UpdateMovementIntentFromTarget();

        if (Time.time >= stateEndTime || ReachedCurrentTarget())
        {
            EnterIdle();
        }
    }

    private void UpdateAlert(float distanceToPlayer)
    {
        StopMoving();
        FacePlayer(true);

        if (playerTransform == null || distanceToPlayer > loseInterestRange)
        {
            EnterIdle();
            return;
        }

        if (Time.time >= stateEndTime)
        {
            EnterChase();
        }
    }

    private void UpdateChase(float distanceToPlayer)
    {
        if (playerTransform == null || distanceToPlayer > loseInterestRange)
        {
            roamAnchor = transform.position;
            EnterIdle();
            return;
        }

        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            EnterAttackWindup();
            return;
        }

        if (Time.time >= nextThinkTime)
        {
            ScheduleNextThink();
            if (ShouldReposition(distanceToPlayer))
            {
                EnterReposition();
                return;
            }
        }

        if (Time.time >= nextPathRefreshTime || !slimePathFinding.HasTarget)
        {
            RefreshChaseTarget();
        }

        UpdateMovementIntentTowardsPlayer();
    }

    private void UpdateReposition(float distanceToPlayer)
    {
        if (playerTransform == null || distanceToPlayer > loseInterestRange)
        {
            EnterIdle();
            return;
        }

        if (Time.time >= nextPathRefreshTime || !slimePathFinding.HasTarget)
        {
            PickRepositionTarget();
        }

        UpdateMovementIntentFromTarget();

        if (Time.time >= stateEndTime || ReachedCurrentTarget() || IsBlocked())
        {
            EnterChase();
        }
    }

    private void UpdateAttackWindup(float distanceToPlayer)
    {
        StopMoving();
        FacePlayer(true);

        if (playerTransform == null || distanceToPlayer > attackExitRange)
        {
            EnterAttackRecover();
            return;
        }

        if (Time.time >= stateEndTime)
        {
            EnterAttackCommit();
        }
    }

    private void UpdateAttackCommit(float distanceToPlayer)
    {
        StopMoving();
        FacePlayer(true);

        if (playerTransform == null || distanceToPlayer > attackExitRange)
        {
            EnterAttackRecover();
            return;
        }

        if (Time.time >= stateEndTime)
        {
            EnterAttackRecover();
        }
    }

    private void UpdateAttackRecover(float distanceToPlayer)
    {
        StopMoving();

        if (Time.time < stateEndTime)
        {
            if (CanUsePlayer(distanceToPlayer))
            {
                FacePlayer(false);
            }

            return;
        }

        if (!CanUsePlayer(distanceToPlayer))
        {
            EnterIdle();
            return;
        }

        if (distanceToPlayer <= crowdingRange)
        {
            EnterReposition();
            return;
        }

        EnterChase();
    }

    private void UpdateHurt(float distanceToPlayer)
    {
        StopMoving();

        if (Time.time < stateEndTime)
        {
            return;
        }

        if (!CanUsePlayer(distanceToPlayer))
        {
            EnterIdle();
            return;
        }

        if (Random.value <= hurtRepositionChance)
        {
            EnterReposition();
            return;
        }

        EnterAlert();
    }

    private void EnterIdle()
    {
        SetState(State.Idle);
        StopMoving();
        stateEndTime = Time.time + Random.Range(idleDurationMin, idleDurationMax);
        ScheduleNextThink();
        SetMoveIntent(Vector2.zero, true);
        if (Random.value < 0.35f)
        {
            MaybeTurnIdleFacing();
        }
    }

    private void EnterWander()
    {
        SetState(State.Wander);
        roamAnchor = transform.position;
        stateEndTime = Time.time + Random.Range(wanderDurationMin, wanderDurationMax);
        ScheduleNextThink();
        PickWanderTarget();
    }

    private void EnterAlert()
    {
        SetState(State.Alert);
        StopMoving();
        stateEndTime = Time.time + Random.Range(minAggroReactionDelay, maxAggroReactionDelay);
        ScheduleNextThink();
        FacePlayer(true);
    }

    private void EnterChase()
    {
        SetState(State.Chase);
        ScheduleNextThink();
        RefreshChaseTarget(true);
    }

    private void EnterReposition()
    {
        SetState(State.Reposition);
        stateEndTime = Time.time + Random.Range(repositionDurationMin, repositionDurationMax);
        ScheduleNextThink();
        PickRepositionTarget(true);
    }

    private void EnterAttackWindup()
    {
        SetState(State.AttackWindup);
        StopMoving();
        FacePlayer(true, attackWindupDuration + attackCommitDuration);
        stateEndTime = Time.time + attackWindupDuration;
        nextAttackTime = stateEndTime + attackCommitDuration + attackRecoverDuration + attackCooldown;
        animatorDriver?.TriggerAttack();
    }

    private void EnterAttackCommit()
    {
        SetState(State.AttackCommit);
        StopMoving();
        stateEndTime = Time.time + attackCommitDuration;
    }

    private void EnterAttackRecover()
    {
        SetState(State.AttackRecover);
        StopMoving();
        stateEndTime = Time.time + attackRecoverDuration;
    }

    private void EnterHurt()
    {
        if (state == State.Dead)
        {
            return;
        }

        SetState(State.HurtStunned);
        StopMoving();
        stateEndTime = Time.time + hurtStunDuration;
        SetMoveIntent(Vector2.zero, true);
    }

    private void EnterDead()
    {
        SetState(State.Dead);
        StopMoving();
        SetMoveIntent(Vector2.zero, true);
    }

    private void SetState(State newState)
    {
        if (state == newState)
        {
            return;
        }

        state = newState;
        blockedTimer = 0f;
        LogStateChange(newState);
    }

    private void PickWanderTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        Vector2 targetPosition = roamAnchor + randomOffset;
        SetMoveTarget(targetPosition, wanderSpeedMultiplier, targetPosition - (Vector2)transform.position, true);
    }

    private void RefreshChaseTarget(bool forceIntentUpdate = false)
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector2 targetPosition = playerTransform.position;
        SetMoveTarget(targetPosition, chaseSpeedMultiplier, targetPosition - (Vector2)transform.position, forceIntentUpdate);
    }

    private void PickRepositionTarget(bool forceIntentUpdate = false)
    {
        if (playerTransform == null)
        {
            EnterChase();
            return;
        }

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        if (toPlayer == Vector2.zero)
        {
            toPlayer = facingDirection;
        }

        Vector2 side = Random.value < 0.5f ? new Vector2(-toPlayer.y, toPlayer.x) : new Vector2(toPlayer.y, -toPlayer.x);
        float lateralDistance = Random.Range(repositionDistanceMin, repositionDistanceMax);
        float backoffDistance = Random.Range(0.15f, 0.5f);
        Vector2 targetPosition = (Vector2)transform.position + side * lateralDistance - toPlayer * backoffDistance;
        SetMoveTarget(targetPosition, repositionSpeedMultiplier, targetPosition - (Vector2)transform.position, forceIntentUpdate);
    }

    private void SetMoveTarget(Vector2 targetPosition, float speedMultiplier, Vector2 desiredIntent, bool forceIntentUpdate = false)
    {
        slimePathFinding.SetSpeedMultiplier(speedMultiplier);
        slimePathFinding.MoveTo(targetPosition);
        nextPathRefreshTime = Time.time + Random.Range(pathRefreshIntervalMin, pathRefreshIntervalMax);
        SetMoveIntent(desiredIntent, forceIntentUpdate);
    }

    private void StopMoving()
    {
        slimePathFinding.SetSpeedMultiplier(1f);
        slimePathFinding.StopMoving();
    }

    private void SetMoveIntent(Vector2 desiredIntent, bool force)
    {
        if (desiredIntent.sqrMagnitude <= 0.0001f)
        {
            moveIntentDirection = Vector2.zero;
            return;
        }

        if (!force && Time.time < moveIntentLockEndTime)
        {
            return;
        }

        moveIntentDirection = desiredIntent.normalized;
        moveIntentLockEndTime = Time.time + moveIntentLockDuration;
        SetFacing(moveIntentDirection, force);
    }

    private void UpdateMovementIntentTowardsPlayer()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector2 desiredDirection = (Vector2)playerTransform.position - (Vector2)transform.position;
        SetMoveIntent(desiredDirection, false);
    }

    private void UpdateMovementIntentFromTarget()
    {
        if (!slimePathFinding.HasTarget)
        {
            return;
        }

        Vector2 desiredDirection = slimePathFinding.CurrentTargetPosition - (Vector2)transform.position;
        SetMoveIntent(desiredDirection, false);
    }

    private void MaybeTurnIdleFacing()
    {
        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        SetFacing(randomDirection, false);
    }

    private void FacePlayer(bool force, float customLockDuration = -1f)
    {
        if (playerTransform == null)
        {
            return;
        }

        SetFacing((Vector2)playerTransform.position - (Vector2)transform.position, force, customLockDuration);
    }

    private void SetFacing(Vector2 desiredDirection, bool force, float customLockDuration = -1f)
    {
        if (desiredDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        if (!force && Time.time < facingLockEndTime)
        {
            return;
        }

        Vector2 newFacing = QuantizeCardinal(desiredDirection);
        if (!force && newFacing == facingDirection)
        {
            return;
        }

        facingDirection = newFacing;
        float lockDuration = customLockDuration > 0f ? customLockDuration : facingLockDuration;
        facingLockEndTime = Time.time + lockDuration;
    }

    private bool ShouldReposition(float distanceToPlayer)
    {
        if (distanceToPlayer <= crowdingRange)
        {
            return true;
        }

        if (IsBlocked())
        {
            return true;
        }

        return Random.value <= chaseRepositionChance;
    }

    private bool CanSeePlayer(float distanceToPlayer)
    {
        return playerTransform != null && distanceToPlayer <= detectionRange;
    }

    private bool CanUsePlayer(float distanceToPlayer)
    {
        return playerTransform != null && distanceToPlayer <= loseInterestRange;
    }

    private bool ReachedCurrentTarget()
    {
        if (!slimePathFinding.HasTarget)
        {
            return true;
        }

        return Vector2.Distance(transform.position, slimePathFinding.CurrentTargetPosition) <= 0.2f;
    }

    private float GetDistanceToPlayer()
    {
        if (playerTransform == null)
        {
            return float.PositiveInfinity;
        }

        return Vector2.Distance(transform.position, playerTransform.position);
    }

    private void UpdateBlockedStatus()
    {
        bool shouldTrackMovement = state == State.Wander || state == State.Chase || state == State.Reposition;
        if (!shouldTrackMovement || !slimePathFinding.HasTarget)
        {
            blockedTimer = 0f;
            return;
        }

        float movedDistance = Vector2.Distance(previousPosition, transform.position);
        if (movedDistance <= blockedMoveThreshold)
        {
            blockedTimer += Time.deltaTime;
        }
        else
        {
            blockedTimer = 0f;
        }
    }

    private bool IsBlocked()
    {
        return blockedTimer >= blockedDuration;
    }

    private void ScheduleNextThink()
    {
        nextThinkTime = Time.time + Random.Range(minThinkInterval, maxThinkInterval);
    }

    private void TryFindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private float GetAnimationSpeed()
    {
        switch (state)
        {
            case State.Wander:
            case State.Chase:
            case State.Reposition:
                return slimePathFinding != null ? slimePathFinding.BaseMoveSpeed * slimePathFinding.SpeedMultiplier : 0f;
            default:
                return 0f;
        }
    }

    private void OnStartedFlashing()
    {
        if (state != State.Dead)
        {
            EnterHurt();
        }
    }

    private void OnDied(EnemyDeathNotifier _)
    {
        EnterDead();
    }

    private void LogStateChange(State newState)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[SlimeAI] {name} -> {newState}", this);
    }

    private static Vector2 QuantizeCardinal(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x < 0f ? Vector2.left : Vector2.right;
        }

        return direction.y < 0f ? Vector2.down : Vector2.up;
    }

    private static int ResolveFacingIndex(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x < 0f ? 1 : 2;
        }

        return direction.y < 0f ? 0 : 3;
    }
}
