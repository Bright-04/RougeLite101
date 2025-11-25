using UnityEngine;

public class SlimeKingProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float damage = 10f;

    private bool launched;
    private Vector2 direction;

    private void Awake()
    {
        // Failsafe: no matter what, this projectile will be destroyed after `lifetime` seconds
        Destroy(gameObject, lifetime);
    }

    public void Prepare()
    {
        launched = false;
        direction = Vector2.zero;
        // (No need to call Destroy here since Awake already scheduled it)
    }

    public void LaunchTowards(Vector3 targetPos)
    {
        direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;
        launched = true;

        // DO NOT call Destroy here anymore – it might never be called if boss dies early.
        // Lifetime is already handled by Awake.
    }

    private void Update()
    {
        if (launched)
        {
            transform.position += (Vector3)direction * speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerStats playerStats))
        {
            playerStats.TakeDamage(damage);

            // if (other.TryGetComponent(out Knockback playerKnockback))
            // {
            //     playerKnockback.GetKnockedBack(transform, knockbackForce);
            // }

            if (other.TryGetComponent(out Flash flash))
            {
                StartCoroutine(flash.FlashRoutine());
            }

            Destroy(gameObject);
        }

        // Optional: despawn when hitting walls:
        // if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        // {
        //     Destroy(gameObject);
        // }
    }
}
