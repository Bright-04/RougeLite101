using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BnnyAI : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float timeBetweenSkills = 2f;
    [SerializeField] private string playerTag = "Player";

    private Transform player;
    private Vector3 spawnPosition;
    private bool isActing;

    [Header("Debug")]
    [Tooltip("-1 = random, 0 = Skill1, 1 = Skill2, 2 = Skill3")]
    [SerializeField] private int debugSkillOverride = -1;
    [SerializeField] private bool drawDetectionRadius = true;

    [Header("Skill 1 - Back Teleport Slash")]
    [SerializeField] private float behindDistance = 1.5f;
    [SerializeField] private float skill1WindupTime = 0.2f;
    [SerializeField] private float skill1SlashDuration = 0.3f;
    [SerializeField] private GameObject skill1SlashHitbox;

    [Header("Skill 2 - Circular Fireballs")]
    [SerializeField] private GameObject fireballProjectilePrefab;
    [SerializeField] private int fireballCount = 12;
    [SerializeField] private float fireballDelay = 0.05f;

    [Header("Skill 3 - Edge Slash (from Bnny_EdgeTop)")]
    [SerializeField] private GameObject bigSlashProjectilePrefab;
    [SerializeField] private float skill3ChargeTime = 1.5f;
    [SerializeField] private float skill3ReleaseDuration = 0.7f;

    private Transform edgeTop; // found by name "Bnny_EdgeTop"

    [Header("Skill Cooldowns")]
    [SerializeField] private float skill1Cooldown = 3f;
    [SerializeField] private float skill2Cooldown = 4f;
    [SerializeField] private float skill3Cooldown = 6f;

    private float lastSkill1Time = -999f;
    private float lastSkill2Time = -999f;
    private float lastSkill3Time = -999f;

    // Animator – use the one already on Bnny
    private Animator animator;

    // Names must match your Animator parameters
    private const string SKILL1_TRIGGER = "Skill1_Slash";
    private const string SKILL2_TRIGGER = "Skill2_Cast";
    private const string SKILL3_CHARGE_TRIGGER = "Skill3_Charge";
    private const string SKILL3_RELEASE_TRIGGER = "Skill3_Release";

    private void Awake()
    {
        spawnPosition = transform.position;
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Find player
        if (PlayerMovement.Instance != null)
        {
            player = PlayerMovement.Instance.transform;
        }
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        // Find Bnny_EdgeTop in the scene (for Skill 3)
        GameObject edgeObj = GameObject.Find("Bnny_EdgeTop");
        if (edgeObj != null)
        {
            edgeTop = edgeObj.transform;
            Debug.Log("[BNNY] Found Bnny_EdgeTop – Skill 3 enabled.");
        }
        else
        {
            Debug.LogWarning("[BNNY] Bnny_EdgeTop NOT found – Skill 3 will be skipped (unless forced by debug).");
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distToSpawn = Vector2.Distance(player.position, spawnPosition);

        if (!isActing && distToSpawn <= detectionRange)
        {
            Debug.Log("[BNNY] Player in range, starting behaviour loop.");
            StartCoroutine(BehaviourLoop());
        }
    }

    private IEnumerator BehaviourLoop()
    {
        isActing = true;

        while (true)
        {
            if (player == null)
            {
                Debug.Log("[BNNY] Player null, stopping loop.");
                isActing = false;
                yield break;
            }

            float distToSpawn = Vector2.Distance(player.position, spawnPosition);
            if (distToSpawn > detectionRange)
            {
                Debug.Log("[BNNY] Player left range, returning to spawn.");
                transform.position = spawnPosition;
                transform.rotation = Quaternion.identity;
                isActing = false;
                yield break;
            }

            int skill = GetNextSkillIndex();
            Debug.Log($"[BNNY] Using SKILL {skill}");

            switch (skill)
            {
                case 0:
                    yield return StartCoroutine(Skill1_TeleportBehindSlash());
                    lastSkill1Time = Time.time;
                    break;
                case 1:
                    yield return StartCoroutine(Skill2_CircleFireballs());
                    lastSkill2Time = Time.time;
                    break;
                case 2:
                    yield return StartCoroutine(Skill3_EdgeSlash());
                    lastSkill3Time = Time.time;
                    break;
            }

            // global delay between skills
            yield return new WaitForSeconds(timeBetweenSkills);
        }
    }

    private int GetNextSkillIndex()
    {
        // Force one skill manually (ignores cooldown)
        if (debugSkillOverride >= 0 && debugSkillOverride <= 2)
            return debugSkillOverride;

        float now = Time.time;
        bool canUseSkill3 = (edgeTop != null && bigSlashProjectilePrefab != null);

        var candidates = new List<int>();

        if (now >= lastSkill1Time + skill1Cooldown) candidates.Add(0);
        if (now >= lastSkill2Time + skill2Cooldown) candidates.Add(1);
        if (canUseSkill3 && now >= lastSkill3Time + skill3Cooldown) candidates.Add(2);

        // if everything is on cooldown, just allow all valid skills
        if (candidates.Count == 0)
        {
            candidates.Add(0);
            candidates.Add(1);
            if (canUseSkill3) candidates.Add(2);
        }

        int idx = Random.Range(0, candidates.Count);
        return candidates[idx];
    }

    #region Skill 1 – Teleport Behind + Slash

    private IEnumerator Skill1_TeleportBehindSlash()
    {
        Debug.Log("[BNNY] Skill 1 START");

        TrySetTrigger(SKILL1_TRIGGER);

        yield return new WaitForSeconds(skill1WindupTime);

        if (player == null)
        {
            Debug.Log("[BNNY] Skill 1 abort: player null");
            yield break;
        }

        // Behind relative to spawn->player
        Vector2 dir = (player.position - spawnPosition).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;

        Vector3 behindPos = player.position - (Vector3)(dir * behindDistance);
        Debug.Log("[BNNY] Skill 1 teleport to " + behindPos);
        transform.position = behindPos;

        // Face player (this part was already working for you)
        Vector2 toPlayer = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Spawn slash hitbox
        if (skill1SlashHitbox != null)
        {
            Debug.Log("[BNNY] Skill 1 spawn slash hitbox");
            Instantiate(
                skill1SlashHitbox,
                transform.position,
                Quaternion.Euler(0f, 0f, angle)
            );
        }
        else
        {
            Debug.LogError("[BNNY] Skill1SlashHitbox NOT ASSIGNED in Inspector – Skill 1 has no hitbox.");
        }

        yield return new WaitForSeconds(skill1SlashDuration);

        Debug.Log("[BNNY] Skill 1 return to spawn");
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
    }


    #endregion

    #region Skill 2 – Circular Fireballs

    private IEnumerator Skill2_CircleFireballs()
    {
        Debug.Log("[BNNY] Skill 2 START");

        if (fireballProjectilePrefab == null)
        {
            Debug.LogWarning("[BNNY] Fireball prefab is NULL – Skill 2 does nothing.");
            yield break;
        }

        TrySetTrigger(SKILL2_TRIGGER);

        Vector3 origin = spawnPosition;
        transform.position = origin;
        transform.rotation = Quaternion.identity;

        for (int i = 0; i < fireballCount; i++)
        {
            float angle = (360f / fireballCount) * i;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

            Debug.Log($"[BNNY] Skill 2 spawn fireball {i} at angle {angle}");
            Instantiate(fireballProjectilePrefab, origin, rot);

            if (fireballDelay > 0f)
                yield return new WaitForSeconds(fireballDelay);
        }

        Debug.Log("[BNNY] Skill 2 END");
    }

    #endregion

    #region Skill 3 – Edge Charge + Slash (top only, NO rotation)

    private IEnumerator Skill3_EdgeSlash()
    {
        Debug.Log("[BNNY] Skill 3 START");

        if (bigSlashProjectilePrefab == null)
        {
            Debug.LogWarning("[BNNY] Big slash prefab is NULL – Skill 3 does nothing.");
            yield break;
        }

        if (edgeTop == null)
        {
            Debug.LogWarning("[BNNY] Bnny_EdgeTop not found – Skill 3 abort.");
            yield break;
        }

        // 1) Teleport to top
        Debug.Log("[BNNY] Skill 3 teleport to " + edgeTop.position);
        transform.position = edgeTop.position;

        // NO rotation changes – use natural sprite orientation
        transform.rotation = Quaternion.identity;

        // 2) Play CHARGE animation
        TrySetTrigger(SKILL3_CHARGE_TRIGGER);

        // Wait for charge time
        float t = skill3ChargeTime;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        // 3) Play RELEASE animation
        TrySetTrigger(SKILL3_RELEASE_TRIGGER);

        // Spawn big slash (BnnyBigSlashProjectile moves it straight down)
        Debug.Log("[BNNY] Skill 3 spawn big slash");
        Instantiate(
            bigSlashProjectilePrefab,
            transform.position,
            Quaternion.identity
        );

        // ⏱ 4) Let RELEASE animation play fully
        // This should match your release clip length in seconds
        yield return new WaitForSeconds(skill3ReleaseDuration);

        // 5) Freeze on LAST FRAME of release
        float originalSpeed = 1f;
        if (animator != null)
        {
            originalSpeed = animator.speed;
            animator.speed = 0f; // freeze whatever frame we're currently on (end of release)
        }

        // Stay frozen in that last pose for ~2 seconds
        yield return new WaitForSeconds(0.2f);

        // 6) Unfreeze and return to origin
        if (animator != null)
            animator.speed = originalSpeed;

        Debug.Log("[BNNY] Skill 3 return to spawn");
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
    }



    #endregion

    private void TrySetTrigger(string triggerName)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(triggerName)) return;

        bool found = false;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[BNNY] Animator trigger '{triggerName}' not found. (Check Animator parameters.)");
            return;
        }

        animator.SetTrigger(triggerName);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDetectionRadius) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            Application.isPlaying ? spawnPosition : transform.position,
            detectionRange
        );
    }
}
