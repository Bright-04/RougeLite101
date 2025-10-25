using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RougeLite.Events;
using RougeLite.Player;

namespace RougeLite.Managers
{
    /// <summary>
    /// UI Manager handles all user interface elements, panels, menus, and UI state management
    /// Provides centralized UI control and event-based UI updates
    /// </summary>
    public class UIManager : EventBehaviour, IEventListener<PlayerDamagedEvent>, IEventListener<PlayerHealedEvent>
    {
        #region Singleton
        
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region UI Panels

        [Header("UI Panels")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayHUD;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("HUD Elements")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider manaBar;
        [SerializeField] private Slider staminaBar;
        [SerializeField] private Text healthText;
        [SerializeField] private Text manaText;
        [SerializeField] private Text staminaText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text enemiesKilledText;

        [Header("Menu Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        #endregion

        #region State Management

        private Dictionary<UIPanel, GameObject> uiPanels;
        private UIPanel currentActivePanel = UIPanel.MainMenu;
        private PlayerStats playerStats;

        public enum UIPanel
        {
            None,
            MainMenu,
            GameplayHUD,
            Pause,
            GameOver,
            Victory,
            Settings,
            Loading
        }

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
                InitializeUI();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Subscribe to events
            SubscribeToEvent<PlayerDamagedEvent>(this);
            SubscribeToEvent<PlayerHealedEvent>(this);
        }

        private void Start()
        {
            SetupButtonListeners();
            FindPlayerStats();
            ShowPanel(UIPanel.GameplayHUD); // Start with HUD for now
        }

        private void Update()
        {
            UpdateHUD();
        }

        protected override void OnDestroy()
        {
            UnsubscribeFromEvent<PlayerDamagedEvent>(this);
            UnsubscribeFromEvent<PlayerHealedEvent>(this);
            base.OnDestroy();
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            Debug.Log("UIManager: Initializing...");

            // Initialize panel dictionary
            uiPanels = new Dictionary<UIPanel, GameObject>
            {
                { UIPanel.MainMenu, mainMenuPanel },
                { UIPanel.GameplayHUD, gameplayHUD },
                { UIPanel.Pause, pausePanel },
                { UIPanel.GameOver, gameOverPanel },
                { UIPanel.Victory, victoryPanel },
                { UIPanel.Settings, settingsPanel },
                { UIPanel.Loading, loadingPanel }
            };

            // Ensure canvas exists
            if (mainCanvas == null)
            {
                mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas == null)
                {
                    CreateMainCanvas();
                }
            }

            // Hide all panels initially
            HideAllPanels();
        }

        private void CreateMainCanvas()
        {
            GameObject canvasGO = new GameObject("Main Canvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 0;

            // Add Canvas Scaler
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster
            canvasGO.AddComponent<GraphicRaycaster>();

            DontDestroyOnLoad(canvasGO);
        }

        private void SetupButtonListeners()
        {
            if (startGameButton != null)
                startGameButton.onClick.AddListener(() => GameManager.Instance.StartNewGame());
            
            if (resumeButton != null)
                resumeButton.onClick.AddListener(() => GameManager.Instance.ResumeGame());
            
            if (restartButton != null)
                restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => ShowPanel(UIPanel.Settings));
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMainMenu());
            
            if (quitButton != null)
                quitButton.onClick.AddListener(() => GameManager.Instance.QuitGame());
        }

        private void FindPlayerStats()
        {
            if (playerStats == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                }
            }
        }

        #endregion

        #region Panel Management

        public void ShowPanel(UIPanel panel)
        {
            if (currentActivePanel == panel) return;

            Debug.Log($"UIManager: Switching to {panel} panel");

            // Hide current panel
            HideAllPanels();

            // Show new panel
            if (uiPanels.ContainsKey(panel) && uiPanels[panel] != null)
            {
                uiPanels[panel].SetActive(true);
                currentActivePanel = panel;
            }
            else
            {
                Debug.LogWarning($"UIManager: Panel {panel} not found or not assigned!");
            }
        }

        public void HidePanel(UIPanel panel)
        {
            if (uiPanels.ContainsKey(panel) && uiPanels[panel] != null)
            {
                uiPanels[panel].SetActive(false);
                
                if (currentActivePanel == panel)
                {
                    currentActivePanel = UIPanel.None;
                }
            }
        }

        public void HideAllPanels()
        {
            foreach (var panel in uiPanels.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
            currentActivePanel = UIPanel.None;
        }

        public bool IsPanelActive(UIPanel panel)
        {
            return currentActivePanel == panel;
        }

        #endregion

        #region HUD Updates

        private void UpdateHUD()
        {
            if (currentActivePanel != UIPanel.GameplayHUD) return;
            if (playerStats == null) FindPlayerStats();
            if (playerStats == null) return;

            // Update health bar
            if (healthBar != null)
            {
                healthBar.value = playerStats.currentHP / playerStats.maxHP;
            }
            if (healthText != null)
            {
                healthText.text = $"{playerStats.currentHP:F0}/{playerStats.maxHP:F0}";
            }

            // Update mana bar
            if (manaBar != null)
            {
                manaBar.value = playerStats.currentMana / playerStats.maxMana;
            }
            if (manaText != null)
            {
                manaText.text = $"{playerStats.currentMana:F0}/{playerStats.maxMana:F0}";
            }

            // Update stamina bar
            if (staminaBar != null)
            {
                staminaBar.value = playerStats.currentStamina / playerStats.maxStamina;
            }
            if (staminaText != null)
            {
                staminaText.text = $"{playerStats.currentStamina:F0}/{playerStats.maxStamina:F0}";
            }

            // Update game stats
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                if (scoreText != null)
                {
                    int score = gameManager.EnemiesKilled * 100 + Mathf.FloorToInt(gameManager.DamageDealt);
                    scoreText.text = $"Score: {score}";
                }

                if (timeText != null)
                {
                    timeText.text = $"Time: {gameManager.GameTime:F1}s";
                }

                if (enemiesKilledText != null)
                {
                    enemiesKilledText.text = $"Enemies: {gameManager.EnemiesKilled}";
                }
            }
        }

        #endregion

        #region Event Listeners

        public void OnEventReceived(PlayerDamagedEvent eventData)
        {
            // Flash health bar red or show damage indicator
            if (healthBar != null)
            {
                StartCoroutine(FlashHealthBar());
            }
        }

        public void OnEventReceived(PlayerHealedEvent eventData)
        {
            // Flash health bar green or show heal indicator
            if (healthBar != null)
            {
                StartCoroutine(FlashHealthBarGreen());
            }
        }

        private System.Collections.IEnumerator FlashHealthBar()
        {
            if (healthBar == null) yield break;

            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                Color originalColor = fillImage.color;
                fillImage.color = Color.red;
                yield return new WaitForSecondsRealtime(0.2f);
                fillImage.color = originalColor;
            }
        }

        private System.Collections.IEnumerator FlashHealthBarGreen()
        {
            if (healthBar == null) yield break;

            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                Color originalColor = fillImage.color;
                fillImage.color = Color.green;
                yield return new WaitForSecondsRealtime(0.2f);
                fillImage.color = originalColor;
            }
        }

        #endregion

        #region Game State Response

        public void OnGameStateChanged(GameManager.GameState previousState, GameManager.GameState newState)
        {
            switch (newState)
            {
                case GameManager.GameState.MainMenu:
                    ShowPanel(UIPanel.MainMenu);
                    break;
                    
                case GameManager.GameState.Loading:
                    ShowPanel(UIPanel.Loading);
                    break;
                    
                case GameManager.GameState.Gameplay:
                    ShowPanel(UIPanel.GameplayHUD);
                    break;
                    
                case GameManager.GameState.Paused:
                    ShowPanel(UIPanel.Pause);
                    break;
                    
                case GameManager.GameState.GameOver:
                    ShowPanel(UIPanel.GameOver);
                    break;
                    
                case GameManager.GameState.Victory:
                    ShowPanel(UIPanel.Victory);
                    break;
                    
                case GameManager.GameState.Settings:
                    ShowPanel(UIPanel.Settings);
                    break;
            }
        }

        #endregion

        #region Public Methods

        public void ShowGameOverPanel(int score, float time, int enemiesKilled)
        {
            ShowPanel(UIPanel.GameOver);
            // Update game over panel with stats
        }

        public void ShowVictoryPanel(int score, float time, int enemiesKilled)
        {
            ShowPanel(UIPanel.Victory);
            // Update victory panel with stats
        }

        public void SetLoadingProgress(float progress)
        {
            // Update loading bar if exists
        }

        #endregion

        #region Debug

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.Box(new Rect(10, 200, 200, 100), "UI Manager Debug");
            GUI.Label(new Rect(20, 220, 180, 20), $"Active Panel: {currentActivePanel}");
            GUI.Label(new Rect(20, 240, 180, 20), $"Canvas: {(mainCanvas != null ? "OK" : "Missing")}");
            GUI.Label(new Rect(20, 260, 180, 20), $"Player Stats: {(playerStats != null ? "Found" : "Missing")}");
        }

        #endregion
    }
}
