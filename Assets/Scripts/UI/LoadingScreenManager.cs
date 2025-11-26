using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Persistent singleton that manages loading screen transitions between scenes and rooms.
/// Provides smooth fade in/out effects with optional progress tracking.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fadePanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject progressBarObject; // Parent of progress bar for showing/hiding

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float minimumDisplayTime = 2.0f; // Minimum time to show loading screen

    [Header("Text Style")]
    [SerializeField] private float textSize = 24f;
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.BottomRight;
    
    [Header("Loading Messages")]
    [SerializeField] private string[] loadingMessages = new string[]
    {
        "Loading...",
        "Preparing the dungeon...",
        "Summoning enemies...",
        "Lighting torches...",
        "Polishing treasures...",
        "Sharpening swords...",
        "Brewing potions..."
    };

    private bool isTransitioning = false;
    private Coroutine currentTransition;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Validate references
        ValidateReferences();

        // Ensure fade panel has correct settings
        if (fadePanel != null)
        {
            // Make sure fade panel color is opaque
            Color panelColor = fadePanel.color;
            if (panelColor.a < 1f)
            {
                Debug.LogWarning($"LoadingScreenManager: FadePanel alpha is {panelColor.a}. Setting to 1 (opaque) for proper fade effect.");
                panelColor.a = 1f;
                fadePanel.color = panelColor;
            }
        }

        // Apply text style settings
        if (loadingText != null)
        {
            loadingText.fontSize = textSize;
            loadingText.alignment = textAlignment;
        }

        // Initialize hidden
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // Hide progress bar by default
        if (progressBarObject != null)
        {
            progressBarObject.SetActive(false);
        }
    }

    private void ValidateReferences()
    {
        bool hasErrors = false;

        if (canvasGroup == null)
        {
            Debug.LogError("LoadingScreenManager: CanvasGroup reference is missing! Please assign it in the Inspector.");
            hasErrors = true;
        }
        else
        {
            Debug.Log($"LoadingScreenManager: CanvasGroup OK (alpha: {canvasGroup.alpha})");
        }

        if (fadePanel == null)
        {
            Debug.LogError("LoadingScreenManager: FadePanel reference is missing! Please assign it in the Inspector.");
            hasErrors = true;
        }
        else
        {
            RectTransform rt = fadePanel.rectTransform;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            
            Debug.Log($"LoadingScreenManager: FadePanel OK");
            Debug.Log($"  - Color: {fadePanel.color}");
            Debug.Log($"  - Rect: {rt.rect}");
            Debug.Log($"  - Anchors: Min({rt.anchorMin.x}, {rt.anchorMin.y}) Max({rt.anchorMax.x}, {rt.anchorMax.y})");
            Debug.Log($"  - Screen Size: {screenSize}");
            Debug.Log($"  - GameObject Active: {fadePanel.gameObject.activeSelf}");
            
            // Check if properly stretched
            if (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one)
            {
                Debug.LogWarning("⚠️ FadePanel is NOT stretched to full screen! Anchors should be (0,0) to (1,1)");
                Debug.LogWarning("   Fix: Select FadePanel → RectTransform → Alt+Shift+Click bottom-right anchor preset");
            }
            
            // Check if rect is too small
            if (rt.rect.width < 100 || rt.rect.height < 100)
            {
                Debug.LogWarning($"⚠️ FadePanel rect is very small ({rt.rect.width}x{rt.rect.height})! It may not cover the screen.");
            }
        }

        if (loadingText == null)
        {
            Debug.LogWarning("LoadingScreenManager: LoadingText reference is missing. Loading messages won't be displayed.");
        }
        else
        {
            Debug.Log($"LoadingScreenManager: LoadingText OK (text: '{loadingText.text}')");
        }

        if (progressBar == null)
        {
            Debug.LogWarning("LoadingScreenManager: ProgressBar reference is missing. Progress won't be displayed.");
        }

        if (!hasErrors)
        {
            Debug.Log("<color=green>✓ LoadingScreenManager initialized successfully!</color>");
        }
        else
        {
            Debug.LogError("<color=red>✗ LoadingScreenManager has missing references! Check above errors.</color>");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Load a scene asynchronously with loading screen
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"Already transitioning to another scene/room. Ignoring request to load '{sceneName}'");
            return;
        }

        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Show loading screen for room transition (no actual scene load)
    /// </summary>
    public void ShowRoomTransition(System.Action onComplete)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Already transitioning. Ignoring room transition request.");
            return;
        }

        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(RoomTransitionCoroutine(onComplete));
    }

    /// <summary>
    /// Show loading screen for room transition - returns a coroutine that can be yielded
    /// </summary>
    public IEnumerator ShowRoomTransitionCoroutine(System.Action onComplete)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Already transitioning. Ignoring room transition request.");
            yield break;
        }

        yield return StartCoroutine(RoomTransitionCoroutine(onComplete));
    }

    /// <summary>
    /// Simple fade in/out transition (no scene load, just visual effect)
    /// </summary>
    public void FadeTransition(System.Action onFadeComplete)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(SimpleFadeCoroutine(onFadeComplete));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isTransitioning = true;
        float startTime = Time.realtimeSinceStartup;

        // Show random loading message
        SetLoadingText(GetRandomLoadingMessage());

        // Show progress bar for scene loading
        if (progressBarObject != null)
        {
            progressBarObject.SetActive(true);
        }

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Start loading scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Update progress bar
        while (!asyncLoad.isDone)
        {
            // AsyncOperation progress goes from 0 to 0.9 when loading
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Scene is ready when progress reaches 0.9
            if (asyncLoad.progress >= 0.9f)
            {
                // Ensure minimum display time
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed < minimumDisplayTime)
                {
                    yield return new WaitForSecondsRealtime(minimumDisplayTime - elapsed);
                }

                // Set progress to 100%
                if (progressBar != null)
                {
                    progressBar.value = 1f;
                }

                // Allow scene activation
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Hide progress bar
        if (progressBarObject != null)
        {
            progressBarObject.SetActive(false);
        }

        isTransitioning = false;
        currentTransition = null;
    }

    private IEnumerator RoomTransitionCoroutine(System.Action onComplete)
    {
        isTransitioning = true;
        float startTime = Time.realtimeSinceStartup;

        // Show random loading message (same as scene transitions)
        SetLoadingText(GetRandomLoadingMessage());

        // Hide progress bar for room transitions
        if (progressBarObject != null)
        {
            progressBarObject.SetActive(false);
        }

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Execute the transition action (e.g., load next room)
        onComplete?.Invoke();

        // Ensure minimum display time
        float elapsed = Time.realtimeSinceStartup - startTime;
        if (elapsed < minimumDisplayTime)
        {
            yield return new WaitForSecondsRealtime(minimumDisplayTime - elapsed);
        }

        // Fade out
        yield return StartCoroutine(FadeOut());

        isTransitioning = false;
        currentTransition = null;
    }

    private IEnumerator SimpleFadeCoroutine(System.Action onFadeComplete)
    {
        // Hide text and progress bar for simple fades
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }
        if (progressBarObject != null)
        {
            progressBarObject.SetActive(false);
        }

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Execute action at peak fade
        onFadeComplete?.Invoke();

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Restore text visibility
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);
        }

        currentTransition = null;
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("FadeIn: CanvasGroup is null, cannot fade!");
            yield break;
        }

        // DIAGNOSTIC: Check FadePanel state
        if (fadePanel != null)
        {
            Debug.Log($"<color=orange>FadePanel Status:</color>");
            Debug.Log($"  - GameObject Active: {fadePanel.gameObject.activeInHierarchy}");
            Debug.Log($"  - Image Enabled: {fadePanel.enabled}");
            Debug.Log($"  - Image Color: {fadePanel.color}");
            Debug.Log($"  - Canvas Active: {canvasGroup.gameObject.activeInHierarchy}");
        }

        Debug.Log($"<color=cyan>Starting FadeIn (duration: {fadeInDuration}s)</color>");
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        Debug.Log($"<color=cyan>FadeIn complete (alpha: {canvasGroup.alpha})</color>");
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("FadeOut: CanvasGroup is null, cannot fade!");
            yield break;
        }

        Debug.Log($"<color=yellow>Starting FadeOut (duration: {fadeOutDuration}s)</color>");
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        Debug.Log($"<color=yellow>FadeOut complete (alpha: {canvasGroup.alpha})</color>");
    }

    private void SetLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    private string GetRandomLoadingMessage()
    {
        if (loadingMessages == null || loadingMessages.Length == 0)
        {
            return "Loading...";
        }

        return loadingMessages[Random.Range(0, loadingMessages.Length)];
    }

    /// <summary>
    /// Check if currently transitioning
    /// </summary>
    public bool IsTransitioning => isTransitioning;
}
