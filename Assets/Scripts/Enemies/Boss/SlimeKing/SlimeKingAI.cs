using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SlimeKingPathFinding))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SlimeKingAI : MonoBehaviour
{
    [SerializeField] private string bossLayerName = "Enemy";
    [SerializeField] private string playerLayerName = "Player";

    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float behaviourCheckInterval = 0.2f;

    [Header("Jump Skill (telegraphed)")]
    [SerializeField] private float jumpChargeDuration = 0.6f;   // length of charge animation
    [SerializeField] private float shadowFollowDuration = 1.0f; // time shadow tracks player
    [SerializeField] private float shadowStayDuration = 0.5f;   // time shadow stays still before landing
    [SerializeField] private float jumpImpactRadius = 1.2f;
    [SerializeField] private float jumpDamage = 30f;
    [SerializeField] private GameObject jumpShadowPrefab;       // prefab with SlimeKingJumpShadow
    [SerializeField] private float jumpCooldown = 5f;
    private float jumpCooldownTimer = 0f;

    [Header("Shake + Projectile Skill")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int projectileCount = 8;
    [SerializeField] private float projectileRadius = 2.2f;
    [SerializeField] private float delayBeforeLaunch = 1.5f;
    [SerializeField] private float shakeDuration = 0.6f;
    [SerializeField] private float shakeCooldown = 5f;
    private float shakeCooldownTimer = 0f;

    [Header("General")]
    [SerializeField] private float timeBetweenAttacks = 3f;
    [SerializeField] private LayerMask playerLayer;

    private SlimeKingPathFinding path;
    private Transform playerTransform;
    private Rigidbody2D rb;
    private Animator animator;

    private bool isAttacking;
    private float attackTimer;

    // NEW: boss health for invulnerability control
    private SlimeKingHealth slimeKingHealth;

    // specific colliders for body vs body collision ignore
    private Collider2D bossCollider;
    private Collider2D playerCollider;

    private void Awake()
    {
        path = GetComponent<SlimeKingPathFinding>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        bossCollider = GetComponent<Collider2D>();   // main boss collider
        slimeKingHealth = GetComponent<SlimeKingHealth>();

        if (rb != null) rb.freezeRotation = true;

        if (path == null)
            Debug.LogError("[SlimeKingAI] Missing SlimeKingPathFinding!");

        if (slimeKingHealth == null)
            Debug.LogError("[SlimeKingAI] Missing SlimeKingHealth!");
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            path?.SetTarget(playerTransform);

            // try to get player body collider (not weapon)
            playerCollider = playerObj.GetComponent<Collider2D>();
            if (playerCollider == null)
                Debug.LogWarning("[SlimeKingAI] Player has no Collider2D on root object. Assign manually or add one.");
        }
        else
        {
            Debug.LogWarning("[SlimeKingAI] No Player with tag 'Player' in scene.");
        }

        attackTimer = timeBetweenAttacks;
        StartCoroutine(BehaviourLoop());
    }

    private IEnumerator BehaviourLoop()
    {
        while (true)
        {
            // ---------- Cooldowns tick down ----------
            if (jumpCooldownTimer > 0f) jumpCooldownTimer -= behaviourCheckInterval;
            if (shakeCooldownTimer > 0f) shakeCooldownTimer -= behaviourCheckInterval;

            // Try to recover player reference if lost
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                    path?.SetTarget(playerTransform);
                    playerCollider = playerObj.GetComponent<Collider2D>();
                }

                yield return new WaitForSeconds(behaviourCheckInterval);
                continue;
            }

            float distance = Vector2.Distance(transform.position, playerTransform.position);

            // -------- MOVEMENT (NORMAL) --------
            if (!isAttacking && path != null)
            {
                if (distance <= detectionRange)
                {
                    path.SetCanMove(true);
                    SetMoving(true);
                }
                else
                {
                    path.SetCanMove(false);
                    SetMoving(false);
                }
            }

            // -------- ATTACK DECISION --------
            attackTimer -= behaviourCheckInterval;

            if (!isAttacking && attackTimer <= 0f && distance <= attackRange)
            {
                attackTimer = timeBetweenAttacks;
                StartCoroutine(AttackRoutine());
            }

            yield return new WaitForSeconds(behaviourCheckInterval);
        }
    }

    private void SetMoving(bool moving)
    {
        if (animator != null)
            animator.SetBool("IsMoving", moving);
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // stop movement while using skill
        path?.SetCanMove(false);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        // -------- Cooldown-aware skill choice --------
        bool canJump = jumpCooldownTimer <= 0f;
        bool canShake = shakeCooldownTimer <= 0f;

        if (canJump && canShake)
        {
            // both ready → random
            if (Random.value < 0.5f)
                yield return StartCoroutine(JumpSkillTelegraphed());
            else
                yield return StartCoroutine(ShakeProjectileSkill());
        }
        else if (canJump)
        {
            yield return StartCoroutine(JumpSkillTelegraphed());
        }
        else if (canShake)
        {
            yield return StartCoroutine(ShakeProjectileSkill());
        }
        else
        {
            // no skill ready → do nothing special, small delay
            yield return new WaitForSeconds(0.5f);
        }

        isAttacking = false;
    }

    // helper: ignore only body vs body collisions
    private void DisableBossPlayerCollision()
    {
        if (bossCollider != null && playerCollider != null)
            Physics2D.IgnoreCollision(bossCollider, playerCollider, true);
    }

    private void EnableBossPlayerCollision()
    {
        if (bossCollider != null && playerCollider != null)
            Physics2D.IgnoreCollision(bossCollider, playerCollider, false);
    }

    // =========================
    //  JUMP SKILL (4 PHASES)
    // =========================
    private IEnumerator JumpSkillTelegraphed()
    {
        if (playerTransform == null) yield break;

        // Make boss invulnerable during the whole jump + landing phase
        if (slimeKingHealth != null)
            slimeKingHealth.SetInvulnerable(true);

        // disable body collision for whole jump (weapons still work, but damage is ignored)
        DisableBossPlayerCollision();

        // --- Phase 1: Charge animation while boss stands still ---
        path?.SetCanMove(false);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        if (animator != null)
            animator.SetTrigger("Jump"); // plays SlimeKing_Jump

        yield return new WaitForSeconds(jumpChargeDuration);

        // --- Phase 2: Shadow follows player for shadowFollowDuration ---
        GameObject shadowObj = null;
        SlimeKingJumpShadow shadow = null;

        if (jumpShadowPrefab != null)
        {
            shadowObj = Instantiate(jumpShadowPrefab, playerTransform.position, Quaternion.identity);
            shadow = shadowObj.GetComponent<SlimeKingJumpShadow>();
            if (shadow != null)
                shadow.Init(playerTransform);
        }

        float elapsed = 0f;
        while (elapsed < shadowFollowDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- Phase 3: Lock shadow & give player escape window (shadowStayDuration) ---
        Vector3 landPos = playerTransform.position;

        if (shadow != null)
        {
            shadow.LockPosition();
            landPos = shadow.transform.position;
        }

        // shadow stays in place, player can run away
        yield return new WaitForSeconds(shadowStayDuration);

        // --- Phase 4: Boss lands + Land animation + AoE all at once ---
        transform.position = landPos;          // 1) teleport to impact point

        if (animator != null)
            animator.SetTrigger("Land");       // 2) play land anim same frame

        // 3) deal damage on same frame as landing
        Collider2D[] hits = Physics2D.OverlapCircleAll(landPos, jumpImpactRadius, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out PlayerStats playerStats))
            {
                playerStats.TakeDamage(jumpDamage);

                // OPTIONAL: comment out if you don't want knockback on landing
                if (hit.TryGetComponent(out Knockback knock))
                    knock.GetKnockedBack(transform, 15f);
            }
        }

        if (shadowObj != null)
            Destroy(shadowObj);

        // re-enable collision after landing
        EnableBossPlayerCollision();

        // put Jump on cooldown
        jumpCooldownTimer = jumpCooldown;

        // small recover delay after landing while still invulnerable
        yield return new WaitForSeconds(0.3f);

        // Boss becomes hittable again
        if (slimeKingHealth != null)
            slimeKingHealth.SetInvulnerable(false);
    }

    // =========================
    //  SHAKE + PROJECTILE SKILL
    // =========================
    private IEnumerator ShakeProjectileSkill()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        if (animator != null)
            animator.SetTrigger("Shake");

        // small jitter shake
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float ox = Random.Range(-0.05f, 0.05f);
            float oy = Random.Range(-0.05f, 0.05f);
            transform.position = originalPos + new Vector3(ox, oy, 0f);
            yield return null;
        }
        transform.position = originalPos;

        // spawn projectiles in a ring
        var spawned = new List<SlimeKingProjectile>();
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = i * (360f / projectileCount);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * projectileRadius;
            Vector3 spawnPos = transform.position + offset;

            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            if (projObj.TryGetComponent(out SlimeKingProjectile proj))
            {
                proj.Prepare();
                spawned.Add(proj);
            }
        }

        // wait, then launch toward player
        yield return new WaitForSeconds(delayBeforeLaunch);

        if (playerTransform != null)
        {
            Vector3 targetPos = playerTransform.position;
            foreach (var proj in spawned)
            {
                if (proj != null)
                    proj.LaunchTowards(targetPos);
            }
        }

        // put Shake on cooldown
        shakeCooldownTimer = shakeCooldown;

        // small recover delay
        yield return new WaitForSeconds(0.3f);
    }

    // Draw jump impact radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, jumpImpactRadius);
    }
}
