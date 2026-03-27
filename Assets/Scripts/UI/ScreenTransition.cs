using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenTransition : MonoBehaviour
{
    [Serializable]
    private class TransitionPreset
    {
        [Min(0f)] public float fadeOutDuration = 0.12f;
        [Min(0f)] public float holdDuration = 0.06f;
        [Min(0f)] public float fadeInDuration = 0.16f;
        public bool showLoadingText = false;
        public string loadingText = "Loading...";
    }

    private struct RuntimeTransitionSettings
    {
        public float FadeOutDuration;
        public float HoldDuration;
        public float FadeInDuration;
        public bool ShowLoadingText;
        public string LoadingText;
    }

    private static ScreenTransition _instance;

    [Header("Default Presets")]
    [SerializeField] private TransitionPreset roomPreset = new TransitionPreset
    {
        fadeOutDuration = 0.1f,
        holdDuration = 0.04f,
        fadeInDuration = 0.14f,
        showLoadingText = false,
        loadingText = "Loading..."
    };

    [SerializeField] private TransitionPreset scenePreset = new TransitionPreset
    {
        fadeOutDuration = 0.14f,
        holdDuration = 0.08f,
        fadeInDuration = 0.18f,
        showLoadingText = true,
        loadingText = "Loading..."
    };

    [Header("Visual")]
    [SerializeField] private Color overlayColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float maxOverlayAlpha = 1f;
    [SerializeField] private int sortingOrder = 5000;
    [SerializeField] private Color loadingColor = Color.white;
    [SerializeField, Min(8)] private int loadingFontSize = 20;
    [SerializeField, Min(0f)] private float spinnerSpeed = 260f;

    private CanvasGroup _canvasGroup;
    private RectTransform _spinnerRoot;
    private GameObject _loadingGroup;
    private Text _loadingText;
    private bool _isTransitioning;

    public static bool IsTransitioning => _instance != null && _instance._isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static ScreenTransition EnsureInstance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        var existing = FindFirstObjectByType<ScreenTransition>();
        if (existing != null)
        {
            _instance = existing;
            return _instance;
        }

        var go = new GameObject("ScreenTransition");
        _instance = go.AddComponent<ScreenTransition>();
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        CreateOverlay();
        _canvasGroup.alpha = 0f;
        SetLoadingVisuals(false, null);
    }

    private void Update()
    {
        if (_isTransitioning && _spinnerRoot != null && _loadingGroup != null && _loadingGroup.activeSelf)
        {
            _spinnerRoot.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);
        }
    }

    private void CreateOverlay()
    {
        if (_canvasGroup != null)
        {
            return;
        }

        var canvasGO = new GameObject("OverlayCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        canvasGO.AddComponent<GraphicRaycaster>();

        _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        var overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);

        var rect = overlayGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = overlayGO.AddComponent<Image>();
        var color = overlayColor;
        color.a = maxOverlayAlpha;
        image.color = color;

        _loadingGroup = new GameObject("LoadingGroup");
        _loadingGroup.transform.SetParent(canvasGO.transform, false);

        var loadingRect = _loadingGroup.AddComponent<RectTransform>();
        loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingRect.pivot = new Vector2(0.5f, 0.5f);
        loadingRect.anchoredPosition = new Vector2(0f, -120f);
        loadingRect.sizeDelta = new Vector2(240f, 80f);

        var spinnerGO = new GameObject("Spinner");
        spinnerGO.transform.SetParent(_loadingGroup.transform, false);
        _spinnerRoot = spinnerGO.AddComponent<RectTransform>();
        _spinnerRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _spinnerRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _spinnerRoot.pivot = new Vector2(0.5f, 0.5f);
        _spinnerRoot.anchoredPosition = new Vector2(0f, 16f);
        _spinnerRoot.sizeDelta = new Vector2(26f, 26f);

        CreateSpinnerBar("BarA", _spinnerRoot, 0f);
        CreateSpinnerBar("BarB", _spinnerRoot, 90f);

        var textGO = new GameObject("LoadingText");
        textGO.transform.SetParent(_loadingGroup.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, -18f);
        textRect.sizeDelta = new Vector2(240f, 32f);

        _loadingText = textGO.AddComponent<Text>();
        _loadingText.alignment = TextAnchor.MiddleCenter;
        _loadingText.fontSize = loadingFontSize;
        _loadingText.color = loadingColor;
        // Unity 6 removed Arial.ttf from built-in resources.
        _loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static IEnumerator Play(Action swapAction)
    {
        var instance = EnsureInstance();
        yield return instance.PlayRoutine(instance.InvokeAction(swapAction), instance.BuildFromPreset(instance.roomPreset));
    }

    public static IEnumerator Play(Action swapAction, float fadeOutDuration, float holdDuration, float fadeInDuration, bool showLoadingText, string loadingText)
    {
        var instance = EnsureInstance();
        var settings = instance.BuildRuntimeSettings(fadeOutDuration, holdDuration, fadeInDuration, showLoadingText, loadingText);
        yield return instance.PlayRoutine(instance.InvokeAction(swapAction), settings);
    }

    public static IEnumerator PlaySceneLoad(string sceneName)
    {
        var instance = EnsureInstance();
        yield return instance.PlayRoutine(instance.LoadSceneRoutine(sceneName), instance.BuildFromPreset(instance.scenePreset));
    }

    public static IEnumerator PlaySceneLoad(string sceneName, float fadeOutDuration, float holdDuration, float fadeInDuration, bool showLoadingText, string loadingText)
    {
        var instance = EnsureInstance();
        var settings = instance.BuildRuntimeSettings(fadeOutDuration, holdDuration, fadeInDuration, showLoadingText, loadingText);
        yield return instance.PlayRoutine(instance.LoadSceneRoutine(sceneName), settings);
    }

    private IEnumerator PlayRoutine(IEnumerator swapRoutine, RuntimeTransitionSettings settings)
    {
        if (_isTransitioning)
        {
            yield break;
        }

        _isTransitioning = true;
        _canvasGroup.blocksRaycasts = true;
        SetLoadingVisuals(settings.ShowLoadingText, settings.LoadingText);

        yield return Fade(0f, maxOverlayAlpha, settings.FadeOutDuration);

        if (swapRoutine != null)
        {
            yield return StartCoroutine(swapRoutine);
        }

        if (settings.HoldDuration > 0f)
        {
            yield return new WaitForSeconds(settings.HoldDuration);
        }

        yield return Fade(maxOverlayAlpha, 0f, settings.FadeInDuration);
        SetLoadingVisuals(false, null);
        _canvasGroup.blocksRaycasts = false;
        _isTransitioning = false;
    }

    private RuntimeTransitionSettings BuildFromPreset(TransitionPreset preset)
    {
        if (preset == null)
        {
            return BuildRuntimeSettings(0.12f, 0.06f, 0.16f, false, "Loading...");
        }

        return BuildRuntimeSettings(
            preset.fadeOutDuration,
            preset.holdDuration,
            preset.fadeInDuration,
            preset.showLoadingText,
            preset.loadingText
        );
    }

    private RuntimeTransitionSettings BuildRuntimeSettings(float fadeOut, float hold, float fadeIn, bool showLoadingText, string loadingText)
    {
        return new RuntimeTransitionSettings
        {
            FadeOutDuration = Mathf.Max(0f, fadeOut),
            HoldDuration = Mathf.Max(0f, hold),
            FadeInDuration = Mathf.Max(0f, fadeIn),
            ShowLoadingText = showLoadingText,
            LoadingText = string.IsNullOrWhiteSpace(loadingText) ? "Loading..." : loadingText
        };
    }

    private void SetLoadingVisuals(bool visible, string text)
    {
        if (_loadingGroup == null)
        {
            return;
        }

        _loadingGroup.SetActive(visible);
        if (_loadingText != null)
        {
            _loadingText.text = string.IsNullOrWhiteSpace(text) ? "Loading..." : text;
        }
    }

    private void CreateSpinnerBar(string name, Transform parent, float rotationZ)
    {
        var bar = new GameObject(name);
        bar.transform.SetParent(parent, false);

        var rect = bar.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(4f, 24f);
        rect.localEulerAngles = new Vector3(0f, 0f, rotationZ);

        var image = bar.AddComponent<Image>();
        image.color = loadingColor;
    }

    private IEnumerator InvokeAction(Action action)
    {
        action?.Invoke();
        yield return null;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("ScreenTransition: Scene name is empty.");
            yield break;
        }

        var operation = SceneManager.LoadSceneAsync(sceneName);
        while (operation != null && !operation.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (_canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            _canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        _canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        _canvasGroup.alpha = to;
    }
}