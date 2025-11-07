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
        transform.Translate(Vector2.left * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to get any component that implements IEnemy interface
        IEnemy enemy = other.gameObject.GetComponent<IEnemy>();
        
        if (enemy != null)
        {
            enemy.TakeDamage((int)damage);
            Destroy(gameObject);
        }
    }
}
