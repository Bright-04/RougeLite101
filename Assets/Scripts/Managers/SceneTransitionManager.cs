using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RougeLite.Events;

namespace RougeLite.Managers
{
    /// <summary>
    /// Scene Manager wrapper for scene loading, transitions, and scene state management
    /// Provides loading screens, scene preloading, and transition effects
    /// </summary>
    public class SceneTransitionManager : EventBehaviour
    {
        #region Singleton
        
        private static SceneTransitionManager _instance;
        public static SceneTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SceneTransitionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SceneTransitionManager");
                        _instance = go.AddComponent<SceneTransitionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Scene Settings

        [Header("Scene Transition Settings")]
        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private float fadeOutDuration = 1f;
        [SerializeField] private float minimumLoadingTime = 2f;
        [SerializeField] private bool showLoadingScreen = true;
        [SerializeField] private bool enableScenePreloading = true;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameplaySceneName = "Gameplay";
        [SerializeField] private string loadingSceneName = "Loading";

        #endregion

        #region Scene State

        private bool isTransitioning = false;
        private string currentSceneName;
        private string targetSceneName;
        private AsyncOperation currentLoadOperation;
        private float loadingStartTime;
        private Dictionary<string, AsyncOperation> preloadedScenes;

        #endregion

        #region Events

        public System.Action<string> OnSceneLoadStarted;
        public System.Action<string, float> OnSceneLoadProgress;
        public System.Action<string> OnSceneLoadCompleted;
        public System.Action<string> OnSceneUnloaded;
        public System.Action OnTransitionStarted;
        public System.Action OnTransitionCompleted;

        #endregion

        #region Properties

        public bool IsTransitioning => isTransitioning;
        public string CurrentSceneName => currentSceneName;
        public float LoadingProgress => currentLoadOperation?.progress ?? 0f;
        public string LoadingSceneName => loadingSceneName;

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
                InitializeSceneManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Get current scene
            currentSceneName = SceneManager.GetActiveScene().name;
            
            // Preload common scenes if enabled
            if (enableScenePreloading)
            {
                StartCoroutine(PreloadCommonScenes());
            }
        }

        #endregion

        #region Initialization

        private void InitializeSceneManager()
        {
            Debug.Log("SceneTransitionManager: Initializing...");

            preloadedScenes = new Dictionary<string, AsyncOperation>();

            // Subscribe to Unity's scene management events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;

            Debug.Log("SceneTransitionManager: Initialization complete");
        }

        protected override void OnDestroy()
        {
            // Unsubscribe from events
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            
            // Call base OnDestroy
            base.OnDestroy();
        }

        #endregion

        #region Scene Loading

        public void LoadScene(string sceneName, bool useTransition = true)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("SceneTransitionManager: Already transitioning, ignoring load request");
                return;
            }

            if (sceneName == currentSceneName)
            {
                Debug.LogWarning($"SceneTransitionManager: Already in scene {sceneName}");
                return;
            }

            targetSceneName = sceneName;

            if (useTransition)
            {
                StartCoroutine(LoadSceneWithTransition(sceneName));
            }
            else
            {
                StartCoroutine(LoadSceneDirectly(sceneName));
            }
        }

        public void LoadMainMenu()
        {
            LoadScene(mainMenuSceneName);
        }

        public void LoadGameplay()
        {
            LoadScene(gameplaySceneName);
        }

        public void ReloadCurrentScene()
        {
            LoadScene(currentSceneName);
        }

        private IEnumerator LoadSceneWithTransition(string sceneName)
        {
            isTransitioning = true;
            OnTransitionStarted?.Invoke();

            // Notify UI Manager to show loading screen
            var uiManager = UIManager.Instance;
            if (uiManager != null && showLoadingScreen)
            {
                uiManager.ShowPanel(UIManager.UIPanel.Loading);
            }

            // Fade out
            yield return StartCoroutine(FadeOut());

            // Load the scene
            yield return StartCoroutine(LoadSceneAsync(sceneName));

            // Fade in
            yield return StartCoroutine(FadeIn());

            isTransitioning = false;
            OnTransitionCompleted?.Invoke();

            Debug.Log($"SceneTransitionManager: Scene transition to {sceneName} completed");
        }

        private IEnumerator LoadSceneDirectly(string sceneName)
        {
            isTransitioning = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            yield return StartCoroutine(LoadSceneAsync(sceneName));

            isTransitioning = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            loadingStartTime = Time.realtimeSinceStartup;
            
            // Check if scene is preloaded
            if (preloadedScenes.ContainsKey(sceneName))
            {
                currentLoadOperation = preloadedScenes[sceneName];
                preloadedScenes.Remove(sceneName);
                
                // Allow the preloaded scene to activate
                currentLoadOperation.allowSceneActivation = true;
            }
            else
            {
                // Load scene normally
                currentLoadOperation = SceneManager.LoadSceneAsync(sceneName);
            }

            if (currentLoadOperation == null)
            {
                Debug.LogError($"SceneTransitionManager: Failed to load scene {sceneName}");
                yield break;
            }

            OnSceneLoadStarted?.Invoke(sceneName);

            // Wait for loading with progress updates
            while (!currentLoadOperation.isDone)
            {
                float progress = Mathf.Clamp01(currentLoadOperation.progress / 0.9f);
                OnSceneLoadProgress?.Invoke(sceneName, progress);

                // Update UI loading progress
                var uiManager = UIManager.Instance;
                if (uiManager != null)
                {
                    uiManager.SetLoadingProgress(progress);
                }

                yield return null;
            }

            // Ensure minimum loading time for smooth experience
            float loadingTime = Time.realtimeSinceStartup - loadingStartTime;
            if (loadingTime < minimumLoadingTime)
            {
                yield return new WaitForSecondsRealtime(minimumLoadingTime - loadingTime);
            }

            currentSceneName = sceneName;
            OnSceneLoadCompleted?.Invoke(sceneName);

            // Broadcast scene changed event
            BroadcastEvent(new SceneChangedEvent(sceneName));
        }

        #endregion

        #region Scene Preloading

        private IEnumerator PreloadCommonScenes()
        {
            // Preload main menu if not current scene
            if (currentSceneName != mainMenuSceneName)
            {
                yield return StartCoroutine(PreloadSceneAsync(mainMenuSceneName));
            }

            // Preload gameplay scene if not current scene
            if (currentSceneName != gameplaySceneName)
            {
                yield return StartCoroutine(PreloadSceneAsync(gameplaySceneName));
            }
        }

        public void PreloadScene(string sceneName)
        {
            if (!enableScenePreloading) return;
            if (preloadedScenes.ContainsKey(sceneName)) return;
            if (sceneName == currentSceneName) return;

            StartCoroutine(PreloadSceneAsync(sceneName));
        }

        private IEnumerator PreloadSceneAsync(string sceneName)
        {
            Debug.Log($"SceneTransitionManager: Preloading scene {sceneName}");

            AsyncOperation preloadOperation = SceneManager.LoadSceneAsync(sceneName);
            preloadOperation.allowSceneActivation = false; // Don't activate yet

            preloadedScenes[sceneName] = preloadOperation;

            // Wait until preload is 90% complete (Unity limitation)
            while (preloadOperation.progress < 0.9f)
            {
                yield return null;
            }

            Debug.Log($"SceneTransitionManager: Scene {sceneName} preloaded");
        }

        #endregion

        #region Transition Effects

        private IEnumerator FadeOut()
        {
            Debug.Log("SceneTransitionManager: Fading out");
            
            // This could be implemented with a UI panel or camera effect
            // For now, just wait for the duration
            yield return new WaitForSecondsRealtime(fadeOutDuration);
        }

        private IEnumerator FadeIn()
        {
            Debug.Log("SceneTransitionManager: Fading in");
            
            // This could be implemented with a UI panel or camera effect
            // For now, just wait for the duration
            yield return new WaitForSecondsRealtime(fadeInDuration);
        }

        #endregion

        #region Scene Events

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"SceneTransitionManager: Scene {scene.name} loaded");
            
            // Update current scene if it's the main scene
            if (mode == LoadSceneMode.Single)
            {
                currentSceneName = scene.name;
            }
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            Debug.Log($"SceneTransitionManager: Scene {scene.name} unloaded");
            OnSceneUnloaded?.Invoke(scene.name);
        }

        #endregion

        #region Utility Methods

        public bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                {
                    return true;
                }
            }
            return false;
        }

        public void UnloadScene(string sceneName)
        {
            if (IsSceneLoaded(sceneName) && sceneName != currentSceneName)
            {
                SceneManager.UnloadSceneAsync(sceneName);
            }
        }

        public List<string> GetLoadedScenes()
        {
            List<string> loadedScenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    loadedScenes.Add(scene.name);
                }
            }
            return loadedScenes;
        }

        #endregion

        #region Settings

        public void SetFadeDuration(float fadeIn, float fadeOut)
        {
            fadeInDuration = Mathf.Max(0f, fadeIn);
            fadeOutDuration = Mathf.Max(0f, fadeOut);
        }

        public void SetMinimumLoadingTime(float time)
        {
            minimumLoadingTime = Mathf.Max(0f, time);
        }

        public void EnableLoadingScreen(bool enable)
        {
            showLoadingScreen = enable;
        }

        public void EnableScenePreloading(bool enable)
        {
            enableScenePreloading = enable;
            
            if (!enable)
            {
                // Clear preloaded scenes
                preloadedScenes.Clear();
            }
        }

        #endregion

        #region Debug

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.Box(new Rect(320, 10, 250, 140), "Scene Manager Debug");
            GUI.Label(new Rect(330, 30, 230, 20), $"Current Scene: {currentSceneName}");
            GUI.Label(new Rect(330, 50, 230, 20), $"Transitioning: {isTransitioning}");
            GUI.Label(new Rect(330, 70, 230, 20), $"Load Progress: {LoadingProgress:P0}");
            GUI.Label(new Rect(330, 90, 230, 20), $"Preloaded: {preloadedScenes.Count}");

            if (GUI.Button(new Rect(330, 110, 100, 20), "Main Menu"))
            {
                LoadMainMenu();
            }

            if (GUI.Button(new Rect(440, 110, 100, 20), "Gameplay"))
            {
                LoadGameplay();
            }

            if (GUI.Button(new Rect(330, 130, 100, 20), "Reload"))
            {
                ReloadCurrentScene();
            }

            if (GUI.Button(new Rect(440, 130, 100, 20), "Clear Preload"))
            {
                preloadedScenes.Clear();
            }
        }

        #endregion
    }

    #region Scene Events

    /// <summary>
    /// Fired when scene changes
    /// </summary>
    public class SceneChangedEvent : GameEvent
    {
        public string SceneName { get; private set; }

        public SceneChangedEvent(string sceneName, GameObject source = null) : base(source)
        {
            SceneName = sceneName;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Scene: {SceneName}";
        }
    }

    /// <summary>
    /// Fired when scene loading starts
    /// </summary>
    public class SceneLoadStartedEvent : GameEvent
    {
        public string SceneName { get; private set; }

        public SceneLoadStartedEvent(string sceneName, GameObject source = null) : base(source)
        {
            SceneName = sceneName;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Loading: {SceneName}";
        }
    }

    /// <summary>
    /// Fired when scene loading completes
    /// </summary>
    public class SceneLoadCompletedEvent : GameEvent
    {
        public string SceneName { get; private set; }
        public float LoadTime { get; private set; }

        public SceneLoadCompletedEvent(string sceneName, float loadTime, GameObject source = null) : base(source)
        {
            SceneName = sceneName;
            LoadTime = loadTime;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Loaded: {SceneName} ({LoadTime:F1}s)";
        }
    }

    #endregion
}