using UnityEngine;

public class BnnySkill1SlashHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 2;
    [SerializeField] private float lifetime = 0.25f;
    [SerializeField] private string targetTag = "Player";

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only hurt player, never the boss or other enemies
        if (!other.CompareTag(targetTag)) return;

        IDamageable dmg = other.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            // no need to destroy, lifetime will auto clean up
        }
    }
}
