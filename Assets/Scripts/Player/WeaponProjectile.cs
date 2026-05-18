using UnityEngine;

public class WeaponProjectile : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected float speed = 15f;
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float lifetime = 5f;
    private PooledProjectile pooledProjectile;

    private void Awake()
    {
        pooledProjectile = GetComponent<PooledProjectile>();
    }

    public virtual void Initialize(float projectileSpeed, int baseDamage, float projectileRange)
    {
        if (pooledProjectile == null)
        {
            pooledProjectile = GetComponent<PooledProjectile>();
        }

        speed = projectileSpeed;
        damage = baseDamage;

        if (speed > 0f && projectileRange > 0f)
        {
            lifetime = projectileRange / speed;
        }

        CancelInvoke(nameof(Expire));
        Invoke(nameof(Expire), lifetime);
    }

    protected virtual void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<WeaponProjectile>() != null)
        {
            return;
        }

        if (other.isTrigger)
        {
            return;
        }

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage((int)damage);
        }

        Release();
    }

    protected void Release()
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

    private void Expire()
    {
        Release();
    }
}
