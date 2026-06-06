using System.Collections;
using UnityEngine;

public enum BatAIState
{
    Idle,
    Repositioning,
    Aiming,
    Firing,
    Recovering,
    Dead
}

public class BatAI : MonoBehaviour, IDdaAdaptiveEnemy
{
    private const float ThinkInterval = 0.05f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;

    [Header("Ranged Attack")]
    [SerializeField] private float preferredAttackRange = 5f;
    [SerializeField] private float minimumDistanceFromPlayer = 2.5f;
    [SerializeField] private float repositionSpeed = 3.5f;
    [SerializeField] private float shootCooldown = 1.5f;
    [SerializeField] private float aimWindupTime = 0.45f;
    [SerializeField] private float postShotRecoveryTime = 0.5f;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float projectileDamage = 8f;
    [SerializeField] private float projectileSpawnOffset = 0.9f;
    [SerializeField] private GameObject fireballProjectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Legacy Charge Tuning")]
    [SerializeField] private float prepareTime = 0.6f;
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private float chargeDuration = 0.5f;
    [SerializeField] private float cooldownTime = 1.0f;

    [Header("Visual Effects")]
    [SerializeField] private float prepareFlashSpeed = 10f;
    [SerializeField] private Color prepareColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color chargeColor = new Color(1f, 0f, 0f, 1f);

    [Header("Roaming")]
    [SerializeField] private float roamRadius = 2f;
    [SerializeField] private float roamSpeed = 1.5f;

    private BatAIState state;
    private BatPathFinding batPathFinding;
    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private EnemyDamageSource contactDamageSource;
    private Color originalColor;
    private Vector3 originalScale;
    private Vector2 roamCenter;
    private float roamAngle;
    private float nextShootTime;

    private float baseDetectionRange;
    private float basePreferredAttackRange;
    private float baseMinimumDistanceFromPlayer;
    private float baseRepositionSpeed;
    private float baseShootCooldown;
    private float baseAimWindupTime;
    private float basePostShotRecoveryTime;
    private float baseProjectileSpeed;
    private float baseProjectileDamage;

    private float currentDetectionRange;
    private float currentPreferredAttackRange;
    private float currentMinimumDistanceFromPlayer;
    private float currentRepositionSpeed;
    private float currentShootCooldown;
    private float currentAimWindupTime;
    private float currentPostShotRecoveryTime;
    private float currentProjectileSpeed;
    private float currentProjectileDamage;

    private void Awake()
    {
        batPathFinding = GetComponent<BatPathFinding>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        contactDamageSource = GetComponent<EnemyDamageSource>();
        state = BatAIState.Idle;

        CacheBaseValues();
        ResetCurrentValues();

        if (contactDamageSource != null)
        {
            contactDamageSource.enabled = false;
        }
    }

    private void Start()
    {
        TryFindPlayer();

        roamCenter = transform.position;
        roamAngle = Random.Range(0f, 360f);

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        originalScale = transform.localScale;
        StartCoroutine(AIBehaviour());
    }

    private IEnumerator AIBehaviour()
    {
        while (state != BatAIState.Dead)
        {
            if (playerTransform == null)
            {
                TryFindPlayer();
                HandleIdleRoaming();
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            float distance = Vector2.Distance(transform.position, playerTransform.position);

            if (distance > currentDetectionRange)
            {
                state = BatAIState.Idle;
                RestoreVisuals();
                HandleIdleRoaming();
                yield return new WaitForSeconds(ThinkInterval);
                continue;
            }

            if (ShouldReposition(distance))
            {
                state = BatAIState.Repositioning;
                RestoreVisuals();
                Reposition(distance);
                yield return new WaitForSeconds(ThinkInterval);
                continue;
            }

            if (Time.time >= nextShootTime)
            {
                yield return AimAndFire();
                continue;
            }

            state = BatAIState.Recovering;
            RestoreVisuals();
            batPathFinding?.StopMoving();
            yield return new WaitForSeconds(ThinkInterval);
        }
    }

    private void HandleIdleRoaming()
    {
        if (batPathFinding == null)
        {
            return;
        }

        roamAngle += roamSpeed * ThinkInterval * 50f;
        if (roamAngle > 360f)
        {
            roamAngle -= 360f;
        }

        float rad = roamAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * roamRadius;
        batPathFinding.SetMoveSpeed(currentRepositionSpeed);
        batPathFinding.MoveTo(roamCenter + offset);
    }

    private bool ShouldReposition(float distance)
    {
        return distance > currentPreferredAttackRange + 0.35f ||
               distance < currentMinimumDistanceFromPlayer;
    }

    private void Reposition(float distance)
    {
        if (batPathFinding == null || playerTransform == null)
        {
            return;
        }

        Vector2 batPosition = transform.position;
        Vector2 playerPosition = playerTransform.position;
        Vector2 fromPlayer = (batPosition - playerPosition).normalized;

        if (fromPlayer == Vector2.zero)
        {
            fromPlayer = Random.insideUnitCircle.normalized;
        }

        Vector2 targetPosition = distance < currentMinimumDistanceFromPlayer
            ? batPosition + fromPlayer * currentMinimumDistanceFromPlayer
            : playerPosition + fromPlayer * currentPreferredAttackRange;

        batPathFinding.SetMoveSpeed(currentRepositionSpeed);
        batPathFinding.MoveTo(targetPosition);
        FacePlayer();
    }

    private IEnumerator AimAndFire()
    {
        state = BatAIState.Aiming;
        batPathFinding?.StopMoving();

        float timer = currentAimWindupTime;
        while (timer > 0f && playerTransform != null)
        {
            FacePlayer();
            ShowAimingVisual();
            timer -= ThinkInterval;
            yield return new WaitForSeconds(ThinkInterval);
        }

        state = BatAIState.Firing;
        FireProjectile();
        RestoreVisuals();

        state = BatAIState.Recovering;
        nextShootTime = Time.time + currentShootCooldown + currentPostShotRecoveryTime;
        yield return new WaitForSeconds(currentPostShotRecoveryTime);
    }

    private void FireProjectile()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector2 aimOrigin = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = ((Vector2)playerTransform.position - aimOrigin).normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        Vector3 spawnPosition = firePoint != null
            ? firePoint.position
            : (Vector3)(aimOrigin + direction * projectileSpawnOffset);

        GameObject projectileObject = fireballProjectilePrefab != null
            ? Instantiate(fireballProjectilePrefab, spawnPosition, GetProjectileRotation(direction))
            : CreateFallbackProjectile(spawnPosition);

        DisablePlayerFireballBehaviour(projectileObject);
        BatFireballProjectile projectile = projectileObject.GetComponent<BatFireballProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<BatFireballProjectile>();
        }

        projectile.Initialize(gameObject, direction, currentProjectileSpeed, currentProjectileDamage);
    }

    private Quaternion GetProjectileRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }

    private GameObject CreateFallbackProjectile(Vector3 spawnPosition)
    {
        GameObject projectileObject = new GameObject("BatFireball");
        projectileObject.transform.position = spawnPosition;
        CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.25f;
        return projectileObject;
    }

    private void DisablePlayerFireballBehaviour(GameObject projectileObject)
    {
        if (projectileObject == null)
        {
            return;
        }

        FireballSpell playerFireball = projectileObject.GetComponent<FireballSpell>();
        if (playerFireball != null)
        {
            playerFireball.enabled = false;
        }
    }

    private void TryFindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector3 scale = originalScale;
        scale.x = Mathf.Abs(scale.x) * (playerTransform.position.x < transform.position.x ? -1f : 1f);
        transform.localScale = scale;
    }

    private void ShowAimingVisual()
    {
        if (spriteRenderer != null)
        {
            float flashValue = Mathf.PingPong(Time.time * prepareFlashSpeed, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, prepareColor, flashValue);
        }
    }

    private void RestoreVisuals()
    {
        if (spriteRenderer != null && spriteRenderer.color != originalColor)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void CacheBaseValues()
    {
        baseDetectionRange = detectionRange;
        basePreferredAttackRange = preferredAttackRange;
        baseMinimumDistanceFromPlayer = minimumDistanceFromPlayer;
        baseRepositionSpeed = repositionSpeed;
        baseShootCooldown = shootCooldown;
        baseAimWindupTime = aimWindupTime;
        basePostShotRecoveryTime = postShotRecoveryTime;
        baseProjectileSpeed = projectileSpeed;
        baseProjectileDamage = projectileDamage;
    }

    private void ResetCurrentValues()
    {
        currentDetectionRange = baseDetectionRange;
        currentPreferredAttackRange = basePreferredAttackRange;
        currentMinimumDistanceFromPlayer = baseMinimumDistanceFromPlayer;
        currentRepositionSpeed = baseRepositionSpeed;
        currentShootCooldown = baseShootCooldown;
        currentAimWindupTime = baseAimWindupTime;
        currentPostShotRecoveryTime = basePostShotRecoveryTime;
        currentProjectileSpeed = baseProjectileSpeed;
        currentProjectileDamage = baseProjectileDamage;
    }

    public void ApplyDdaProfile(DdaDifficultyProfile profile)
    {
        if (profile == null)
        {
            profile = DdaDifficultyProfile.Balanced();
        }

        currentDetectionRange = baseDetectionRange * DdaDifficultyProfile.ClampDetectionRange(profile.detectionRangeMultiplier);
        currentPreferredAttackRange = basePreferredAttackRange * DdaDifficultyProfile.ClampDetectionRange(profile.detectionRangeMultiplier);
        currentMinimumDistanceFromPlayer = baseMinimumDistanceFromPlayer;
        currentRepositionSpeed = baseRepositionSpeed * DdaDifficultyProfile.ClampChaseSpeed(profile.chaseSpeedMultiplier);
        currentShootCooldown = baseShootCooldown * DdaDifficultyProfile.ClampAttackCooldown(profile.attackCooldownMultiplier);
        currentAimWindupTime = baseAimWindupTime;
        currentPostShotRecoveryTime = basePostShotRecoveryTime * DdaDifficultyProfile.ClampRecoveryTime(profile.recoveryTimeMultiplier);
        currentProjectileSpeed = baseProjectileSpeed;
        currentProjectileDamage = baseProjectileDamage * DdaDifficultyProfile.ClampDamage(profile.damageMultiplier);

        Debug.Log(
            $"[DDA] BatAI profile={profile.profileName} " +
            $"detection={baseDetectionRange:0.##}->{currentDetectionRange:0.##} " +
            $"reposition={baseRepositionSpeed:0.##}->{currentRepositionSpeed:0.##} " +
            $"shootCooldown={baseShootCooldown:0.##}->{currentShootCooldown:0.##} " +
            $"recovery={basePostShotRecoveryTime:0.##}->{currentPostShotRecoveryTime:0.##} " +
            $"projectileDamage={baseProjectileDamage:0.##}->{currentProjectileDamage:0.##}",
            this);
    }

    private void OnDisable()
    {
        state = BatAIState.Dead;
        batPathFinding?.StopMoving();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? currentDetectionRange : detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? currentPreferredAttackRange : preferredAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? currentMinimumDistanceFromPlayer : minimumDistanceFromPlayer);
    }
}
