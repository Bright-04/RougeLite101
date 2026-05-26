using System.Collections.Generic;
using UnityEngine;
using RougeLite.Combat.Damage;

public class MeleeWeapon : Weapon
{
    private const float ProceduralSlashWindupDegrees = 28f;
    private const float ProceduralSlashFollowThroughDegrees = 96f;
    private const float ProceduralThrustPullbackFactor = 0.1f;
    private const float ProceduralThrustLungeFactor = 0.35f;
    private const float ProceduralThrustStretch = 1.12f;
    private const float DefaultSlashVfxLifetime = 0.12f;

    private enum ProceduralAttackPhase
    {
        None,
        Anticipation,
        Active,
        Recovery
    }

    [Header("Melee")]
    [SerializeField] protected float attackCooldown = 0.5f;
    [SerializeField] protected float colliderDistance = 0.15f;
    [SerializeField] protected Vector3 weaponColliderScale = new Vector3(2f, 2f, 1f);
    [SerializeField] protected Transform weaponCollider;
    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private Transform weaponHolder;

    protected float nextAttackTime = 0f;

    private readonly HashSet<int> proceduralHitTargets = new HashSet<int>();

    private Animator animator;
    private PlayerMovement playerMovement;
    private WeaponController weaponController;
    private GameObject slashAnim;
    private ProceduralAttackPhase proceduralPhase;
    private Vector2 proceduralAimDirection = Vector2.right;
    private float proceduralPhaseStartTime;
    private int proceduralSlashDirection = 1;
    private bool proceduralActiveFxSpawned;
    private SpriteRenderer pulsedWeaponRenderer;
    private Vector3 pulsedWeaponRendererBaseScale = Vector3.one;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        weaponController = GetComponentInParent<WeaponController>();
        animator = GetComponent<Animator>();

        if (weaponHolder == null)
        {
            weaponHolder = transform.parent;
        }

        if (weaponCollider != null && playerMovement != null)
        {
            weaponCollider.SetParent(playerMovement.transform);
            weaponCollider.localScale = weaponColliderScale;
            weaponCollider.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        ResetProceduralWeaponPulse();

        if (weaponCollider != null)
        {
            Destroy(weaponCollider.gameObject);
        }
    }

    private void Update()
    {
        UpdateProceduralAttackState();

        if (!UsesProceduralPresetMelee())
        {
            FollowPlayerDirection();
        }
        else if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(false);
        }
    }

    public override void Initialize(WeaponDefinitionSO definition)
    {
        base.Initialize(definition);

        if (definition == null)
        {
            return;
        }

        attackCooldown = definition.Cooldown;
        colliderDistance = definition.HitboxDistance;
        weaponColliderScale = definition.HitboxScale;
        ApplyHitboxDefinition(definition);

        if (UsesProceduralPresetMelee() && weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(false);
        }
    }

    public override void Use()
    {
        Attack();
    }

    public override bool TryGetPoseAimDirectionOverride(out Vector2 aimDirection)
    {
        if (proceduralPhase != ProceduralAttackPhase.None)
        {
            aimDirection = proceduralAimDirection;
            return true;
        }

        aimDirection = default;
        return false;
    }

    public bool TryGetProceduralAttackPhase(out string phaseLabel)
    {
        phaseLabel = proceduralPhase.ToString();
        return proceduralPhase != ProceduralAttackPhase.None;
    }

    public override WeaponAlignmentPose AdjustPose(WeaponAlignmentPose pose)
    {
        if (!UsesProceduralPresetMelee() || proceduralPhase == ProceduralAttackPhase.None)
        {
            return pose;
        }

        WeaponRigRuntimeResolution resolution = GetResolvedRig();
        switch (weaponDefinition.AttackType)
        {
            case WeaponAttackType.Slash:
                return BuildProceduralSlashPose(pose, resolution);
            case WeaponAttackType.Thrust:
                return BuildProceduralThrustPose(pose, resolution);
            default:
                return pose;
        }
    }

    private void Attack()
    {
        if (!CanAttack())
        {
            return;
        }

        if (UsesProceduralPresetMelee())
        {
            BeginProceduralAttack();
            return;
        }

        if (animator == null || weaponCollider == null)
        {
            return;
        }

        animator.SetTrigger("Attack");
        weaponCollider.gameObject.SetActive(true);

        if (slashAnimPrefab != null)
        {
            Vector3 spawnPosition = GetCurrentSlashPose().SlashOrigin;
            slashAnim = SlashEffectPool.Instance.Get(slashAnimPrefab, spawnPosition, Quaternion.identity, transform.parent);
            SyncSlashSortingWithWeapon();
        }
    }

    public void DoneAttackingAnimEvent()
    {
        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(false);
        }
    }

    public void SwingUpFlipAnimEvent()
    {
        if (slashAnim == null)
        {
            return;
        }

        slashAnim.transform.rotation = Quaternion.Euler(-180f, 0f, 0f);
        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (slashRenderer != null)
        {
            slashRenderer.flipX = playerMovement != null && playerMovement.FacingLeft;
        }
    }

    public void SwingDownFlipAnimEvent()
    {
        if (slashAnim == null)
        {
            return;
        }

        slashAnim.transform.rotation = Quaternion.identity;
        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (slashRenderer != null)
        {
            slashRenderer.flipX = playerMovement != null && playerMovement.FacingLeft;
        }
    }

    protected bool CanAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        nextAttackTime = Time.time + attackCooldown;
        return true;
    }

    protected void ApplyHitboxDefinition(WeaponDefinitionSO definition)
    {
        if (weaponCollider == null || definition == null)
        {
            return;
        }

        weaponCollider.localScale = weaponColliderScale;

        DamageSource damageSource = weaponCollider.GetComponent<DamageSource>();
        if (damageSource != null)
        {
            damageSource.SetBaseDamage(definition.Damage);
        }
    }

    private bool UsesProceduralPresetMelee()
    {
        if (weaponDefinition == null || !weaponDefinition.IsMeleeAttack)
        {
            return false;
        }

        return GetResolvedRig().ResolvedMode == WeaponRigPointSourceMode.UsePresetRig;
    }

    private void BeginProceduralAttack()
    {
        proceduralHitTargets.Clear();
        proceduralAimDirection = playerMovement != null && playerMovement.LastAimDirection.sqrMagnitude > 0.0001f
            ? playerMovement.LastAimDirection.normalized
            : Vector2.right;
        proceduralActiveFxSpawned = false;

        if (weaponDefinition != null && weaponDefinition.AttackType == WeaponAttackType.Slash)
        {
            proceduralSlashDirection *= -1;
        }

        EnterProceduralPhase(ProceduralAttackPhase.Anticipation);
    }

    private void UpdateProceduralAttackState()
    {
        if (proceduralPhase == ProceduralAttackPhase.None || weaponDefinition == null)
        {
            return;
        }

        switch (proceduralPhase)
        {
            case ProceduralAttackPhase.Anticipation:
                if (GetCurrentPhaseDuration() <= 0f || GetCurrentPhaseElapsed() >= GetCurrentPhaseDuration())
                {
                    EnterProceduralPhase(ProceduralAttackPhase.Active);
                }
                break;

            case ProceduralAttackPhase.Active:
                PerformProceduralHitDetection();
                if (!proceduralActiveFxSpawned && weaponDefinition.AttackType == WeaponAttackType.Slash)
                {
                    SpawnProceduralSlashVfx();
                    proceduralActiveFxSpawned = true;
                }

                if (GetCurrentPhaseDuration() <= 0f || GetCurrentPhaseElapsed() >= GetCurrentPhaseDuration())
                {
                    EnterProceduralPhase(ProceduralAttackPhase.Recovery);
                }
                break;

            case ProceduralAttackPhase.Recovery:
                if (GetCurrentPhaseDuration() <= 0f || GetCurrentPhaseElapsed() >= GetCurrentPhaseDuration())
                {
                    EnterProceduralPhase(ProceduralAttackPhase.None);
                }
                break;
        }

        UpdateProceduralWeaponPulse();
    }

    private void EnterProceduralPhase(ProceduralAttackPhase nextPhase)
    {
        proceduralPhase = nextPhase;
        proceduralPhaseStartTime = Time.time;

        if (proceduralPhase == ProceduralAttackPhase.None)
        {
            proceduralActiveFxSpawned = false;
            ResetProceduralWeaponPulse();
        }
    }

    private void PerformProceduralHitDetection()
    {
        WeaponAlignmentPose pose = GetProceduralAttackPose();
        switch (weaponDefinition.AttackType)
        {
            case WeaponAttackType.Slash:
                PerformSlashHitDetection(pose);
                break;

            case WeaponAttackType.Thrust:
                PerformThrustHitDetection(pose);
                break;
        }
    }

    private WeaponAlignmentPose GetProceduralAttackPose()
    {
        WeaponAlignmentPose basePose = weaponController != null
            ? weaponController.CalculatePoseForDefinition(weaponDefinition, proceduralAimDirection)
            : WeaponAlignmentUtility.CalculateWeaponPose(transform.position, proceduralAimDirection, weaponDefinition, GetComponentInChildren<WeaponRig>(true));

        return AdjustPose(basePose);
    }

    private void PerformSlashHitDetection(WeaponAlignmentPose pose)
    {
        float range = Mathf.Max(0.05f, weaponDefinition.SlashRange);
        float halfArc = Mathf.Max(1f, weaponDefinition.SlashArcDegrees) * 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pose.SlashOrigin, range);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!IsValidProceduralTarget(hit, pose.SlashOrigin, out IDamageable damageable, out Component damageableComponent, out Vector2 targetPoint))
            {
                continue;
            }

            Vector2 toTarget = targetPoint - (Vector2)pose.SlashOrigin;
            if (toTarget.sqrMagnitude > range * range)
            {
                continue;
            }

            if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(proceduralAimDirection, toTarget.normalized) > halfArc)
            {
                continue;
            }

            ApplyProceduralDamage(damageable, damageableComponent);
        }
    }

    private void PerformThrustHitDetection(WeaponAlignmentPose pose)
    {
        float distance = Mathf.Max(0.05f, weaponDefinition.ThrustDistance);
        float width = Mathf.Max(0.05f, weaponDefinition.ThrustWidth);
        Vector2 center = (Vector2)pose.WeaponAnchorPosition + proceduralAimDirection * (distance * 0.5f);
        float angle = Mathf.Atan2(proceduralAimDirection.y, proceduralAimDirection.x) * Mathf.Rad2Deg;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(distance, width), angle);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!IsValidProceduralTarget(hit, center, out IDamageable damageable, out Component damageableComponent, out _))
            {
                continue;
            }

            ApplyProceduralDamage(damageable, damageableComponent);
        }
    }

    private bool IsValidProceduralTarget(Collider2D target, Vector2 origin, out IDamageable damageable, out Component damageableComponent, out Vector2 targetPoint)
    {
        damageable = null;
        damageableComponent = null;
        targetPoint = origin;

        if (target == null)
        {
            return false;
        }

        if (playerMovement != null && target.transform.IsChildOf(playerMovement.transform))
        {
            return false;
        }

        if (!TryResolveDamageable(target, out damageable, out damageableComponent))
        {
            return false;
        }

        int targetId = damageableComponent.gameObject.GetInstanceID();
        if (proceduralHitTargets.Contains(targetId))
        {
            return false;
        }

        targetPoint = target.ClosestPoint(origin);
        if ((targetPoint - origin).sqrMagnitude < 0.0001f)
        {
            targetPoint = damageableComponent.transform.position;
        }

        return true;
    }

    private void ApplyProceduralDamage(IDamageable damageable, Component damageableComponent)
    {
        if (damageable == null || damageableComponent == null)
        {
            return;
        }

        proceduralHitTargets.Add(damageableComponent.gameObject.GetInstanceID());
        damageable.TakeDamage(ComputeFinalDamage());

        if (weaponDefinition != null && weaponDefinition.Knockback > 0f)
        {
            Knockback knockback = damageableComponent.GetComponent<Knockback>();
            if (knockback == null)
            {
                knockback = damageableComponent.GetComponentInParent<Knockback>();
            }

            if (knockback != null)
            {
                Transform source = playerMovement != null ? playerMovement.transform : transform;
                knockback.GetKnockedBack(source, weaponDefinition.Knockback);
            }
        }
    }

    private int ComputeFinalDamage()
    {
        float finalDamage = weaponDefinition != null ? weaponDefinition.Damage : 1f;
        PlayerStats stats = playerMovement != null ? playerMovement.GetComponent<PlayerStats>() : null;
        if (stats == null && PlayerMovement.Instance != null)
        {
            stats = PlayerMovement.Instance.GetComponent<PlayerStats>();
        }

        if (stats != null)
        {
            finalDamage += stats.attackDamage;
            if (stats.TryCrit())
            {
                finalDamage *= stats.GetCritMultiplier();
            }
        }

        return Mathf.RoundToInt(finalDamage);
    }

    private static bool TryResolveDamageable(Collider2D target, out IDamageable damageable, out Component damageableComponent)
    {
        damageable = target.GetComponent<IDamageable>();
        damageableComponent = damageable as Component;
        if (damageableComponent != null)
        {
            return true;
        }

        damageable = target.GetComponentInParent<IDamageable>();
        damageableComponent = damageable as Component;
        return damageableComponent != null;
    }

    private void SpawnProceduralSlashVfx()
    {
        if (slashAnimPrefab == null)
        {
            return;
        }

        WeaponAlignmentPose pose = GetProceduralAttackPose();
        slashAnim = SlashEffectPool.Instance.Get(slashAnimPrefab, pose.SlashOrigin, pose.WeaponRotation, transform.parent);
        if (slashAnim == null)
        {
            return;
        }

        Animator slashAnimator = slashAnim.GetComponent<Animator>();
        if (slashAnimator != null)
        {
            slashAnimator.speed = 3f;
        }

        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (slashRenderer != null)
        {
            slashRenderer.flipX = proceduralSlashDirection < 0;
        }

        float rangeScale = Mathf.Lerp(0.9f, 1.15f, Mathf.Clamp01((weaponDefinition.SlashRange - 0.75f) / 0.75f));
        float startScaleMultiplier = weaponDefinition != null && weaponDefinition.SlashVfxStartScaleMultiplier > 0f
            ? weaponDefinition.SlashVfxStartScaleMultiplier
            : 1f;
        float endScaleMultiplier = weaponDefinition != null && weaponDefinition.SlashVfxEndScaleMultiplier > 0f
            ? weaponDefinition.SlashVfxEndScaleMultiplier
            : startScaleMultiplier;
        float lifetime = weaponDefinition != null && weaponDefinition.SlashVfxLifetime > 0f
            ? weaponDefinition.SlashVfxLifetime
            : DefaultSlashVfxLifetime;
        Quaternion slashRotation = pose.WeaponRotation * Quaternion.Euler(0f, 0f, proceduralSlashDirection > 0 ? 12f : -12f);
        Vector3 startScale = Vector3.one * (rangeScale * startScaleMultiplier);
        Vector3 endScale = Vector3.one * (rangeScale * endScaleMultiplier);

        SlashAnim proceduralSlashAnim = slashAnim.GetComponent<SlashAnim>();
        if (proceduralSlashAnim != null)
        {
            proceduralSlashAnim.Play(lifetime, startScale, endScale, slashRotation, weaponDefinition != null && weaponDefinition.SlashVfxFadeOut);
        }
        else
        {
            slashAnim.transform.rotation = slashRotation;
            slashAnim.transform.localScale = startScale;
        }

        SyncSlashSortingWithWeapon();
    }

    private WeaponAlignmentPose BuildProceduralSlashPose(WeaponAlignmentPose basePose, WeaponRigRuntimeResolution resolution)
    {
        float phaseProgress = GetCurrentPhaseProgress();
        float angleOffset = 0f;
        float slashDirection = proceduralSlashDirection;
        float windupDegrees = ProceduralSlashWindupDegrees + Mathf.Max(0f, weaponDefinition.SlashVisualExtraAnticipationDegrees);
        float followThroughDegrees = ProceduralSlashFollowThroughDegrees + Mathf.Max(0f, weaponDefinition.SlashVisualExtraFollowThroughDegrees);

        switch (proceduralPhase)
        {
            case ProceduralAttackPhase.Anticipation:
                angleOffset = -slashDirection * windupDegrees * EaseOutQuad(phaseProgress);
                break;

            case ProceduralAttackPhase.Active:
                angleOffset = Mathf.Lerp(
                    -slashDirection * windupDegrees,
                    slashDirection * followThroughDegrees,
                    EaseOutQuint(phaseProgress));
                break;

            case ProceduralAttackPhase.Recovery:
                angleOffset = Mathf.Lerp(
                    slashDirection * followThroughDegrees,
                    0f,
                    EaseOutCubic(phaseProgress));
                break;
        }

        Quaternion rotation = basePose.WeaponRotation * Quaternion.Euler(0f, 0f, angleOffset);
        Vector3 weaponPosition = CalculateGripAnchoredPosition(basePose.WeaponAnchorPosition, rotation, resolution.GripPoint, basePose.VisualScale);
        return BuildAdjustedPose(basePose, resolution, weaponPosition, rotation, basePose.VisualScale);
    }

    private WeaponAlignmentPose BuildProceduralThrustPose(WeaponAlignmentPose basePose, WeaponRigRuntimeResolution resolution)
    {
        float phaseProgress = GetCurrentPhaseProgress();
        float thrustDistance = Mathf.Max(0.05f, weaponDefinition.ThrustDistance);
        float pullbackFactor = weaponDefinition.ThrustVisualPullbackFactor > 0f
            ? weaponDefinition.ThrustVisualPullbackFactor
            : ProceduralThrustPullbackFactor;
        float lungeFactor = weaponDefinition.ThrustVisualLungeFactor > 0f
            ? weaponDefinition.ThrustVisualLungeFactor
            : ProceduralThrustLungeFactor;
        float stretchTarget = weaponDefinition.ThrustVisualStretchFactor > 0f
            ? weaponDefinition.ThrustVisualStretchFactor
            : ProceduralThrustStretch;
        float offsetDistance = 0f;
        float stretchFactor = 1f;

        switch (proceduralPhase)
        {
            case ProceduralAttackPhase.Anticipation:
                offsetDistance = -thrustDistance * pullbackFactor * EaseOutQuad(phaseProgress);
                break;

            case ProceduralAttackPhase.Active:
                offsetDistance = Mathf.Lerp(
                    -thrustDistance * pullbackFactor,
                    thrustDistance * lungeFactor,
                    EaseOutQuint(phaseProgress));
                stretchFactor = Mathf.Lerp(1f, stretchTarget, EaseOutQuint(phaseProgress));
                break;

            case ProceduralAttackPhase.Recovery:
                offsetDistance = Mathf.Lerp(
                    thrustDistance * lungeFactor,
                    0f,
                    EaseOutCubic(phaseProgress));
                stretchFactor = Mathf.Lerp(stretchTarget, 1f, EaseOutCubic(phaseProgress));
                break;
        }

        Vector3 visualScale = basePose.VisualScale;
        visualScale.x = Mathf.Sign(visualScale.x) * Mathf.Abs(visualScale.x) * stretchFactor;

        Vector3 weaponPosition = CalculateGripAnchoredPosition(basePose.WeaponAnchorPosition, basePose.WeaponRotation, resolution.GripPoint, visualScale);
        weaponPosition += (Vector3)(proceduralAimDirection * offsetDistance);
        return BuildAdjustedPose(basePose, resolution, weaponPosition, basePose.WeaponRotation, visualScale);
    }

    private WeaponAlignmentPose BuildAdjustedPose(
        WeaponAlignmentPose basePose,
        WeaponRigRuntimeResolution resolution,
        Vector3 weaponPosition,
        Quaternion weaponRotation,
        Vector3 visualScale)
    {
        basePose.WeaponPosition = weaponPosition;
        basePose.WeaponRotation = weaponRotation;
        basePose.VisualScale = visualScale;
        basePose.GripPoint = TransformLocalPoint(weaponPosition, weaponRotation, resolution.GripPoint, visualScale);
        basePose.MuzzleTipPoint = TransformLocalPoint(weaponPosition, weaponRotation, resolution.TipPoint, visualScale);
        basePose.ProjectileSpawnPoint = TransformLocalPoint(weaponPosition, weaponRotation, resolution.ProjectileSpawnPoint, visualScale);
        basePose.SlashOrigin = TransformLocalPoint(weaponPosition, weaponRotation, resolution.SlashOrigin, visualScale);
        basePose.SlashArcStart = TransformLocalPoint(weaponPosition, weaponRotation, resolution.SlashArcStart, visualScale);
        basePose.SlashArcEnd = TransformLocalPoint(weaponPosition, weaponRotation, resolution.SlashArcEnd, visualScale);
        return basePose;
    }

    private static Vector3 CalculateGripAnchoredPosition(Vector3 anchor, Quaternion rotation, Vector3 gripLocalPoint, Vector3 visualScale)
    {
        return anchor - rotation * ScaleLocalPoint(gripLocalPoint, visualScale);
    }

    private static Vector3 TransformLocalPoint(Vector3 weaponPosition, Quaternion rotation, Vector3 localPoint, Vector3 visualScale)
    {
        return weaponPosition + rotation * ScaleLocalPoint(localPoint, visualScale);
    }

    private static Vector3 ScaleLocalPoint(Vector3 localPoint, Vector3 visualScale)
    {
        return Vector3.Scale(localPoint, visualScale);
    }

    private WeaponRigRuntimeResolution GetResolvedRig()
    {
        if (weaponController != null)
        {
            return weaponController.CurrentRigResolution;
        }

        return WeaponAlignmentUtility.ResolveRuntimeRig(weaponDefinition, GetComponentInChildren<WeaponRig>(true));
    }

    private float GetCurrentPhaseElapsed()
    {
        return Time.time - proceduralPhaseStartTime;
    }

    private float GetCurrentPhaseDuration()
    {
        if (weaponDefinition == null)
        {
            return 0f;
        }

        return proceduralPhase switch
        {
            ProceduralAttackPhase.Anticipation => weaponDefinition.AnticipationDuration,
            ProceduralAttackPhase.Active => weaponDefinition.ActiveDuration,
            ProceduralAttackPhase.Recovery => weaponDefinition.RecoveryDuration,
            _ => 0f
        };
    }

    private float GetCurrentPhaseProgress()
    {
        float duration = GetCurrentPhaseDuration();
        if (duration <= 0.0001f)
        {
            return 1f;
        }

        return Mathf.Clamp01(GetCurrentPhaseElapsed() / duration);
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }

    private static float EaseOutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - ((1f - t) * (1f - t));
    }

    private static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    private static float EaseOutQuint(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 5f);
    }

    private void UpdateProceduralWeaponPulse()
    {
        if (proceduralPhase == ProceduralAttackPhase.None || weaponDefinition == null)
        {
            ResetProceduralWeaponPulse();
            return;
        }

        float pulseBlend = Mathf.Clamp01(weaponDefinition.MeleeVisualPulseBlend);
        float pulseAmount = Mathf.Max(0f, weaponDefinition.MeleeVisualPulseScaleAmount);
        if (proceduralPhase != ProceduralAttackPhase.Active || pulseBlend <= 0f || pulseAmount <= 0f)
        {
            ApplyProceduralWeaponPulse(1f);
            return;
        }

        float pulse = 1f + (Mathf.Sin(GetCurrentPhaseProgress() * Mathf.PI) * pulseAmount * pulseBlend);
        ApplyProceduralWeaponPulse(pulse);
    }

    private void ApplyProceduralWeaponPulse(float pulseMultiplier)
    {
        SpriteRenderer renderer = DisplayedSpriteRenderer != null ? DisplayedSpriteRenderer : GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            ResetProceduralWeaponPulse();
            return;
        }

        if (pulsedWeaponRenderer != renderer)
        {
            ResetProceduralWeaponPulse();
            pulsedWeaponRenderer = renderer;
            pulsedWeaponRendererBaseScale = renderer.transform.localScale;
        }

        renderer.transform.localScale = pulsedWeaponRendererBaseScale * pulseMultiplier;
    }

    private void ResetProceduralWeaponPulse()
    {
        if (pulsedWeaponRenderer != null)
        {
            pulsedWeaponRenderer.transform.localScale = pulsedWeaponRendererBaseScale;
            pulsedWeaponRenderer = null;
        }
    }

    private void SyncSlashSortingWithWeapon()
    {
        if (slashAnim == null)
        {
            return;
        }

        SpriteRenderer weaponRenderer = DisplayedSpriteRenderer != null ? DisplayedSpriteRenderer : GetComponent<SpriteRenderer>();
        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (weaponRenderer == null || slashRenderer == null)
        {
            return;
        }

        slashRenderer.sortingLayerID = weaponRenderer.sortingLayerID;
        slashRenderer.sortingOrder = weaponRenderer.sortingOrder;
    }

    private void FollowPlayerDirection()
    {
        if (playerMovement == null || weaponCollider == null)
        {
            return;
        }

        Vector2 aimDirection = playerMovement.LastAimDirection;
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 normalizedAim = aimDirection.normalized;

        WeaponAlignmentPose pose = GetSlashPose(normalizedAim);
        weaponCollider.position = pose.SlashOrigin;
        weaponCollider.rotation = Quaternion.Euler(0f, 0f, pose.AimAngle);
        weaponCollider.localScale = GetHitboxScaleFromSlashArc(pose);
    }

    private WeaponAlignmentPose GetCurrentSlashPose()
    {
        Vector2 aimDirection = playerMovement != null && playerMovement.LastAimDirection.sqrMagnitude > 0.0001f
            ? playerMovement.LastAimDirection.normalized
            : Vector2.right;

        return GetSlashPose(aimDirection);
    }

    private WeaponAlignmentPose GetSlashPose(Vector2 aimDirection)
    {
        if (weaponController == null)
        {
            weaponController = GetComponentInParent<WeaponController>();
        }

        if (weaponController != null)
        {
            return weaponController.CalculatePoseForDefinition(weaponDefinition, aimDirection);
        }

        return WeaponAlignmentUtility.CalculateWeaponPose(transform.position, aimDirection, weaponDefinition, GetComponentInChildren<WeaponRig>(true));
    }

    private Vector3 GetHitboxScaleFromSlashArc(WeaponAlignmentPose pose)
    {
        float arcWidth = Vector3.Distance(pose.SlashArcStart, pose.SlashArcEnd);
        float arcReach = Mathf.Max(
            Vector3.Distance(pose.SlashOrigin, pose.SlashArcStart),
            Vector3.Distance(pose.SlashOrigin, pose.SlashArcEnd));

        if (arcWidth < 0.0001f || arcReach < 0.0001f)
        {
            return weaponColliderScale;
        }

        return new Vector3(
            Mathf.Max(weaponColliderScale.x, arcReach),
            Mathf.Max(weaponColliderScale.y, arcWidth),
            weaponColliderScale.z);
    }

    private void OnDrawGizmos()
    {
        if (weaponCollider == null)
        {
            return;
        }

        Collider2D col = weaponCollider.GetComponent<Collider2D>();
        if (col == null)
        {
            return;
        }

        Gizmos.color = weaponCollider.gameObject.activeSelf ? Color.red : Color.yellow;
        Gizmos.matrix = weaponCollider.transform.localToWorldMatrix;

        if (col is PolygonCollider2D polyCol)
        {
            Vector2[] points = polyCol.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[(i + 1) % points.Length];
                Gizmos.DrawLine(start, end);
            }
        }
        else
        {
            Gizmos.DrawWireCube(col.offset, col.bounds.size);
        }

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(weaponCollider.position, 0.1f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            weaponCollider.position,
            "WeaponRig melee hitbox");
#endif
    }
}
