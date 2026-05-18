using UnityEngine;

public class WeaponProjectile : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected float speed = 15f;
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float lifetime = 5f;

    public virtual void Initialize(float projectileSpeed, int baseDamage, float projectileRange)
    {
        speed = projectileSpeed;
        damage = baseDamage;

        if (speed > 0f && projectileRange > 0f)
        {
            lifetime = projectileRange / speed;
        }
    }

    protected virtual void Start()
    {
        Destroy(gameObject, lifetime);
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

        Destroy(gameObject);
    }
}
