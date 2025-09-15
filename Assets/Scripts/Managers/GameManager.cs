using System.Collections;
using UnityEngine;
using RougeLite.Events;

namespace RougeLite.Managers
{
    /// <summary>
    /// Central game manager that controls overall game state, flow, and coordination between other managers
    /// Singleton pattern ensures only one instance exists throughout the game
    /// </summary>
    public class GameManager : EventBehaviour,
        IEventListener<PlayerDeathEvent>,
        IEventListener<EnemyDeathEvent>,
        IEventListener<PlayerDamagedEvent>
    {
        #region Singleton
        
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Game State

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private bool isPaused = false;
        [SerializeField] private float gameTime = 0f;
        [SerializeField] private int enemiesKilled = 0;
        [SerializeField] private float damageDealt = 0f;
        [SerializeField] private bool gameOver = false;

        [Header("Game Settings")]
        [SerializeField] private int enemiesForVictory = 10;
        [SerializeField] private bool respawnPlayer = true;
        [SerializeField] private float respawnDelay = 3f;

        public enum GameState
        {
            MainMenu,
            Loading,
            Gameplay,
            Paused,
            GameOver,
            Victory,
            Settings
        }

        #endregion

        #region Events

        public System.Action<GameState, GameState> OnGameStateChanged;
        public System.Action<bool> OnPauseStateChanged;
        public System.Action OnGameStarted;
        public System.Action OnGameEnded;
        public System.Action OnPlayerDied;
        public System.Action OnLevelCompleted;

        #endregion

        #region Properties

        public GameState CurrentState => currentState;
        public bool IsPaused => isPaused;
        public float GameTime => gameTime;
        public bool IsGameplayActive => currentState == GameState.Gameplay && !isPaused;
        public int EnemiesKilled => enemiesKilled;
        public float DamageDealt => damageDealt;
        public bool GameOver => gameOver;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                base.Awake();
                InitializeManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Subscribe to game events
            SubscribeToEvent<PlayerDeathEvent>(this);
            SubscribeToEvent<EnemyDeathEvent>(this);
            SubscribeToEvent<PlayerDamagedEvent>(this);
        }

        private void Start()
        {
            // Additional initialization after all managers are loaded
            StartCoroutine(InitializeOtherManagers());
        }

        private void Update()
        {
            // Update game time during gameplay
            if (IsGameplayActive)
            {
                gameTime += Time.deltaTime;
            }

            // Handle input
            HandleInput();
        }

        protected override void OnDestroy()
        {
            // Unsubscribe from events
            UnsubscribeFromEvent<PlayerDeathEvent>(this);
            UnsubscribeFromEvent<EnemyDeathEvent>(this);
            UnsubscribeFromEvent<PlayerDamagedEvent>(this);
            
            base.OnDestroy();
        }

        #endregion

        #region Initialization

        private void InitializeManager()
        {
            Debug.Log("GameManager: Initializing...");
            
            // Set initial state
            ChangeGameState(GameState.Gameplay); // Start in gameplay for now
        }

        private IEnumerator InitializeOtherManagers()
        {
            // Wait for other managers to be ready
            yield return new WaitForEndOfFrame();
            
            Debug.Log("GameManager: All managers initialized successfully");
        }

        #endregion

        #region Game State Management

        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            Debug.Log($"GameManager: State changed from {previousState} to {newState}");

            // Handle state transitions
            OnExitState(previousState);
            OnEnterState(newState);

            // Notify listeners
            OnGameStateChanged?.Invoke(previousState, newState);
            
            // Broadcast event
            BroadcastEvent(new GameStateChangedEvent(previousState, newState));
        }

        private void OnEnterState(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                    
                case GameState.Loading:
                    // Loading state handling
                    break;
                    
                case GameState.Gameplay:
                    Time.timeScale = 1f;
                    isPaused = false;
                    OnGameStarted?.Invoke();
                    break;
                    
                case GameState.Paused:
                    Time.timeScale = 0f;
                    isPaused = true;
                    break;
                    
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    OnGameEnded?.Invoke();
                    break;
                    
                case GameState.Victory:
                    Time.timeScale = 0f;
                    OnLevelCompleted?.Invoke();
                    break;
                    
                case GameState.Settings:
                    // Settings state handling
                    break;
            }
        }

        private void OnExitState(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                    Time.timeScale = 1f;
                    isPaused = false;
                    break;
            }
        }

        #endregion

        #region Pause Management

        public void PauseGame()
        {
            if (currentState == GameState.Gameplay)
            {
                ChangeGameState(GameState.Paused);
                OnPauseStateChanged?.Invoke(true);
                BroadcastEvent(new GamePausedEvent(true));
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeGameState(GameState.Gameplay);
                OnPauseStateChanged?.Invoke(false);
                BroadcastEvent(new GamePausedEvent(false));
            }
        }

        public void TogglePause()
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        #endregion

        #region Game Flow

        public void StartNewGame()
        {
            Debug.Log("GameManager: Starting new game");
            gameTime = 0f;
            enemiesKilled = 0;
            damageDealt = 0f;
            gameOver = false;
            ChangeGameState(GameState.Gameplay);
        }

        public void RestartGame()
        {
            Debug.Log("GameManager: Restarting game");
            gameTime = 0f;
            enemiesKilled = 0;
            damageDealt = 0f;
            gameOver = false;
            ChangeGameState(GameState.Loading);
            // Scene reload will be handled by SceneManager when created
        }

        public void ReturnToMainMenu()
        {
            Debug.Log("GameManager: Returning to main menu");
            ChangeGameState(GameState.MainMenu);
            // Scene loading will be handled by SceneManager when created
        }

        public void PlayerDied()
        {
            Debug.Log("GameManager: Player died");
            gameOver = true;
            OnPlayerDied?.Invoke();
            ChangeGameState(GameState.GameOver);
        }

        public void LevelCompleted()
        {
            Debug.Log("GameManager: Level completed");
            OnLevelCompleted?.Invoke();
            ChangeGameState(GameState.Victory);
        }

        public void QuitGame()
        {
            Debug.Log("GameManager: Quitting game");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Handle pause input (ESC key)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameState.Gameplay)
                {
                    PauseGame();
                }
                else if (currentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }

        #endregion

        #region Event Listeners

        public void OnEventReceived(PlayerDeathEvent eventData)
        {
            if (gameOver) return;
            
            Debug.Log("Game Manager: Player has died!");
            PlayerDied();
            
            // Handle respawn if enabled
            if (respawnPlayer)
            {
                StartCoroutine(RespawnPlayerCoroutine());
            }
        }

        public void OnEventReceived(EnemyDeathEvent eventData)
        {
            enemiesKilled++;
            Debug.Log($"Game Manager: Enemy killed! Total: {enemiesKilled}");
            
            // Check victory condition
            if (enemiesKilled >= enemiesForVictory)
            {
                LevelCompleted();
            }
        }

        public void OnEventReceived(PlayerDamagedEvent eventData)
        {
            damageDealt += eventData.Data.damage;
            Debug.Log($"Game Manager: Player took {eventData.Data.damage} damage. Total damage: {damageDealt}");
        }

        private IEnumerator RespawnPlayerCoroutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            
            if (gameOver)
            {
                Debug.Log("Game Manager: Respawning player...");
                gameOver = false;
                ChangeGameState(GameState.Gameplay);
                
                // Find and reset player
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    var playerStats = player.GetComponent<PlayerStats>();
                    if (playerStats != null)
                    {
                        // Reset player health
                        playerStats.currentHP = playerStats.maxHP;
                    }
                }
            }
        }

        #endregion

        #region Debug

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.Box(new Rect(10, 10, 250, 160), "Game Manager Debug");
            GUI.Label(new Rect(20, 30, 220, 20), $"State: {currentState}");
            GUI.Label(new Rect(20, 50, 220, 20), $"Paused: {isPaused}");
            GUI.Label(new Rect(20, 70, 220, 20), $"Game Time: {gameTime:F1}s");
            GUI.Label(new Rect(20, 90, 220, 20), $"Enemies Killed: {enemiesKilled}/{enemiesForVictory}");
            GUI.Label(new Rect(20, 110, 220, 20), $"Damage Dealt: {damageDealt:F1}");

            if (GUI.Button(new Rect(20, 130, 80, 20), "Pause/Resume"))
            {
                TogglePause();
            }

            if (GUI.Button(new Rect(110, 130, 60, 20), "Restart"))
            {
                RestartGame();
            }

            if (GUI.Button(new Rect(180, 130, 60, 20), "Quit"))
            {
                QuitGame();
            }
        }

        #endregion
    }

    #region Game Manager Events

    /// <summary>
    /// Fired when game state changes
    /// </summary>
    public class GameStateChangedEvent : GameEvent
    {
        public GameManager.GameState PreviousState { get; private set; }
        public GameManager.GameState NewState { get; private set; }

        public GameStateChangedEvent(GameManager.GameState previousState, GameManager.GameState newState, GameObject source = null) : base(source)
        {
            PreviousState = previousState;
            NewState = newState;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - {PreviousState} → {NewState}";
        }
    }

    /// <summary>
    /// Fired when game is paused or resumed
    /// </summary>
    public class GamePausedEvent : GameEvent
    {
        public bool IsPaused { get; private set; }

        public GamePausedEvent(bool isPaused, GameObject source = null) : base(source)
        {
            IsPaused = isPaused;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Paused: {IsPaused}";
        }
    }

    #endregion
}