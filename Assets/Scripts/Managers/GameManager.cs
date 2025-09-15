using UnityEngine;
using RougeLite.Events;

/// <summary>
/// Simple game manager that responds to player and enemy events
/// Demonstrates centralized game state management using events
/// </summary>
public class GameManager : EventBehaviour,
    IEventListener<PlayerDeathEvent>,
    IEventListener<EnemyDeathEvent>,
    IEventListener<PlayerDamagedEvent>
{
    [Header("Game State")]
    [SerializeField] private int enemiesKilled = 0;
    [SerializeField] private float damageDealt = 0f;
    [SerializeField] private bool gameOver = false;
    
    [Header("Game Settings")]
    [SerializeField] private int enemiesForVictory = 10;
    [SerializeField] private bool respawnPlayer = true;
    [SerializeField] private float respawnDelay = 3f;

    protected override void Awake()
    {
        base.Awake();
        
        // Subscribe to game events
        SubscribeToEvent<PlayerDeathEvent>(this);
        SubscribeToEvent<EnemyDeathEvent>(this);
        SubscribeToEvent<PlayerDamagedEvent>(this);
    }

    protected override void OnDestroy()
    {
        // Unsubscribe from events
        UnsubscribeFromEvent<PlayerDeathEvent>(this);
        UnsubscribeFromEvent<EnemyDeathEvent>(this);
        UnsubscribeFromEvent<PlayerDamagedEvent>(this);
        
        base.OnDestroy();
    }

    public void OnEventReceived(PlayerDeathEvent eventData)
    {
        if (gameOver) return;
        
        Debug.Log("Game Manager: Player has died!");
        gameOver = true;
        
        // Broadcast game over event
        var gameOverEvent = new GameOverEvent("Player Death", gameObject);
        BroadcastEvent(gameOverEvent);
        
        if (respawnPlayer)
        {
            Invoke(nameof(RespawnPlayer), respawnDelay);
        }
        
        // Show game stats
        ShowGameStats();
    }

    public void OnEventReceived(EnemyDeathEvent eventData)
    {
        enemiesKilled++;
        Debug.Log($"Game Manager: Enemy defeated! Total kills: {enemiesKilled}");
        
        // Check for victory condition
        if (enemiesKilled >= enemiesForVictory)
        {
            Victory();
        }
    }

    public void OnEventReceived(PlayerDamagedEvent eventData)
    {
        damageDealt += eventData.Data.damage;
    }

    private void Victory()
    {
        if (gameOver) return;
        
        Debug.Log("Game Manager: Victory! All enemies defeated!");
        gameOver = true;
        
        // Broadcast level complete event
        var levelData = new LevelData
        {
            levelNumber = 1,
            enemiesKilled = enemiesKilled,
            totalDamageDealt = damageDealt,
            timeCompleted = Time.time
        };
        
        // Calculate score based on performance
        int score = enemiesKilled * 100 + Mathf.FloorToInt(damageDealt);
        var levelCompleteEvent = new LevelCompleteEvent(levelData, Time.time, score, gameObject);
        BroadcastEvent(levelCompleteEvent);
        
        ShowGameStats();
    }

    private void RespawnPlayer()
    {
        Debug.Log("Game Manager: Respawning player...");
        gameOver = false;
        
        // Find and heal player
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.Heal(playerStats.maxHP); // Full heal
            
            // Broadcast respawn event
            Vector3 respawnPosition = Vector3.zero; // You can set a specific respawn point
            var respawnEvent = new PlayerRespawnEvent(respawnPosition, gameObject);
            BroadcastEvent(respawnEvent);
        }
    }

    private void ShowGameStats()
    {
        Debug.Log("=== GAME STATS ===");
        Debug.Log($"Enemies Killed: {enemiesKilled}");
        Debug.Log($"Total Damage Received: {damageDealt:F1}");
        Debug.Log($"Time Played: {Time.time:F1} seconds");
        Debug.Log("=================");
    }

    /// <summary>
    /// Public method to restart the game
    /// </summary>
    public void RestartGame()
    {
        enemiesKilled = 0;
        damageDealt = 0f;
        gameOver = false;
        
        // Broadcast game start event
        var gameStartEvent = new GameStartEvent(gameObject);
        BroadcastEvent(gameStartEvent);
        
        Debug.Log("Game Manager: Game restarted!");
    }

    /// <summary>
    /// Get current game statistics
    /// </summary>
    public (int kills, float damage, float time) GetGameStats()
    {
        return (enemiesKilled, damageDealt, Time.time);
    }
}