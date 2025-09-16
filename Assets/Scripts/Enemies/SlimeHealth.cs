using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RougeLite.Events;

public class SlimeHealth : EventBehaviour
{
    [SerializeField] private int startingHealth = 3;

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;
    
    protected override void Awake()
    {   
        base.Awake();
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
    }
    private void Start()
    {
        currentHealth = startingHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        Debug.Log($"Slime took {damage} damage. Health: {currentHealth}/{startingHealth}");
        
        if (knockback != null && PlayerController.Instance != null)
        {
            knockback.GetKnockedBack(PlayerController.Instance.transform, 15f);
        }
        
        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }
    }

    public void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            // Broadcast enemy death event
            var enemyData = new EnemyData(
                enemy: gameObject,
                type: "Slime",
                health: 0f,
                maxHealth: startingHealth,
                position: transform.position
            );
            
            var deathEvent = new EnemyDeathEvent(enemyData, gameObject);
            BroadcastEvent(deathEvent);
            
            Debug.Log("Slime defeated!");
            Destroy(gameObject);
        }
    }
}
