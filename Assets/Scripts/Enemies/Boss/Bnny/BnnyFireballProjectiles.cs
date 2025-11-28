//using UnityEngine;

//public class BnnyFireballProjectile : MonoBehaviour
//{
//    [SerializeField] private float speed = 8f;
//    [SerializeField] private int damage = 1;
//    [SerializeField] private float lifetime = 5f;
//    [SerializeField] private LayerMask hitLayers;
//    [SerializeField] private bool destroyOnHit = true;

//    private void Start()
//    {
//        Destroy(gameObject, lifetime);
//    }

//    private void Update()
//    {
//        // Move along local right (so rotation decides the direction)
//        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
//    }

//    private void OnTriggerEnter2D(Collider2D other)
//    {
//        // Only hit specified layers
//        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0)
//            return;

//        IDamageable target = other.GetComponent<IDamageable>();
//        if (target != null)
//        {
//            target.TakeDamage(damage);
//        }

//        if (destroyOnHit)
//        {
//            Destroy(gameObject);
//        }
//    }
//}
using UnityEngine;

public class BnnyFireballProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 4f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
    }
}
