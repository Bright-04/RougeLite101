using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BatFireballProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private LayerMask wallLayer;

    private float speed = 6f;
    private float damage = 8f;
    private GameObject owner;
    private Collider2D[] projectileColliders;
    private float lifeTimer;

    public void Initialize(GameObject projectileOwner, Vector2 fireDirection, float projectileSpeed, float projectileDamage)
    {
        Vector2 direction = fireDirection.sqrMagnitude > 0f ? fireDirection.normalized : Vector2.right;
        speed = Mathf.Max(0f, projectileSpeed);
        damage = Mathf.Max(0f, projectileDamage);
        owner = projectileOwner;
        lifeTimer = lifetime;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        IgnoreOwnerColliders();
    }

    private void Awake()
    {
        projectileColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D projectileCollider in projectileColliders)
        {
            projectileCollider.isTrigger = true;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (wallLayer == 0)
        {
            wallLayer = LayerMask.GetMask("Default", "Environment", "Obstacle");
        }

        lifeTimer = lifetime;
    }

    private void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsOwnerCollider(other))
        {
            return;
        }

        if (other.TryGetComponent(out PlayerStats playerStats))
        {
            playerStats.TakeDamage(damage);

            if (other.TryGetComponent(out Knockback playerKnockback))
            {
                playerKnockback.GetKnockedBack(transform, knockbackForce);
            }

            if (other.TryGetComponent(out Flash playerFlash))
            {
                playerFlash.StartCoroutine(playerFlash.FlashRoutine());
            }

            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger && IsWallLayer(other.gameObject.layer))
        {
            Destroy(gameObject);
        }
    }

    private void IgnoreOwnerColliders()
    {
        if (owner == null || projectileColliders == null)
        {
            return;
        }

        Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D projectileCollider in projectileColliders)
        {
            foreach (Collider2D ownerCollider in ownerColliders)
            {
                if (projectileCollider != null && ownerCollider != null)
                {
                    Physics2D.IgnoreCollision(projectileCollider, ownerCollider, true);
                }
            }
        }
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        return owner != null && other != null && other.transform.root == owner.transform.root;
    }

    private bool IsWallLayer(int layer)
    {
        return (wallLayer.value & (1 << layer)) != 0;
    }
}
