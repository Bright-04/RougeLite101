using UnityEngine;

public class LightningSpell : MonoBehaviour
{
    [SerializeField] private float damage = 20f;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float duration = 0.5f;

    private void Start()
    {
        // Find enemies in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out SlimeHealth slimeHealth))
            {
                slimeHealth.TakeDamage((int)damage);
            }
        }

        Destroy(gameObject, duration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
