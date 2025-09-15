using System.Collections;
using UnityEngine;
using RougeLite.Events;

public class SlimeAI : EventBehaviour
{
    private enum State
    {
        Roaming,
        Chasing
    }

    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float updateFrequency = 0.2f;
    [SerializeField] private float roamingWaitTime = 2f;

    private State currentState;
    private SlimePathFinding slimePathFinding;
    private Transform playerTransform;
    
    // Performance optimization - cache squared detection range
    private float detectionRangeSqr;
    
    // Cache for reducing garbage collection
    private WaitForSeconds updateWait;
    private WaitForSeconds roamingWait;

    protected override void Awake()
    {
        // Call base class Awake to initialize event system
        base.Awake();
        
        // Validate and cache components
        slimePathFinding = GetComponent<SlimePathFinding>();
        if (slimePathFinding == null)
        {
            Debug.LogError($"SlimeAI: SlimePathFinding component missing on {gameObject.name}!", this);
            enabled = false;
            return;
        }

        // Initialize state and cached values
        currentState = State.Roaming;
        detectionRangeSqr = detectionRange * detectionRange;
        
        // Cache WaitForSeconds to reduce garbage collection
        updateWait = new WaitForSeconds(updateFrequency);
        roamingWait = new WaitForSeconds(roamingWaitTime);
    }

    private void Start()
    {
        FindPlayer();
        StartCoroutine(AIBehaviourCoroutine());
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"SlimeAI: No GameObject tagged 'Player' found. {gameObject.name} will only roam.", this);
        }
    }

    private IEnumerator AIBehaviourCoroutine()
    {
        while (true)
        {
            if (playerTransform != null)
            {
                UpdateAIState();
                ExecuteCurrentState();
            }
            else
            {
                // If player is still null, try to find it again
                FindPlayer();
                ExecuteRoamingBehavior();
            }

            yield return updateWait;
        }
    }

    private void UpdateAIState()
    {
        // Use squared distance for better performance
        float distanceSqr = (transform.position - playerTransform.position).sqrMagnitude;
        
        if (distanceSqr <= detectionRangeSqr)
        {
            if (currentState != State.Chasing)
            {
                currentState = State.Chasing;
            }
        }
        else
        {
            if (currentState != State.Roaming)
            {
                currentState = State.Roaming;
            }
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case State.Chasing:
                ExecuteChasingBehavior();
                break;
            case State.Roaming:
                ExecuteRoamingBehavior();
                break;
        }
    }

    private void ExecuteChasingBehavior()
    {
        if (playerTransform != null && slimePathFinding != null)
        {
            slimePathFinding.MoveTo(playerTransform.position);
        }
    }

    private void ExecuteRoamingBehavior()
    {
        if (slimePathFinding != null)
        {
            Vector2 roamPosition = GetRoamingPosition();
            slimePathFinding.MoveTo(roamPosition);
        }
    }

    private Vector2 GetRoamingPosition()
    {
        // Generate a random direction for roaming
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        return randomDirection.normalized;
    }

    /// <summary>
    /// Call this method when the slime dies to broadcast the death event
    /// </summary>
    public void Die()
    {
        // Broadcast enemy death event
        var enemyData = new EnemyData(
            enemy: gameObject,
            type: "Slime",
            health: 0f, // Dead
            maxHealth: 100f, // You can make this configurable
            position: transform.position
        );
        
        var deathEvent = new EnemyDeathEvent(enemyData, gameObject);
        
        BroadcastEvent(deathEvent);
        
        // Destroy the game object
        Destroy(gameObject);
    }

    protected override void OnDestroy()
    {
        // Clean up any running coroutines
        StopAllCoroutines();
        
        // Call base class OnDestroy for event system cleanup
        base.OnDestroy();
    }

    // Debug visualization in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        if (currentState == State.Chasing)
        {
            Gizmos.color = Color.red;
            if (playerTransform != null)
            {
                Gizmos.DrawLine(transform.position, playerTransform.position);
            }
        }
    }
}
