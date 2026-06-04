using UnityEngine;
using UnityEngine.Serialization;

public class SlimeAI : MonoBehaviour
{
    private enum BehaviorPreset
    {
        Basic,
        Ice,
        Magma
    }

    private enum State
    {
        Idle,
        Wander,
        Alert,
        CombatMove,
        Attack,
        Recover,
        Stuck,
        Hurt,
        Dead
    }

    private enum AttackPhase
    {
        None,
        Windup,
        Hit
    }

    [Header("Awareness")]
    [SerializeField] private BehaviorPreset preset = BehaviorPreset.Basic;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float loseInterestRange = 7f;
    [SerializeField] private float aggroMemoryDuration = 3.5f;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private float lostSightGraceTime = 0.8f;

    [Header("Advanced")]
    [SerializeField] private float minThinkInterval = 0.35f;
    [SerializeField] private float maxThinkInterval = 0.7f;
    [HideInInspector]
    [SerializeField] private float pathRefreshIntervalMin = 0.25f;
    [HideInInspector]
    [SerializeField] private float pathRefreshIntervalMax = 0.45f;
    [HideInInspector]
    [SerializeField] private float facingLockDuration = 0.22f;
    [HideInInspector]
    [SerializeField] private float moveIntentLockDuration = 0.28f;

    [Header("Wander")]
    [FormerlySerializedAs("roamRadius")]
    [SerializeField] private float wanderRadius = 2.5f;
    [HideInInspector]
    [SerializeField] private int walkablePointAttempts = 8;
    [SerializeField] private float idleDurationMin = 1f;
    [SerializeField] private float idleDurationMax = 3f;
    [SerializeField] private float wanderDurationMin = 1.5f;
    [SerializeField] private float wanderDurationMax = 4f;
    [SerializeField] private float wanderSpeedMultiplier = 0.62f;

    [Header("Combat Movement")]
    [SerializeField] private float minAggroReactionDelay = 0.35f;
    [SerializeField] private float maxAggroReactionDelay = 0.8f;
    [SerializeField] private float chaseSpeedMultiplier = 0.82f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackExitRange = 1.55f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackHitRadius = 0.7f;
    [SerializeField] private Vector2 attackHitOffset = new Vector2(0f, 0.45f);
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float attackKnockbackForce = 10f;
    [SerializeField] private float attackWindupDuration = 0.35f;
    [SerializeField] private float attackCommitDuration = 0.2f;
    [SerializeField] private float attackRecoverDuration = 0.65f;
    [SerializeField] private float attackCooldown = 1.25f;

    [Header("Stuck Handling")]
    [SerializeField] private float blockedDuration = 1f;
    [HideInInspector]
    [SerializeField] private float blockedMoveThreshold = 0.01f;
    [SerializeField] private float minCombatMoveBeforeStuck = 0.6f;
    [SerializeField] private float stuckDuration = 0.5f;
    [SerializeField] private float stuckCooldown = 1.5f;
    [HideInInspector]
    [SerializeField] private float stuckResolveRadius = 0.9f;
    [SerializeField] private float stuckSpeedMultiplier = 0.68f;

    [Header("Hurt")]
    [SerializeField] private float hurtStunDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool debugStateTransitions = true;
    [SerializeField] private bool debugPerception = false;
    [SerializeField] private bool debugStuck = false;
    [SerializeField] private bool debugAttack = false;
    [SerializeField] private bool drawGizmos = true;
    [HideInInspector]
    [SerializeField] private float spawnSnapRadius = 1.5f;
    [HideInInspector]
    [SerializeField] private float targetSnapRadius = 1.2f;

    private State state;
    private AttackPhase attackPhase;
    private SlimePathFinding slimePathFinding;
    private SlimeForestAnimatorDriver animatorDriver;
    private DungeonNavigationProvider navigationProvider;
    private EnemyDeathNotifier deathNotifier;
    private Flash flash;
    private Rigidbody2D rb;
    private Transform playerTransform;
    private Vector2 wanderAnchor;
    private Vector2 moveIntentDirection = Vector2.down;
    private Vector2 facingDirection = Vector2.down;
    private Vector2 previousPosition;
    private Vector2 lastKnownPlayerPosition;
    private float stateEndTime;
    private float nextThinkTime;
    private float nextPathRefreshTime;
    private float nextAttackTime;
    private float nextStuckAllowedTime;
    private float nextAggroLogTime;
    private float combatEngagedUntil;
    private float facingLockEndTime;
    private float moveIntentLockEndTime;
    private float blockedTimer;
    private float attackHitTime;
    private float stateEnterTime;
    private float lastSeenPlayerTime = float.NegativeInfinity;
    private float totalCombatMoveTime;
    private float totalStuckTime;
    private bool hasAppliedAttackDamage;
    private bool hadPlayerSightLastFrame;
    private int combatMoveToStuckCount;
    private int stuckExitCount;
    private int attackAttemptCount;
    private int attackHitCount;
    private int lostTargetCount;
    private string lastPursuitSource = "None";

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
        navigationProvider = ResolveNavigationProvider();
        deathNotifier = GetComponent<EnemyDeathNotifier>();
        flash = GetComponent<Flash>();
        rb = GetComponent<Rigidbody2D>();
        if (playerLayer == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        wanderAnchor = transform.position;
        previousPosition = transform.position;
        state = State.Idle;
        attackPhase = AttackPhase.None;
    }

    private void Reset()
    {
        ApplyPresetDefaults();
    }

    private void OnValidate()
    {
        loseInterestRange = Mathf.Max(loseInterestRange, detectionRange + 0.5f);
        aggroMemoryDuration = Mathf.Max(0.5f, aggroMemoryDuration);
        attackExitRange = Mathf.Max(attackExitRange, attackRange + 0.1f);
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
        if (playerTransform != null)
        {
            lastKnownPlayerPosition = playerTransform.position;
        }

        ValidateSpawnPosition();
        EnterIdle("Start");
    }

    [ContextMenu("Apply Preset Defaults")]
    private void ApplyPresetDefaults()
    {
        switch (preset)
        {
            case BehaviorPreset.Ice:
                detectionRange = 5.5f;
                loseInterestRange = 8.5f;
                aggroMemoryDuration = 4f;
                chaseSpeedMultiplier = 0.9f;
                attackRange = 1.2f;
                attackHitRadius = 0.7f;
                attackCooldown = 1.35f;
                break;
            case BehaviorPreset.Magma:
                detectionRange = 6.5f;
                loseInterestRange = 10f;
                aggroMemoryDuration = 4f;
                chaseSpeedMultiplier = 1.12f;
                attackRange = 1.25f;
                attackHitRadius = 0.75f;
                attackCooldown = 1.05f;
                break;
            default:
                detectionRange = 6f;
                loseInterestRange = 9f;
                aggroMemoryDuration = 3.5f;
                chaseSpeedMultiplier = 1f;
                attackRange = 1.2f;
                attackHitRadius = 0.7f;
                attackCooldown = 1.2f;
                break;
        }

        attackExitRange = Mathf.Max(attackExitRange, attackRange + 0.35f);
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

        float distanceToPlayer = GetDistanceToPlayer();
        UpdatePerception(distanceToPlayer);
        UpdateBlockedStatus();

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
            case State.CombatMove:
                UpdateCombatMove(distanceToPlayer);
                break;
            case State.Attack:
                UpdateAttack(distanceToPlayer);
                break;
            case State.Recover:
                UpdateRecover(distanceToPlayer);
                break;
            case State.Stuck:
                UpdateStuck(distanceToPlayer);
                break;
            case State.Hurt:
                UpdateHurt(distanceToPlayer);
                break;
        }

        previousPosition = transform.position;
    }

    private void UpdateIdle(float distanceToPlayer)
    {
        StopMoving();

        if (CanDetectPlayer(distanceToPlayer))
        {
            EnterAlert("SawPlayer");
            return;
        }

        if (Time.time >= nextThinkTime)
        {
            ScheduleNextThink();
            MaybeTurnIdleFacing();
        }

        if (Time.time >= stateEndTime)
        {
            EnterWander("IdleTimerExpired");
        }
    }

    private void UpdateWander(float distanceToPlayer)
    {
        if (CanDetectPlayer(distanceToPlayer))
        {
            EnterAlert("SawPlayer");
            return;
        }

        UpdateMovementIntentFromTarget();

        if (Time.time >= stateEndTime)
        {
            EnterIdle("WanderTimerExpired");
            return;
        }

        if (!slimePathFinding.HasTarget)
        {
            EnterIdle("NoWalkableTarget");
            return;
        }

        if (ReachedCurrentTarget())
        {
            EnterIdle("ReachedTarget");
            return;
        }

        if (IsBlocked())
        {
            EnterIdle("Blocked");
        }
    }

    private void UpdateAlert(float distanceToPlayer)
    {
        StopMoving();
        FaceLastKnownPlayer(true);

        if (playerTransform != null && distanceToPlayer <= loseInterestRange)
        {
            RefreshAggroMemory("AlertHold");
        }

        if (Time.time < stateEndTime)
        {
            return;
        }

        if (HasAggroMemory() && distanceToPlayer <= loseInterestRange)
        {
            EnterCombatMove("ReactionDelayFinished");
            return;
        }

        EnterIdle("LostPlayer");
    }

    private void UpdateCombatMove(float distanceToPlayer)
    {
        if (playerTransform != null && distanceToPlayer <= loseInterestRange)
        {
            RefreshAggroMemory("CombatMove");
        }

        if (CanAttackNow(distanceToPlayer))
        {
            EnterAttack("InAttackRange");
            return;
        }

        if (ShouldLoseTarget(distanceToPlayer, out string loseReason))
        {
            lostTargetCount++;
            LogPerception($"lost target reason={loseReason}");
            EnterIdle($"LostInterest:{loseReason}");
            return;
        }

        if (Time.time >= nextThinkTime)
        {
            ScheduleNextThink();
            if (ShouldEnterStuck(distanceToPlayer))
            {
                EnterStuck("MovementBlocked");
                return;
            }
        }

        if (Time.time >= nextPathRefreshTime || !slimePathFinding.HasTarget)
        {
            RefreshCombatMoveTarget();
        }

        UpdateMovementIntentFromTarget();
    }

    private void UpdateAttack(float distanceToPlayer)
    {
        StopMoving();

        switch (attackPhase)
        {
            case AttackPhase.Windup:
                if (playerTransform == null || distanceToPlayer > attackExitRange)
                {
                    LogAttack("attempt canceled target left range");
                    EnterRecover("TargetLeftAttackExitRange");
                    return;
                }

                if (Time.time >= stateEndTime)
                {
                    attackPhase = AttackPhase.Hit;
                    hasAppliedAttackDamage = false;
                    attackHitTime = Time.time + attackCommitDuration * 0.35f;
                    stateEndTime = Time.time + attackCommitDuration;
                    LogAttack("commit");
                }
                break;

            case AttackPhase.Hit:
                if (!hasAppliedAttackDamage && Time.time >= attackHitTime)
                {
                    TryApplyAttackDamage();
                }

                if (playerTransform == null || distanceToPlayer > attackExitRange)
                {
                    EnterRecover("TargetLeftAttackExitRange");
                    return;
                }

                if (Time.time >= stateEndTime)
                {
                    EnterRecover("CommitFinished");
                }
                break;
        }
    }

    private void UpdateRecover(float distanceToPlayer)
    {
        StopMoving();

        if (playerTransform != null && distanceToPlayer <= loseInterestRange)
        {
            RefreshAggroMemory("Recover");
        }

        if (Time.time < stateEndTime)
        {
            if (HasAggroMemory())
            {
                FaceLastKnownPlayer(false);
            }

            return;
        }

        if (HasAggroMemory() && distanceToPlayer <= loseInterestRange)
        {
            EnterCombatMove("RecoverFinished");
            return;
        }

        EnterIdle("RecoverFinishedLostPlayer");
    }

    private void UpdateStuck(float distanceToPlayer)
    {
        if (playerTransform != null && distanceToPlayer <= loseInterestRange)
        {
            RefreshAggroMemory("Stuck");
        }

        if (Time.time < stateEndTime)
        {
            UpdateMovementIntentFromTarget();
            return;
        }

        stuckExitCount++;
        if (HasAggroMemory() && distanceToPlayer <= loseInterestRange)
        {
            EnterCombatMove("StuckFinished");
            return;
        }

        EnterIdle("StuckFinishedLostPlayer");
    }

    private void UpdateHurt(float distanceToPlayer)
    {
        StopMoving();

        if (playerTransform != null && distanceToPlayer <= loseInterestRange)
        {
            RefreshAggroMemory("Hurt");
        }

        if (Time.time < stateEndTime)
        {
            return;
        }

        if (HasAggroMemory() && distanceToPlayer <= loseInterestRange)
        {
            EnterCombatMove("HurtRecovered");
            return;
        }

        EnterIdle("HurtRecoveredLostPlayer");
    }

    private void EnterIdle(string reason)
    {
        SetState(State.Idle, reason);
        StopMoving();
        attackPhase = AttackPhase.None;
        stateEndTime = Time.time + Random.Range(idleDurationMin, idleDurationMax);
        ScheduleNextThink();
        SetMoveIntent(Vector2.zero, true);
        if (Random.value < 0.35f)
        {
            MaybeTurnIdleFacing();
        }
    }

    private void EnterWander(string reason)
    {
        SetState(State.Wander, reason);
        wanderAnchor = GetCurrentWorldPosition();
        attackPhase = AttackPhase.None;
        stateEndTime = Time.time + Random.Range(wanderDurationMin, wanderDurationMax);
        ScheduleNextThink();
        PickWanderTarget();
    }

    private void EnterAlert(string reason)
    {
        SetState(State.Alert, reason);
        StopMoving();
        attackPhase = AttackPhase.None;
        RefreshAggroMemory("AlertEnter", true);
        stateEndTime = Time.time + Random.Range(minAggroReactionDelay, maxAggroReactionDelay);
        ScheduleNextThink();
        FaceLastKnownPlayer(true);
    }

    private void EnterCombatMove(string reason)
    {
        SetState(State.CombatMove, reason);
        attackPhase = AttackPhase.None;
        RefreshAggroMemory("CombatMoveEnter", true);
        ScheduleNextThink();
        RefreshCombatMoveTarget(true);
    }

    private void EnterAttack(string reason)
    {
        SetState(State.Attack, reason);
        StopMoving();
        attackPhase = AttackPhase.Windup;
        hasAppliedAttackDamage = false;
        attackAttemptCount++;
        RefreshAggroMemory("AttackEnter", true);
        FaceLastKnownPlayer(true, attackWindupDuration + attackCommitDuration);
        stateEndTime = Time.time + attackWindupDuration;
        attackHitTime = stateEndTime + attackCommitDuration * 0.35f;
        nextAttackTime = Time.time + attackWindupDuration + attackCommitDuration + attackRecoverDuration + attackCooldown;
        animatorDriver?.TriggerAttack();
        LogAttack("attempt");
    }

    private void EnterRecover(string reason)
    {
        SetState(State.Recover, reason);
        StopMoving();
        attackPhase = AttackPhase.None;
        RefreshAggroMemory("RecoverEnter", true);
        stateEndTime = Time.time + attackRecoverDuration;
    }

    private void EnterStuck(string reason)
    {
        SetState(State.Stuck, reason);
        StopMoving();
        attackPhase = AttackPhase.None;
        stateEndTime = Time.time + stuckDuration;
        nextStuckAllowedTime = Time.time + stuckCooldown;
        combatMoveToStuckCount++;
        LogStuck($"enter reason={reason} blockedTimer={blockedTimer:F2}");
        TryPickStuckTarget();
    }

    private void EnterHurt()
    {
        if (state == State.Dead)
        {
            return;
        }

        SetState(State.Hurt, "HurtFlash");
        StopMoving();
        attackPhase = AttackPhase.None;
        stateEndTime = Time.time + hurtStunDuration;
        SetMoveIntent(Vector2.zero, true);
    }

    private void EnterDead()
    {
        SetState(State.Dead, "DeathNotifier");
        StopMoving();
        attackPhase = AttackPhase.None;
        SetMoveIntent(Vector2.zero, true);
    }

    private void SetState(State newState, string reason)
    {
        if (state == newState)
        {
            return;
        }

        State previousState = state;
        float stateAge = Time.time - stateEnterTime;
        AccumulateStateTime(previousState, stateAge);
        state = newState;
        stateEnterTime = Time.time;
        blockedTimer = 0f;
        LogStateChange(previousState, newState, reason, stateAge);
    }

    private void PickWanderTarget()
    {
        if (TryGetWalkablePointNear(wanderAnchor, wanderRadius, out Vector3 targetPosition))
        {
            SetMoveTarget(targetPosition, wanderSpeedMultiplier, targetPosition - transform.position, true);
            return;
        }

        EnterIdle("NoWalkableTarget");
    }

    private void RefreshCombatMoveTarget(bool forceIntentUpdate = false)
    {
        Vector3 pursuitPosition = GetPursuitTarget();
        if (TryGetNearestWalkablePoint(pursuitPosition, targetSnapRadius, out Vector3 targetPosition))
        {
            SetMoveTarget(targetPosition, chaseSpeedMultiplier, targetPosition - transform.position, forceIntentUpdate);
            return;
        }

        StopMoving("NoWalkableTarget");
    }

    private void TryPickStuckTarget()
    {
        Vector2 origin = GetCurrentWorldPosition();
        Vector2 toMemory = lastKnownPlayerPosition - origin;
        Vector2 side = toMemory.sqrMagnitude > 0.0001f
            ? new Vector2(-toMemory.normalized.y, toMemory.normalized.x)
            : Random.insideUnitCircle.normalized;

        Vector2 candidate = origin + side * Mathf.Max(0.3f, stuckResolveRadius);
        if (!TryGetNearestWalkablePoint(candidate, stuckResolveRadius, out Vector3 walkableTarget))
        {
            if (!TryGetWalkablePointNear(origin, stuckResolveRadius, out walkableTarget))
            {
                StopMoving("StuckNoWalkableTarget");
                return;
            }
        }

        SetMoveTarget(walkableTarget, stuckSpeedMultiplier, walkableTarget - transform.position, true);
    }

    private void SetMoveTarget(Vector2 targetPosition, float speedMultiplier, Vector2 desiredIntent, bool forceIntentUpdate = false)
    {
        if (!IsWalkable(targetPosition))
        {
            if (!TryGetNearestWalkablePoint(targetPosition, targetSnapRadius, out Vector3 walkableTarget))
            {
                StopMoving("TargetNotWalkable");
                return;
            }

            targetPosition = walkableTarget;
            desiredIntent = targetPosition - GetCurrentWorldPosition();
        }

        slimePathFinding.SetSpeedMultiplier(speedMultiplier);
        slimePathFinding.MoveTo(targetPosition);
        nextPathRefreshTime = Time.time + Random.Range(pathRefreshIntervalMin, pathRefreshIntervalMax);
        SetMoveIntent(desiredIntent, forceIntentUpdate);
    }

    private void StopMoving(string reason = null)
    {
        slimePathFinding.SetSpeedMultiplier(1f);
        slimePathFinding.StopMoving(reason);
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

    private void FaceLastKnownPlayer(bool force, float customLockDuration = -1f)
    {
        if (playerTransform != null && Vector2.Distance(GetCurrentWorldPosition(), playerTransform.position) <= loseInterestRange)
        {
            SetFacing((Vector2)playerTransform.position - GetCurrentWorldPosition(), force, customLockDuration);
            return;
        }

        if (HasRecentPlayerMemory() || HasAggroMemory())
        {
            SetFacing(lastKnownPlayerPosition - GetCurrentWorldPosition(), force, customLockDuration);
        }
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

    private void UpdatePerception(float distanceToPlayer)
    {
        bool canSeePlayer = CanDetectPlayer(distanceToPlayer);
        if (canSeePlayer)
        {
            lastSeenPlayerTime = Time.time;
            lastKnownPlayerPosition = playerTransform.position;
            RefreshAggroMemory("DetectedPlayer", !hadPlayerSightLastFrame);
            if (!hadPlayerSightLastFrame)
            {
                LogPerception($"acquired dist={distanceToPlayer:F2}");
            }
        }
        else if (hadPlayerSightLastFrame)
        {
            LogPerception("lost line of sight");
        }

        hadPlayerSightLastFrame = canSeePlayer;
    }

    private bool CanDetectPlayer(float distanceToPlayer)
    {
        if (playerTransform == null || distanceToPlayer > detectionRange)
        {
            return false;
        }

        if (!requireLineOfSight || navigationProvider == null)
        {
            return true;
        }

        return navigationProvider.HasLineOfSight(GetCurrentWorldPosition(), playerTransform.position);
    }

    private bool HasRecentPlayerMemory()
    {
        return playerTransform != null && Time.time - lastSeenPlayerTime <= lostSightGraceTime;
    }

    private bool HasAggroMemory()
    {
        return Time.time <= combatEngagedUntil;
    }

    private void RefreshAggroMemory(string reason, bool forceLog = false)
    {
        float previousUntil = combatEngagedUntil;
        combatEngagedUntil = Mathf.Max(combatEngagedUntil, Time.time + aggroMemoryDuration);
        if (!enableDebugLogs || !debugPerception)
        {
            return;
        }

        if (!forceLog && Time.time < nextAggroLogTime && combatEngagedUntil - previousUntil < 0.2f)
        {
            return;
        }

        nextAggroLogTime = Time.time + 0.75f;
        Debug.Log(
            $"[SlimeAI] {name} aggro refresh reason={reason} until={combatEngagedUntil:F2} remaining={(combatEngagedUntil - Time.time):F2} dist={(float.IsInfinity(GetDistanceToPlayer()) ? -1f : GetDistanceToPlayer()):F2}",
            this);
    }

    private bool ShouldLoseTarget(float distanceToPlayer, out string reason)
    {
        if (playerTransform == null)
        {
            reason = "NoPlayer";
            return true;
        }

        if (distanceToPlayer > loseInterestRange)
        {
            reason = "OutOfLoseInterestRange";
            return true;
        }

        if (!HasAggroMemory())
        {
            reason = "AggroMemoryExpired";
            return true;
        }

        reason = null;
        return false;
    }

    private bool CanAttackNow(float distanceToPlayer)
    {
        return playerTransform != null && distanceToPlayer <= attackRange && Time.time >= nextAttackTime;
    }

    private bool ShouldEnterStuck(float distanceToPlayer)
    {
        if (CanAttackNow(distanceToPlayer))
        {
            return false;
        }

        if (Time.time - stateEnterTime < minCombatMoveBeforeStuck)
        {
            return false;
        }

        if (Time.time < nextStuckAllowedTime)
        {
            return false;
        }

        if (blockedTimer < blockedDuration)
        {
            return false;
        }

        LogStuck(
            $"eligible dist={distanceToPlayer:F2} blockedTimer={blockedTimer:F2} navBlocked={slimePathFinding.WasBlockedByNavigation} hasTarget={slimePathFinding.HasTarget}");
        return true;
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
        bool shouldTrackMovement = state == State.Wander || state == State.CombatMove;
        if (!shouldTrackMovement || !slimePathFinding.HasTarget)
        {
            ClearBlockedTimer("NoTrackedMove");
            return;
        }

        float movedDistance = Vector2.Distance(previousPosition, transform.position);
        bool blockedNow = slimePathFinding.WasBlockedByNavigation || movedDistance <= blockedMoveThreshold;
        if (!blockedNow)
        {
            ClearBlockedTimer("MovementResumed");
            return;
        }

        if (blockedTimer <= 0f && state == State.CombatMove)
        {
            LogStuck(
                $"timer start dist={GetDistanceToPlayer():F2} navBlocked={slimePathFinding.WasBlockedByNavigation} moved={movedDistance:F4}");
        }

        blockedTimer = slimePathFinding.WasBlockedByNavigation
            ? Mathf.Max(blockedTimer, blockedDuration)
            : blockedTimer + Time.deltaTime;
    }

    private void ClearBlockedTimer(string reason)
    {
        if (blockedTimer > 0f && state == State.CombatMove)
        {
            LogStuck($"timer clear reason={reason} blockedTimer={blockedTimer:F2}");
        }

        blockedTimer = 0f;
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

    private void ValidateSpawnPosition()
    {
        if (navigationProvider == null || IsWalkable(GetCurrentWorldPosition()))
        {
            return;
        }

        if (TryGetNearestWalkablePoint(GetCurrentWorldPosition(), spawnSnapRadius, out Vector3 walkablePoint))
        {
            transform.position = walkablePoint;
            if (rb != null)
            {
                rb.position = walkablePoint;
            }

            return;
        }

        if (enableDebugLogs)
        {
            Debug.LogWarning($"[SlimeAI] {name} no walkable spawn point found. disabling slime ai", this);
        }

        slimePathFinding.enabled = false;
        enabled = false;
    }

    private bool TryGetWalkablePointNear(Vector3 origin, float radius, out Vector3 point)
    {
        if (navigationProvider == null)
        {
            point = origin + (Vector3)(Random.insideUnitCircle * radius);
            return true;
        }

        return navigationProvider.TryGetRandomWalkablePointNear(origin, radius, walkablePointAttempts, out point);
    }

    private bool TryGetNearestWalkablePoint(Vector3 origin, float radius, out Vector3 point)
    {
        if (navigationProvider == null)
        {
            point = origin;
            return true;
        }

        return navigationProvider.TryGetNearestWalkablePoint(origin, radius, out point);
    }

    private bool IsWalkable(Vector3 worldPosition)
    {
        return navigationProvider == null || navigationProvider.IsWalkable(worldPosition);
    }

    private Vector3 GetPursuitTarget()
    {
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(GetCurrentWorldPosition(), playerTransform.position);
            if (distanceToPlayer <= loseInterestRange)
            {
                LogPursuitTarget("CurrentPlayer", playerTransform.position);
                return playerTransform.position;
            }
        }

        if (HasRecentPlayerMemory() || HasAggroMemory())
        {
            LogPursuitTarget("LastKnown", lastKnownPlayerPosition);
            return lastKnownPlayerPosition;
        }

        LogPursuitTarget("None", transform.position);
        return playerTransform != null ? playerTransform.position : transform.position;
    }

    private Vector2 GetCurrentWorldPosition()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    private DungeonNavigationProvider ResolveNavigationProvider()
    {
        DungeonNavigationProvider provider = GetComponent<DungeonNavigationProvider>();
        if (provider != null)
        {
            return provider;
        }

        provider = FindFirstObjectByType<DungeonNavigationProvider>();
        if (provider != null)
        {
            return provider;
        }

        DungeonManager dungeonManager = FindFirstObjectByType<DungeonManager>();
        GameObject host = dungeonManager != null ? dungeonManager.gameObject : gameObject;
        return host.AddComponent<DungeonNavigationProvider>();
    }

    public void AnimationEvent_AttackHit()
    {
        if (state != State.Attack)
        {
            return;
        }

        TryApplyAttackDamage();
    }

    private void TryApplyAttackDamage()
    {
        if (hasAppliedAttackDamage || state == State.Dead || playerTransform == null)
        {
            return;
        }

        Vector2 attackCenter = GetAttackPoint();
        Collider2D playerHit = playerLayer.value != 0
            ? Physics2D.OverlapCircle(attackCenter, attackHitRadius, playerLayer)
            : Physics2D.OverlapCircle(attackCenter, attackHitRadius);
        if (playerHit == null || playerHit.transform.root != playerTransform.root)
        {
            LogAttack("miss range");
            return;
        }

        if (requireLineOfSight && navigationProvider != null &&
            !navigationProvider.HasLineOfSight(GetCurrentWorldPosition(), playerTransform.position))
        {
            LogAttack("blocked line of sight");
            return;
        }

        Vector2 toPlayer = ((Vector2)playerTransform.position - attackCenter).normalized;
        if (toPlayer != Vector2.zero && Vector2.Dot(FacingDirection.normalized, toPlayer) < -0.15f)
        {
            LogAttack("miss behind");
            return;
        }

        PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            LogAttack("missing PlayerStats");
            return;
        }

        hasAppliedAttackDamage = true;
        attackHitCount++;
        playerStats.TakeDamage(attackDamage);

        if (playerTransform.TryGetComponent(out Knockback playerKnockback))
        {
            playerKnockback.GetKnockedBack(transform, attackKnockbackForce);
        }

        if (playerTransform.TryGetComponent(out Flash playerFlash))
        {
            StartCoroutine(playerFlash.FlashRoutine());
        }

        LogAttack($"hit damage={attackDamage:F1}");
    }

    private Vector2 GetAttackPoint()
    {
        Vector2 facing = FacingDirection.sqrMagnitude > 0.0001f ? FacingDirection.normalized : Vector2.down;
        Vector2 perpendicular = new Vector2(-facing.y, facing.x);
        Vector2 localOffset = perpendicular * attackHitOffset.x + facing * attackHitOffset.y;
        return GetCurrentWorldPosition() + localOffset;
    }

    private float GetAnimationSpeed()
    {
        switch (state)
        {
            case State.Wander:
            case State.CombatMove:
            case State.Stuck:
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

    private void AccumulateStateTime(State previousState, float stateAge)
    {
        if (previousState == State.CombatMove)
        {
            totalCombatMoveTime += stateAge;
        }
        else if (previousState == State.Stuck)
        {
            totalStuckTime += stateAge;
        }
    }

    private void LogStateChange(State previousState, State newState, string reason, float stateAge)
    {
        if (!enableDebugLogs || !debugStateTransitions)
        {
            return;
        }

        Vector2 currentPos = GetCurrentWorldPosition();
        string playerPos = playerTransform != null ? ((Vector2)playerTransform.position).ToString("F2") : "none";
        string pathTarget = slimePathFinding != null && slimePathFinding.HasTarget
            ? slimePathFinding.CurrentTargetPosition.ToString("F2")
            : "none";
        float distanceToPlayer = GetDistanceToPlayer();
        Debug.Log(
            $"[SlimeAI] {name} {previousState}->{newState} reason={reason} dist={(float.IsInfinity(distanceToPlayer) ? -1f : distanceToPlayer):F2} age={stateAge:F2} hasTarget={slimePathFinding != null && slimePathFinding.HasTarget} pathTarget={pathTarget} navBlocked={slimePathFinding != null && slimePathFinding.WasBlockedByNavigation} blockedTimer={blockedTimer:F2} pos={currentPos:F2} playerPos={playerPos} stuckCount={combatMoveToStuckCount} stuckExitCount={stuckExitCount} atkAttempts={attackAttemptCount} atkHits={attackHitCount} lostTargets={lostTargetCount}",
            this);
    }

    private void LogPerception(string message)
    {
        if (!enableDebugLogs || !debugPerception)
        {
            return;
        }

        Debug.Log(
            $"[SlimeAI] {name} perception {message} lastSeenAge={(Time.time - lastSeenPlayerTime):F2} memory={HasRecentPlayerMemory()} pos={GetCurrentWorldPosition():F2} lastKnown={lastKnownPlayerPosition:F2}",
            this);
    }

    private void LogPursuitTarget(string source, Vector3 target)
    {
        if (!enableDebugLogs || !debugPerception || lastPursuitSource == source)
        {
            return;
        }

        lastPursuitSource = source;
        Debug.Log(
            $"[SlimeAI] {name} pursuit source={source} target={((Vector2)target):F2} aggroRemaining={(combatEngagedUntil - Time.time):F2}",
            this);
    }

    private void LogStuck(string message)
    {
        if (!enableDebugLogs || !debugStuck)
        {
            return;
        }

        Debug.Log(
            $"[SlimeAI] {name} stuck {message} combatTime={totalCombatMoveTime + (state == State.CombatMove ? Time.time - stateEnterTime : 0f):F2} stuckTime={totalStuckTime + (state == State.Stuck ? Time.time - stateEnterTime : 0f):F2}",
            this);
    }

    private void LogAttack(string message)
    {
        if (!enableDebugLogs || !debugAttack)
        {
            return;
        }

        Debug.Log($"[SlimeAI] {name} attack {message}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseInterestRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackExitRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stuckResolveRadius);

        Vector2 attackPoint = Application.isPlaying ? GetAttackPoint() : (Vector2)transform.position + attackHitOffset;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(attackPoint, attackHitRadius);

        if (Application.isPlaying && slimePathFinding != null && slimePathFinding.HasTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, slimePathFinding.CurrentTargetPosition);
            Gizmos.DrawWireSphere(slimePathFinding.CurrentTargetPosition, 0.15f);
        }
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
