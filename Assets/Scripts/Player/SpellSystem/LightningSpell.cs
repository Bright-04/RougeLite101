using UnityEngine;

public class LightningSpell : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.5f;
    [SerializeField] private float damage = 30f;
    [SerializeField] private float radius = 1f;

    private void Start()
    {
        // Immediately damage anything in the area
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            // Try to get any component that implements IEnemy interface
            IEnemy enemy = hit.gameObject.GetComponent<IEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)damage);
            }
        }

        Destroy(gameObject, lifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
