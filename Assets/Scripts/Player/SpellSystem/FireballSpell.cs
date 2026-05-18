using UnityEngine;

public class FireballSpell : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float damage = 10f;
    private bool initializedByWeapon;
    private PooledProjectile pooledProjectile;

    private void Awake()
    {
        pooledProjectile = GetComponent<PooledProjectile>();
    }

    public void Initialize(float projectileSpeed, int baseDamage, float projectileRange)
    {
        if (pooledProjectile == null)
        {
            pooledProjectile = GetComponent<PooledProjectile>();
        }

        speed = projectileSpeed;
        damage = baseDamage;
        initializedByWeapon = true;

        if (speed > 0f && projectileRange > 0f)
        {
            lifetime = projectileRange / speed;
        }

        CancelInvoke(nameof(Expire));
        Invoke(nameof(Expire), lifetime);
    }

    private void Start()
    {
        if (!initializedByWeapon)
        {
            Invoke(nameof(Expire), lifetime);
        }
    }


    private void Update()
    {
        Vector2 direction = initializedByWeapon ? Vector2.right : Vector2.left;
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage((int)damage);
            Release();
        }
    }

    private void Expire()
    {
        Release();
    }

    private void Release()
    {
        CancelInvoke(nameof(Expire));

        if (pooledProjectile != null)
        {
            pooledProjectile.Release();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
