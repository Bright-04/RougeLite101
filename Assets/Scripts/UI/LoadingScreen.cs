using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Manages the loading screen that appears between room transitions
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI tipText;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float minimumDisplayTime = 0.5f; // Minimum time to show loading screen

    [Header("Loading Tips")]
    [SerializeField] private string[] loadingTips = new string[]
    {
        "Tip: Clear all enemies to unlock the exit door",
        "Tip: Watch out for charging bat enemies!",
        "Tip: Slimes deal contact damage - keep your distance",
        "Tip: Collect health pickups to survive longer",
        "Tip: Each room gets progressively harder",
        "Tip: Use your sword to defeat enemies",
        "Tip: Different biomes have different enemies",
        "Tip: Stay alert - enemies can appear anywhere!",
        "Tip: Master your dodge timing to avoid damage",
        "Tip: Explore every corner for secrets"
    };

    private bool isShowing = false;
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
        
        // Don't destroy on load so it persists between scenes if needed
        DontDestroyOnLoad(gameObject);

        // Ensure we start hidden
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Shows the loading screen with a fade-in effect
    /// </summary>
    public void Show()
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        currentTransition = StartCoroutine(ShowRoutine());
    }

    /// <summary>
    /// Hides the loading screen with a fade-out effect
    /// </summary>
    public void Hide()
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        currentTransition = StartCoroutine(HideRoutine());
    }

    /// <summary>
    /// Shows the loading screen, waits for the minimum display time, then hides it
    /// </summary>
    /// <returns></returns>
    public IEnumerator ShowAndHideRoutine()
    {
        yield return ShowRoutine();
        yield return new WaitForSeconds(minimumDisplayTime);
        yield return HideRoutine();
    }

    private IEnumerator ShowRoutine()
    {
        isShowing = true;
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true; // Block input during loading
        }

        // Set random tip
        if (tipText != null && loadingTips.Length > 0)
        {
            tipText.text = loadingTips[Random.Range(0, loadingTips.Length)];
        }

        // Reset progress bar
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }

        // Set loading text
        if (loadingText != null)
        {
            loadingText.text = "Loading...";
        }

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time in case game is paused
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }

            // Animate progress bar
            if (progressBar != null)
            {
                progressBar.fillAmount = Mathf.Lerp(0f, 0.8f, elapsed / fadeInDuration);
            }

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Fill progress bar
        if (progressBar != null)
        {
            progressBar.fillAmount = 1f;
        }

        currentTransition = null;
    }

    private IEnumerator HideRoutine()
    {
        isShowing = false;

        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
        currentTransition = null;
    }

    /// <summary>
    /// Updates the progress bar value (0 to 1)
    /// </summary>
    public void SetProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = Mathf.Clamp01(progress);
        }
    }

    /// <summary>
    /// Updates the loading text
    /// </summary>
    public void SetLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    public bool IsShowing => isShowing;
}
