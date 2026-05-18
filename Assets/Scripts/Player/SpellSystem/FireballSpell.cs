using UnityEngine;

public class FireballSpell : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float damage = 10f;
    private bool initializedByWeapon;

    public void Initialize(float projectileSpeed, int baseDamage, float projectileRange)
    {
        speed = projectileSpeed;
        damage = baseDamage;
        initializedByWeapon = true;

        if (speed > 0f && projectileRange > 0f)
        {
            lifetime = projectileRange / speed;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
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
            Destroy(gameObject);
        }
    }
}
