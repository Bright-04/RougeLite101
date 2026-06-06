using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum EnemyAIState
{
    Idle,
    Chasing,
    Attacking,
    Recovering,
    Hurt,
    Dead
}

public class SlimeAI : MonoBehaviour, IDdaAdaptiveEnemy
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
        Chase,
        Attack,
        Recover,
        Hurt,
        Dead
    }

    private enum AttackPhase
    {
        None,
        Windup,
        Commit
    }

    private enum ChaseZone
    {
        None,
        AttackZone,
        MeleeHold,
        FarChase
    }

    [Serializable]
    private class SlimeBehaviorProfileData
    {
        [Header("Preset")]
        [SerializeField] private BehaviorPreset preset = BehaviorPreset.Basic;

        [Header("Awareness")]
        [SerializeField] private float detectionRange = 6f;
        [SerializeField] private float loseInterestRange = 9f;
        [SerializeField] private float aggroMemoryDuration = 3.5f;
        [SerializeField] private float lostSightGraceTime = 0.8f;
        [SerializeField] private bool requireLineOfSight = true;

        [Header("Movement")]
        [SerializeField] private float chaseSpeedMultiplier = 1f;
        [SerializeField] private float wanderSpeedMultiplier = 0.62f;
        [SerializeField] private float wanderRadius = 2.5f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float attackExitRange = 1.55f;
        [SerializeField] private float meleeHoldExitBuffer = 0.25f;
        [SerializeField] [Range(0.45f, 0.65f)] private float meleeHoldSpeedMultiplier = 0.55f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float attackHitRadius = 0.7f;
        [SerializeField] private Vector2 attackHitOffset = new Vector2(0f, 0.45f);
        [SerializeField] private float attackKnockbackForce = 10f;

        [Header("Timing")]
        [SerializeField] private float reactionDelayMin = 0.35f;
        [SerializeField] private float reactionDelayMax = 0.8f;
        [SerializeField] private float recoverDuration = 0.65f;
        [SerializeField] private float hurtDuration = 0.3f;

        [Header("Advanced")]
        [SerializeField] private float minThinkInterval = 0.35f;
        [SerializeField] private float maxThinkInterval = 0.7f;
        [SerializeField] private float pathRefreshIntervalMin = 0.25f;
        [SerializeField] private float pathRefreshIntervalMax = 0.45f;
        [SerializeField] private float facingLockDuration = 0.22f;
        [SerializeField] private float moveIntentLockDuration = 0.28f;
        [SerializeField] private int walkablePointAttempts = 8;
        [SerializeField] private float attackWindupDuration = 0.35f;
        [SerializeField] private float attackCommitDuration = 0.2f;
        [SerializeField] private float idleDurationMin = 1f;
        [SerializeField] private float idleDurationMax = 3f;
        [SerializeField] private float wanderDurationMin = 1.5f;
        [SerializeField] private float wanderDurationMax = 4f;
        [SerializeField] private float spawnSnapRadius = 1.5f;
        [SerializeField] private float targetSnapRadius = 1.2f;

        public BehaviorPreset Preset => preset;
        public float DetectionRange => detectionRange;
        public float LoseInterestRange => loseInterestRange;
        public float AggroMemoryDuration => aggroMemoryDuration;
        public float LostSightGraceTime => lostSightGraceTime;
        public bool RequireLineOfSight => requireLineOfSight;
        public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
        public float WanderSpeedMultiplier => wanderSpeedMultiplier;
        public float WanderRadius => wanderRadius;
        public float AttackRange => attackRange;
        public float AttackExitRange => attackExitRange;
        public float MeleeHoldExitBuffer => meleeHoldExitBuffer;
        public float MeleeHoldSpeedMultiplier => meleeHoldSpeedMultiplier;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public float AttackHitRadius => attackHitRadius;
        public Vector2 AttackHitOffset => attackHitOffset;
        public float AttackKnockbackForce => attackKnockbackForce;
        public float ReactionDelayMin => reactionDelayMin;
        public float ReactionDelayMax => reactionDelayMax;
        public float RecoverDuration => recoverDuration;
        public float HurtDuration => hurtDuration;
        public float MinThinkInterval => minThinkInterval;
        public float MaxThinkInterval => maxThinkInterval;
        public float PathRefreshIntervalMin => pathRefreshIntervalMin;
        public float PathRefreshIntervalMax => pathRefreshIntervalMax;
        public float FacingLockDuration => facingLockDuration;
        public float MoveIntentLockDuration => moveIntentLockDuration;
        public int WalkablePointAttempts => walkablePointAttempts;
        public float AttackWindupDuration => attackWindupDuration;
        public float AttackCommitDuration => attackCommitDuration;
        public float IdleDurationMin => idleDurationMin;
        public float IdleDurationMax => idleDurationMax;
        public float WanderDurationMin => wanderDurationMin;
        public float WanderDurationMax => wanderDurationMax;
        public float SpawnSnapRadius => spawnSnapRadius;
        public float TargetSnapRadius => targetSnapRadius;

        public bool LooksUninitialized()
        {
            return detectionRange <= 0f || loseInterestRange <= 0f || attackRange <= 0f;
        }

        public void ApplyPresetDefaults()
        {
            switch (preset)
            {
                case BehaviorPreset.Ice:
                    detectionRange = 5.5f;
                    loseInterestRange = 8.5f;
                    aggroMemoryDuration = 4f;
                    chaseSpeedMultiplier = 0.9f;
                    wanderSpeedMultiplier = 0.58f;
                    wanderRadius = 2.6f;
                    attackRange = 1.2f;
                    attackExitRange = 1.55f;
                    meleeHoldExitBuffer = 0.25f;
                    meleeHoldSpeedMultiplier = 0.55f;
                    attackDamage = 10f;
                    attackCooldown = 1.35f;
                    attackHitRadius = 0.7f;
                    reactionDelayMin = 0.45f;
                    reactionDelayMax = 0.85f;
                    recoverDuration = 0.75f;
                    break;
                case BehaviorPreset.Magma:
                    detectionRange = 6.5f;
                    loseInterestRange = 10f;
                    aggroMemoryDuration = 4f;
                    chaseSpeedMultiplier = 1.12f;
                    wanderSpeedMultiplier = 0.68f;
                    wanderRadius = 2.8f;
                    attackRange = 1.25f;
                    attackExitRange = 1.6f;
                    meleeHoldExitBuffer = 0.25f;
                    meleeHoldSpeedMultiplier = 0.58f;
                    attackDamage = 12f;
                    attackCooldown = 1.05f;
                    attackHitRadius = 0.75f;
                    reactionDelayMin = 0.25f;
                    reactionDelayMax = 0.55f;
                    recoverDuration = 0.6f;
                    break;
                default:
                    detectionRange = 6f;
                    loseInterestRange = 9f;
                    aggroMemoryDuration = 3.5f;
                    chaseSpeedMultiplier = 1f;
                    wanderSpeedMultiplier = 0.62f;
                    wanderRadius = 2.5f;
                    attackRange = 1.2f;
                    attackExitRange = 1.55f;
                    meleeHoldExitBuffer = 0.25f;
                    meleeHoldSpeedMultiplier = 0.55f;
                    attackDamage = 10f;
                    attackCooldown = 1.2f;
                    attackHitRadius = 0.7f;
                    reactionDelayMin = 0.35f;
                    reactionDelayMax = 0.8f;
                    recoverDuration = 0.65f;
                    break;
            }
        }

        public void Validate()
        {
            detectionRange = Mathf.Max(0.1f, detectionRange);
            loseInterestRange = Mathf.Max(detectionRange + 0.5f, loseInterestRange);
            aggroMemoryDuration = Mathf.Max(0.5f, aggroMemoryDuration);
            attackRange = Mathf.Max(0.1f, attackRange);
            attackExitRange = Mathf.Max(attackRange + 0.1f, attackExitRange);
            meleeHoldExitBuffer = meleeHoldExitBuffer > 0f ? meleeHoldExitBuffer : 0.25f;
            meleeHoldSpeedMultiplier = meleeHoldSpeedMultiplier > 0f ? Mathf.Clamp(meleeHoldSpeedMultiplier, 0.45f, 0.65f) : 0.55f;
            attackHitRadius = Mathf.Max(0.05f, attackHitRadius);
            attackCooldown = Mathf.Max(0.05f, attackCooldown);
            reactionDelayMax = Mathf.Max(reactionDelayMin, reactionDelayMax);
            recoverDuration = Mathf.Max(0.05f, recoverDuration);
            hurtDuration = Mathf.Max(0.05f, hurtDuration);
            minThinkInterval = Mathf.Max(0.05f, minThinkInterval);
            maxThinkInterval = Mathf.Max(minThinkInterval, maxThinkInterval);
            pathRefreshIntervalMax = Mathf.Max(pathRefreshIntervalMin, pathRefreshIntervalMax);
            idleDurationMax = Mathf.Max(idleDurationMin, idleDurationMax);
            wanderDurationMax = Mathf.Max(wanderDurationMin, wanderDurationMax);
            walkablePointAttempts = Mathf.Max(1, walkablePointAttempts);
            spawnSnapRadius = Mathf.Max(0.1f, spawnSnapRadius);
            targetSnapRadius = Mathf.Max(0.1f, targetSnapRadius);
        }
    }

    [Header("Behavior Profile")]
    [SerializeField] private SlimeBehaviorProfileData behaviorProfile = new SlimeBehaviorProfileData();

    [Header("References")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool debugStateTransitions = true;
    [SerializeField] private bool debugPerception = false;
    [SerializeField] private bool debugAttack = false;
    [SerializeField] private bool debugChaseZones = false;
    [SerializeField] private bool drawGizmos = true;

#pragma warning disable 0414
    [HideInInspector] [SerializeField] private BehaviorPreset preset = BehaviorPreset.Basic;
    [HideInInspector] [SerializeField] private float detectionRange = 5f;
    [HideInInspector] [SerializeField] private float loseInterestRange = 7f;
    [HideInInspector] [SerializeField] private float aggroMemoryDuration = 3.5f;
    [HideInInspector] [SerializeField] private bool requireLineOfSight = true;
    [HideInInspector] [SerializeField] private float lostSightGraceTime = 0.8f;
    [HideInInspector] [SerializeField] private float minThinkInterval = 0.35f;
    [HideInInspector] [SerializeField] private float maxThinkInterval = 0.7f;
    [HideInInspector] [SerializeField] private float pathRefreshIntervalMin = 0.25f;
    [HideInInspector] [SerializeField] private float pathRefreshIntervalMax = 0.45f;
    [HideInInspector] [SerializeField] private float facingLockDuration = 0.22f;
    [HideInInspector] [SerializeField] private float moveIntentLockDuration = 0.28f;
    [HideInInspector] [FormerlySerializedAs("roamRadius")] [SerializeField] private float wanderRadius = 2.5f;
    [HideInInspector] [SerializeField] private int walkablePointAttempts = 8;
    [HideInInspector] [SerializeField] private float idleDurationMin = 1f;
    [HideInInspector] [SerializeField] private float idleDurationMax = 3f;
    [HideInInspector] [SerializeField] private float wanderDurationMin = 1.5f;
    [HideInInspector] [SerializeField] private float wanderDurationMax = 4f;
    [HideInInspector] [SerializeField] private float wanderSpeedMultiplier = 0.62f;
    [HideInInspector] [SerializeField] private float minAggroReactionDelay = 0.35f;
    [HideInInspector] [SerializeField] private float maxAggroReactionDelay = 0.8f;
    [HideInInspector] [SerializeField] private float chaseSpeedMultiplier = 0.82f;
    [HideInInspector] [SerializeField] private float attackRange = 1.2f;
    [HideInInspector] [SerializeField] private float attackExitRange = 1.55f;
    [HideInInspector] [SerializeField] private float meleeHoldExitBuffer = 0.25f;
    [HideInInspector] [SerializeField] private float meleeHoldSpeedMultiplier = 0.55f;
    [HideInInspector] [SerializeField] private float attackDamage = 10f;
    [HideInInspector] [SerializeField] private float attackHitRadius = 0.7f;
    [HideInInspector] [SerializeField] private Vector2 attackHitOffset = new Vector2(0f, 0.45f);
    [HideInInspector] [SerializeField] private float attackKnockbackForce = 10f;
    [HideInInspector] [SerializeField] private float attackWindupDuration = 0.35f;
    [HideInInspector] [SerializeField] private float attackCommitDuration = 0.2f;
    [HideInInspector] [SerializeField] private float attackRecoverDuration = 0.65f;
    [HideInInspector] [SerializeField] private float attackCooldown = 1.25f;
    [HideInInspector] [SerializeField] private float hurtStunDuration = 0.3f;
    [HideInInspector] [SerializeField] private float spawnSnapRadius = 1.5f;
    [HideInInspector] [SerializeField] private float targetSnapRadius = 1.2f;
    [HideInInspector] [SerializeField] private float blockedDuration = 1f;
    [HideInInspector] [SerializeField] private float blockedMoveThreshold = 0.01f;
    [HideInInspector] [SerializeField] private float minCombatMoveBeforeStuck = 0.6f;
    [HideInInspector] [SerializeField] private float stuckDuration = 0.5f;
    [HideInInspector] [SerializeField] private float stuckCooldown = 1.5f;
    [HideInInspector] [SerializeField] private float stuckResolveRadius = 0.9f;
    [HideInInspector] [SerializeField] private float stuckSpeedMultiplier = 0.68f;
    [HideInInspector] [SerializeField] private float repositionDurationMin = 0.55f;
    [HideInInspector] [SerializeField] private float repositionDurationMax = 1.1f;
    [HideInInspector] [SerializeField] private float repositionDistanceMin = 0.75f;
    [HideInInspector] [SerializeField] private float repositionDistanceMax = 1.65f;
    [HideInInspector] [SerializeField] private float repositionSpeedMultiplier = 0.72f;
    [HideInInspector] [SerializeField] private float crowdingRange = 0.7f;
    [HideInInspector] [SerializeField] private float chaseRepositionChance = 0.18f;
    [HideInInspector] [SerializeField] private float hurtRepositionChance = 0.7f;
#pragma warning restore 0414

    private State state;
    private AttackPhase attackPhase;
    private ChaseZone chaseZone;
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
    private float nextAggroLogTime;
    private float facingLockEndTime;
    private float moveIntentLockEndTime;
    private float attackHitTime;
    private float stateEnterTime;
    private float combatEngagedUntil;
    private float lastSeenPlayerTime = float.NegativeInfinity;
    private bool hasAppliedAttackDamage;
    private bool hadPlayerSightLastFrame;
    private int attackAttemptCount;
    private int attackHitCount;
    private int lostTargetCount;
    private bool ddaRuntimeValuesInitialized;
    private float baseChaseSpeedMultiplier;
    private float currentChaseSpeedMultiplier;
    private float baseAttackCooldown;
    private float currentAttackCooldown;
    private float baseAttackRecoverDuration;
    private float currentAttackRecoverDuration;
    private float baseDetectionRange;
    private float currentDetectionRange;

    public Vector2 MoveIntentDirection => moveIntentDirection;
    public Vector2 FacingDirection => facingDirection;
    public int FacingIndex => ResolveFacingIndex(facingDirection);
    public float AnimationSpeed => GetAnimationSpeed();
    public bool HasStableFacing => facingDirection.sqrMagnitude > 0.0001f;
    public bool IsDead => state == State.Dead;
    public EnemyAIState CurrentAIState => ToEnemyAIState(state);

    private void Reset()
    {
        if (behaviorProfile == null)
        {
            behaviorProfile = new SlimeBehaviorProfileData();
        }

        behaviorProfile.ApplyPresetDefaults();
        behaviorProfile.Validate();
        ApplyBehaviorProfileToFields();
    }

    private void Awake()
    {
        EnsureBehaviorProfile();
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
        chaseZone = ChaseZone.None;
        InitializeDdaRuntimeValues();
    }

    private void OnValidate()
    {
        EnsureBehaviorProfile();
    }

    [ContextMenu("Apply Behavior Profile Defaults")]
    private void ApplyBehaviorProfileDefaults()
    {
        if (behaviorProfile == null)
        {
            behaviorProfile = new SlimeBehaviorProfileData();
        }

        behaviorProfile.ApplyPresetDefaults();
        behaviorProfile.Validate();
        ApplyBehaviorProfileToFields();
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
            case State.Attack:
                UpdateAttack(distanceToPlayer);
                break;
            case State.Recover:
                UpdateRecover(distanceToPlayer);
                break;
            case State.Hurt:
                UpdateHurt(distanceToPlayer);
                break;
        }

        previousPosition = transform.position;
    }

    private void UpdateIdle(float distanceToPlayer)
    {
        StopMoving("Idle");

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
            EnterIdle("WanderNoTarget");
            return;
        }

        if (ReachedCurrentTarget())
        {
            EnterIdle("WanderReachedTarget");
        }
    }

    private void UpdateAlert(float distanceToPlayer)
    {
        StopMoving("Alert");
        FaceTarget(true);
        RefreshAggroIfRelevant(distanceToPlayer, "Alert");

        if (Time.time < stateEndTime)
        {
            return;
        }

        if (HasAggroTarget(distanceToPlayer))
        {
            EnterChase("ReactionDelayFinished");
            return;
        }

        EnterIdle("LostPlayer");
    }

    private void UpdateChase(float distanceToPlayer)
    {
        RefreshAggroIfRelevant(distanceToPlayer, "Chase");
        if (ShouldLoseTarget(distanceToPlayer, out string loseReason))
        {
            lostTargetCount++;
            LogPerception($"lost target reason={loseReason}");
            EnterIdle($"LostInterest:{loseReason}");
            return;
        }

        ChaseZone targetZone = ResolveChaseZone(distanceToPlayer);
        SetChaseZone(targetZone);

        if (targetZone == ChaseZone.AttackZone)
        {
            StopMoving("AttackZone");
            FaceTarget(true);
            if (Time.time >= nextAttackTime)
            {
                EnterAttack("InAttackRange");
            }

            return;
        }

        if (targetZone == ChaseZone.MeleeHold)
        {
            FaceTarget(true);
            if (distanceToPlayer > attackRange)
            {
                if (Time.time >= nextPathRefreshTime || !slimePathFinding.HasTarget || slimePathFinding.CurrentStatus == SlimePathFinding.MovementStatus.Blocked)
                {
                    RefreshMeleeHoldTarget();
                }

                UpdateMovementIntentFromTarget();
            }
            else
            {
                StopMoving("MeleeHoldInRange");
            }

            if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
            {
                EnterAttack("MeleeHoldInAttackRange");
            }

            return;
        }

        if (Time.time >= nextPathRefreshTime || !slimePathFinding.HasTarget || slimePathFinding.CurrentStatus == SlimePathFinding.MovementStatus.Blocked)
        {
            RefreshChaseTarget();
        }

        UpdateMovementIntentFromTarget();
    }

    private void UpdateAttack(float distanceToPlayer)
    {
        StopMoving("Attack");

        switch (attackPhase)
        {
            case AttackPhase.Windup:
                FaceTarget(true, attackWindupDuration + attackCommitDuration);
                if (playerTransform == null || distanceToPlayer > attackExitRange)
                {
                    LogAttack("attempt canceled target left range");
                    EnterRecover("TargetLeftAttackExitRange");
                    return;
                }

                if (Time.time >= stateEndTime)
                {
                    attackPhase = AttackPhase.Commit;
                    hasAppliedAttackDamage = false;
                    attackHitTime = Time.time + attackCommitDuration * 0.35f;
                    stateEndTime = Time.time + attackCommitDuration;
                    LogAttack("commit");
                }
                break;

            case AttackPhase.Commit:
                FaceTarget(true, attackCommitDuration);
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
        StopMoving("Recover");
        RefreshAggroIfRelevant(distanceToPlayer, "Recover");

        if (Time.time < stateEndTime)
        {
            FaceTarget(false);
            return;
        }

        if (HasAggroTarget(distanceToPlayer))
        {
            EnterChase("RecoverFinished");
            return;
        }

        EnterIdle("RecoverFinishedLostPlayer");
    }

    private void UpdateHurt(float distanceToPlayer)
    {
        StopMoving("Hurt");
        RefreshAggroIfRelevant(distanceToPlayer, "Hurt");
        if (Time.time < stateEndTime)
        {
            return;
        }

        if (HasAggroTarget(distanceToPlayer))
        {
            EnterChase("HurtRecovered");
            return;
        }

        EnterIdle("HurtRecoveredLostPlayer");
    }

    private void EnterIdle(string reason)
    {
        SetState(State.Idle, reason);
        chaseZone = ChaseZone.None;
        attackPhase = AttackPhase.None;
        StopMoving(reason);
        stateEndTime = Time.time + UnityEngine.Random.Range(idleDurationMin, idleDurationMax);
        ScheduleNextThink();
        SetMoveIntent(Vector2.zero, true);
        if (UnityEngine.Random.value < 0.35f)
        {
            MaybeTurnIdleFacing();
        }
    }

    private void EnterWander(string reason)
    {
        SetState(State.Wander, reason);
        chaseZone = ChaseZone.None;
        attackPhase = AttackPhase.None;
        wanderAnchor = GetCurrentWorldPosition();
        stateEndTime = Time.time + UnityEngine.Random.Range(wanderDurationMin, wanderDurationMax);
        ScheduleNextThink();
        PickWanderTarget();
    }

    private void EnterAlert(string reason)
    {
        SetState(State.Alert, reason);
        chaseZone = ChaseZone.None;
        attackPhase = AttackPhase.None;
        StopMoving(reason);
        RefreshAggroMemory("AlertEnter", true);
        stateEndTime = Time.time + UnityEngine.Random.Range(minAggroReactionDelay, maxAggroReactionDelay);
        ScheduleNextThink();
        FaceTarget(true);
    }

    private void EnterChase(string reason)
    {
        SetState(State.Chase, reason);
        attackPhase = AttackPhase.None;
        RefreshAggroMemory("ChaseEnter", true);
        ScheduleNextThink();
        RefreshChaseTarget(true);
    }

    private void EnterAttack(string reason)
    {
        SetState(State.Attack, reason);
        StopMoving(reason);
        attackPhase = AttackPhase.Windup;
        hasAppliedAttackDamage = false;
        attackAttemptCount++;
        RefreshAggroMemory("AttackEnter", true);
        FaceTarget(true, attackWindupDuration + attackCommitDuration);
        stateEndTime = Time.time + attackWindupDuration;
        attackHitTime = stateEndTime + attackCommitDuration * 0.35f;
        nextAttackTime = Time.time + attackWindupDuration + attackCommitDuration + currentAttackRecoverDuration + currentAttackCooldown;
        animatorDriver?.TriggerAttack();
        LogAttack("attempt");
    }

    private void EnterRecover(string reason)
    {
        SetState(State.Recover, reason);
        attackPhase = AttackPhase.None;
        StopMoving(reason);
        RefreshAggroMemory("RecoverEnter", true);
        stateEndTime = Time.time + currentAttackRecoverDuration;
    }

    private void EnterHurt()
    {
        if (state == State.Dead)
        {
            return;
        }

        SetState(State.Hurt, "HurtFlash");
        attackPhase = AttackPhase.None;
        StopMoving("HurtFlash");
        stateEndTime = Time.time + hurtStunDuration;
        SetMoveIntent(Vector2.zero, true);
    }

    private void EnterDead()
    {
        SetState(State.Dead, "DeathNotifier");
        chaseZone = ChaseZone.None;
        attackPhase = AttackPhase.None;
        StopMoving("Dead");
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
        state = newState;
        stateEnterTime = Time.time;
        LogStateChange(previousState, newState, reason, stateAge);
    }

    private void SetChaseZone(ChaseZone newZone)
    {
        if (chaseZone == newZone)
        {
            return;
        }

        ChaseZone previousZone = chaseZone;
        chaseZone = newZone;
        if (enableDebugLogs && debugChaseZones)
        {
            float distanceToPlayer = GetDistanceToPlayer();
            Debug.Log(
                $"[SlimeAI] {name} chaseZone {previousZone}->{newZone} dist={(float.IsInfinity(distanceToPlayer) ? -1f : distanceToPlayer):F2} attackRange={attackRange:F2} attackExitRange={attackExitRange:F2}",
                this);
        }
    }

    private ChaseZone ResolveChaseZone(float distanceToPlayer)
    {
        if (distanceToPlayer <= attackRange)
        {
            return ChaseZone.AttackZone;
        }

        if (chaseZone == ChaseZone.AttackZone)
        {
            return ChaseZone.MeleeHold;
        }

        if (chaseZone == ChaseZone.MeleeHold)
        {
            float meleeHoldExitRange = attackExitRange + meleeHoldExitBuffer;
            return distanceToPlayer > meleeHoldExitRange ? ChaseZone.FarChase : ChaseZone.MeleeHold;
        }

        return distanceToPlayer <= attackExitRange ? ChaseZone.MeleeHold : ChaseZone.FarChase;
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

    private void RefreshChaseTarget(bool forceIntentUpdate = false)
    {
        Vector3 pursuitPosition = GetPursuitTarget();
        if (TryGetNearestWalkablePoint(pursuitPosition, targetSnapRadius, out Vector3 targetPosition))
        {
            SetMoveTarget(targetPosition, currentChaseSpeedMultiplier, targetPosition - transform.position, forceIntentUpdate);
            return;
        }

        StopMoving("NoWalkableTarget");
    }

    private void RefreshMeleeHoldTarget()
    {
        Vector3 pursuitPosition = GetPursuitTarget();
        if (TryGetNearestWalkablePoint(pursuitPosition, targetSnapRadius, out Vector3 targetPosition))
        {
            SetMoveTarget(targetPosition, meleeHoldSpeedMultiplier, targetPosition - transform.position);
            return;
        }

        StopMoving("MeleeHoldNoWalkableTarget");
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
        nextPathRefreshTime = Time.time + UnityEngine.Random.Range(pathRefreshIntervalMin, pathRefreshIntervalMax);
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
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        SetFacing(randomDirection, false);
    }

    private void FaceTarget(bool force, float customLockDuration = -1f)
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
        if (playerTransform == null || distanceToPlayer > currentDetectionRange)
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

    private bool HasAggroTarget(float distanceToPlayer)
    {
        return playerTransform != null && distanceToPlayer <= loseInterestRange && HasAggroMemory();
    }

    private void RefreshAggroIfRelevant(float distanceToPlayer, string reason)
    {
        if (playerTransform != null && distanceToPlayer <= loseInterestRange)
        {
            RefreshAggroMemory(reason);
        }
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

    private void ScheduleNextThink()
    {
        nextThinkTime = Time.time + UnityEngine.Random.Range(minThinkInterval, maxThinkInterval);
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
            point = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * radius);
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
                return playerTransform.position;
            }
        }

        if (HasRecentPlayerMemory() || HasAggroMemory())
        {
            return lastKnownPlayerPosition;
        }

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
            case State.Chase:
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

    public void ApplyDdaProfile(DdaDifficultyProfile profile)
    {
        if (!ddaRuntimeValuesInitialized)
        {
            InitializeDdaRuntimeValues();
        }

        if (profile == null)
        {
            profile = DdaDifficultyProfile.Balanced();
        }

        currentChaseSpeedMultiplier = baseChaseSpeedMultiplier * DdaDifficultyProfile.ClampChaseSpeed(profile.chaseSpeedMultiplier);
        currentAttackCooldown = baseAttackCooldown * DdaDifficultyProfile.ClampAttackCooldown(profile.attackCooldownMultiplier);
        currentAttackRecoverDuration = baseAttackRecoverDuration * DdaDifficultyProfile.ClampRecoveryTime(profile.recoveryTimeMultiplier);
        currentDetectionRange = baseDetectionRange * DdaDifficultyProfile.ClampDetectionRange(profile.detectionRangeMultiplier);

        Debug.Log(
            $"[DDA] SlimeAI {name} profile={profile.profileName} " +
            $"chase={baseChaseSpeedMultiplier:0.##}->{currentChaseSpeedMultiplier:0.##} " +
            $"cooldown={baseAttackCooldown:0.##}->{currentAttackCooldown:0.##} " +
            $"recovery={baseAttackRecoverDuration:0.##}->{currentAttackRecoverDuration:0.##} " +
            $"detection={baseDetectionRange:0.##}->{currentDetectionRange:0.##}",
            this);
    }

    private void InitializeDdaRuntimeValues()
    {
        baseChaseSpeedMultiplier = chaseSpeedMultiplier;
        baseAttackCooldown = attackCooldown;
        baseAttackRecoverDuration = attackRecoverDuration;
        baseDetectionRange = detectionRange;

        currentChaseSpeedMultiplier = baseChaseSpeedMultiplier;
        currentAttackCooldown = baseAttackCooldown;
        currentAttackRecoverDuration = baseAttackRecoverDuration;
        currentDetectionRange = baseDetectionRange;
        ddaRuntimeValuesInitialized = true;
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
            $"[SlimeAI] {name} {previousState}->{newState} reason={reason} dist={(float.IsInfinity(distanceToPlayer) ? -1f : distanceToPlayer):F2} age={stateAge:F2} hasTarget={slimePathFinding != null && slimePathFinding.HasTarget} pathTarget={pathTarget} moveStatus={(slimePathFinding != null ? slimePathFinding.CurrentStatus.ToString() : "None")} pos={currentPos:F2} playerPos={playerPos} atkAttempts={attackAttemptCount} atkHits={attackHitCount} lostTargets={lostTargetCount}",
            this);
    }

    private void LogPerception(string message)
    {
        if (!enableDebugLogs || !debugPerception)
        {
            return;
        }

        Debug.Log(
            $"[SlimeAI] {name} perception {message} lastSeenAge={(Time.time - lastSeenPlayerTime):F2} aggroRemaining={(combatEngagedUntil - Time.time):F2} pos={GetCurrentWorldPosition():F2} lastKnown={lastKnownPlayerPosition:F2}",
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
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? currentDetectionRange : detectionRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseInterestRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackExitRange);

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

    private void EnsureBehaviorProfile()
    {
        if (behaviorProfile == null || behaviorProfile.LooksUninitialized())
        {
            behaviorProfile = BuildProfileFromLegacyFields();
        }

        behaviorProfile.Validate();
        ApplyBehaviorProfileToFields();
    }

    private SlimeBehaviorProfileData BuildProfileFromLegacyFields()
    {
        var data = new SlimeBehaviorProfileData();
        if (detectionRange <= 0f)
        {
            data.ApplyPresetDefaults();
            data.Validate();
            return data;
        }

        data = new SlimeBehaviorProfileData();
        typeof(SlimeBehaviorProfileData).GetField("preset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, preset);
        typeof(SlimeBehaviorProfileData).GetField("detectionRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, detectionRange);
        typeof(SlimeBehaviorProfileData).GetField("loseInterestRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, loseInterestRange);
        typeof(SlimeBehaviorProfileData).GetField("aggroMemoryDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, aggroMemoryDuration);
        typeof(SlimeBehaviorProfileData).GetField("lostSightGraceTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, lostSightGraceTime);
        typeof(SlimeBehaviorProfileData).GetField("requireLineOfSight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, requireLineOfSight);
        typeof(SlimeBehaviorProfileData).GetField("chaseSpeedMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, chaseSpeedMultiplier);
        typeof(SlimeBehaviorProfileData).GetField("wanderSpeedMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, wanderSpeedMultiplier);
        typeof(SlimeBehaviorProfileData).GetField("wanderRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, wanderRadius);
        typeof(SlimeBehaviorProfileData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackRange);
        typeof(SlimeBehaviorProfileData).GetField("attackExitRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackExitRange);
        typeof(SlimeBehaviorProfileData).GetField("meleeHoldExitBuffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, meleeHoldExitBuffer);
        typeof(SlimeBehaviorProfileData).GetField("meleeHoldSpeedMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, meleeHoldSpeedMultiplier);
        typeof(SlimeBehaviorProfileData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackDamage);
        typeof(SlimeBehaviorProfileData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackCooldown);
        typeof(SlimeBehaviorProfileData).GetField("attackHitRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackHitRadius);
        typeof(SlimeBehaviorProfileData).GetField("attackHitOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackHitOffset);
        typeof(SlimeBehaviorProfileData).GetField("attackKnockbackForce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackKnockbackForce);
        typeof(SlimeBehaviorProfileData).GetField("reactionDelayMin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, minAggroReactionDelay);
        typeof(SlimeBehaviorProfileData).GetField("reactionDelayMax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, maxAggroReactionDelay);
        typeof(SlimeBehaviorProfileData).GetField("recoverDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackRecoverDuration);
        typeof(SlimeBehaviorProfileData).GetField("hurtDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, hurtStunDuration);
        typeof(SlimeBehaviorProfileData).GetField("minThinkInterval", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, minThinkInterval);
        typeof(SlimeBehaviorProfileData).GetField("maxThinkInterval", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, maxThinkInterval);
        typeof(SlimeBehaviorProfileData).GetField("pathRefreshIntervalMin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, pathRefreshIntervalMin);
        typeof(SlimeBehaviorProfileData).GetField("pathRefreshIntervalMax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, pathRefreshIntervalMax);
        typeof(SlimeBehaviorProfileData).GetField("facingLockDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, facingLockDuration);
        typeof(SlimeBehaviorProfileData).GetField("moveIntentLockDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, moveIntentLockDuration);
        typeof(SlimeBehaviorProfileData).GetField("walkablePointAttempts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, walkablePointAttempts);
        typeof(SlimeBehaviorProfileData).GetField("attackWindupDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackWindupDuration);
        typeof(SlimeBehaviorProfileData).GetField("attackCommitDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, attackCommitDuration);
        typeof(SlimeBehaviorProfileData).GetField("idleDurationMin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, idleDurationMin);
        typeof(SlimeBehaviorProfileData).GetField("idleDurationMax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, idleDurationMax);
        typeof(SlimeBehaviorProfileData).GetField("wanderDurationMin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, wanderDurationMin);
        typeof(SlimeBehaviorProfileData).GetField("wanderDurationMax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, wanderDurationMax);
        typeof(SlimeBehaviorProfileData).GetField("spawnSnapRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, spawnSnapRadius);
        typeof(SlimeBehaviorProfileData).GetField("targetSnapRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, targetSnapRadius);
        data.Validate();
        return data;
    }

    private void ApplyBehaviorProfileToFields()
    {
        preset = behaviorProfile.Preset;
        detectionRange = behaviorProfile.DetectionRange;
        loseInterestRange = behaviorProfile.LoseInterestRange;
        aggroMemoryDuration = behaviorProfile.AggroMemoryDuration;
        lostSightGraceTime = behaviorProfile.LostSightGraceTime;
        requireLineOfSight = behaviorProfile.RequireLineOfSight;
        chaseSpeedMultiplier = behaviorProfile.ChaseSpeedMultiplier;
        wanderSpeedMultiplier = behaviorProfile.WanderSpeedMultiplier;
        wanderRadius = behaviorProfile.WanderRadius;
        attackRange = behaviorProfile.AttackRange;
        attackExitRange = behaviorProfile.AttackExitRange;
        meleeHoldExitBuffer = behaviorProfile.MeleeHoldExitBuffer;
        meleeHoldSpeedMultiplier = behaviorProfile.MeleeHoldSpeedMultiplier;
        attackDamage = behaviorProfile.AttackDamage;
        attackCooldown = behaviorProfile.AttackCooldown;
        attackHitRadius = behaviorProfile.AttackHitRadius;
        attackHitOffset = behaviorProfile.AttackHitOffset;
        attackKnockbackForce = behaviorProfile.AttackKnockbackForce;
        minAggroReactionDelay = behaviorProfile.ReactionDelayMin;
        maxAggroReactionDelay = behaviorProfile.ReactionDelayMax;
        attackRecoverDuration = behaviorProfile.RecoverDuration;
        hurtStunDuration = behaviorProfile.HurtDuration;
        minThinkInterval = behaviorProfile.MinThinkInterval;
        maxThinkInterval = behaviorProfile.MaxThinkInterval;
        pathRefreshIntervalMin = behaviorProfile.PathRefreshIntervalMin;
        pathRefreshIntervalMax = behaviorProfile.PathRefreshIntervalMax;
        facingLockDuration = behaviorProfile.FacingLockDuration;
        moveIntentLockDuration = behaviorProfile.MoveIntentLockDuration;
        walkablePointAttempts = behaviorProfile.WalkablePointAttempts;
        attackWindupDuration = behaviorProfile.AttackWindupDuration;
        attackCommitDuration = behaviorProfile.AttackCommitDuration;
        idleDurationMin = behaviorProfile.IdleDurationMin;
        idleDurationMax = behaviorProfile.IdleDurationMax;
        wanderDurationMin = behaviorProfile.WanderDurationMin;
        wanderDurationMax = behaviorProfile.WanderDurationMax;
        spawnSnapRadius = behaviorProfile.SpawnSnapRadius;
        targetSnapRadius = behaviorProfile.TargetSnapRadius;
    }

    private static EnemyAIState ToEnemyAIState(State slimeState)
    {
        switch (slimeState)
        {
            case State.Chase:
            case State.Alert:
                return EnemyAIState.Chasing;
            case State.Attack:
                return EnemyAIState.Attacking;
            case State.Recover:
                return EnemyAIState.Recovering;
            case State.Hurt:
                return EnemyAIState.Hurt;
            case State.Dead:
                return EnemyAIState.Dead;
            case State.Idle:
            case State.Wander:
            default:
                return EnemyAIState.Idle;
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
