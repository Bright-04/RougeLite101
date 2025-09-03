using UnityEngine;

public class EnemyDamageSource : MonoBehaviour
{
    [SerializeField] private float damageAmount = 3;
    [SerializeField] private float damageCooldown = 0.5f;
    private float damageTimer = 0;

    private void Update()
    {
        if (damageTimer > 0)
            damageTimer -= Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerStats playerStats) && damageTimer <= 0)
        {
            playerStats.TakeDamage(damageAmount);
            damageTimer = damageCooldown;
        }
    }

}
