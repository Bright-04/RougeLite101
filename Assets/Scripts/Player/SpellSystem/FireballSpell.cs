using UnityEngine;

public class FireballSpell : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float damage = 10f;

    private void Start()
    {
        Debug.Log("Fireball instantiated at " + transform.position);
        Destroy(gameObject, lifetime);
    }


    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out SlimeHealth slimeHealth))
        {
            slimeHealth.TakeDamage((int)damage);
            Destroy(gameObject);
        }
    }
}
