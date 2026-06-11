using UnityEngine;
using System.Collections;
using RougeLite.Combat.Damage;

public class SkeletonHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int startingHealth = 20;
    [SerializeField] private int goldReward = 10;
    [SerializeField] private float expReward = 5f;

    private int currentHealth;
    private bool dead;

    private void Start()
    {
        currentHealth = startingHealth;
    }

    public void TakeDamage(int damage)
    {
        if (dead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (dead) return;
        dead = true;

        EnemyDeathNotifier deathNotifier = GetComponent<EnemyDeathNotifier>();
        if (deathNotifier != null)
        {
            deathNotifier.NotifyDied();
        }
        else
        {
            DdaTelemetryService.Instance?.RecordEnemyKilled(gameObject.name);
        }

        PlayerMoney money = FindFirstObjectByType<PlayerMoney>();

        if (money != null)
            money.AddGold(goldReward);

        if (ExpManager.Instance != null)
            ExpManager.Instance.GainExperience(expReward);

        Destroy(gameObject);
    }
}
