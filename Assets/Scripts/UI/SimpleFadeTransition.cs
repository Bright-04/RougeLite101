using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Simple fade to black transition - no setup required!
/// Automatically creates a full-screen black overlay when needed.
/// </summary>
public class SimpleFadeTransition : MonoBehaviour
{
    public static SimpleFadeTransition Instance { get; private set; }

    private Canvas canvas;
    private Image fadeImage;
    private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float holdDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateFadeUI();
    }

    private void CreateFadeUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("SimpleFadeCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Render on top of everything
        canvasObj.AddComponent<GraphicRaycaster>();

        // Add canvas scaler
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Create black panel
        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        fadeImage = panelObj.AddComponent<Image>();
        fadeImage.color = Color.black;

        canvasGroup = panelObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        canvasObj.SetActive(false);
    }

    /// <summary>
    /// Fades to black, holds, then fades back to clear
    /// </summary>
    public IEnumerator FadeTransition()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);

        // Fade to black
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            }
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // Hold at black
        yield return new WaitForSecondsRealtime(holdDuration);

        // Fade back to clear
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            }
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Just fade in (to black)
    /// </summary>
    public IEnumerator FadeIn()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            }
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Just fade out (from black)
    /// </summary>
    public IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            }
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (canvas != null) canvas.gameObject.SetActive(false);
    }
}
