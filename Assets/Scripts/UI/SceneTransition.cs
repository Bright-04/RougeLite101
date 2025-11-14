using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles fade in/out transitions between rooms using a UI panel
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;
    
    private static SceneTransition _instance;
    public static SceneTransition Instance => _instance;

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Setup fade panel
        if (fadePanel != null)
        {
            fadePanel.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        }
    }

    /// <summary>
    /// Fade to black
    /// </summary>
    public IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        Color color = fadeColor;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            color.a = alpha;
            fadePanel.color = color;
            yield return null;
        }
        
        color.a = 1f;
        fadePanel.color = color;
    }

    /// <summary>
    /// Fade from black to transparent
    /// </summary>
    public IEnumerator FadeIn()
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        Color color = fadeColor;
        color.a = 1f;
        fadePanel.color = color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            color.a = alpha;
            fadePanel.color = color;
            yield return null;
        }
        
        color.a = 0f;
        fadePanel.color = color;
    }

    /// <summary>
    /// Convenience method to fade out, execute action, then fade in
    /// </summary>
    public IEnumerator FadeOutAndIn(System.Action onFadedOut)
    {
        yield return FadeOut();
        onFadedOut?.Invoke();
        yield return FadeIn();
    }
}
