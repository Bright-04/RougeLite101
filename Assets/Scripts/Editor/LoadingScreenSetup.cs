#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility to quickly create a loading screen UI
/// Usage: Right-click in Hierarchy → UI → Create Loading Screen
/// </summary>
public class LoadingScreenSetup
{
    [MenuItem("GameObject/UI/Create Loading Screen", false, 10)]
    static void CreateLoadingScreenUI(MenuCommand menuCommand)
    {
        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("Created new Canvas for LoadingScreen");
        }

        // Create LoadingScreen Panel
        GameObject loadingScreenObj = new GameObject("LoadingScreen", typeof(RectTransform));
        loadingScreenObj.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = loadingScreenObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // Add Image component for background
        Image panelImage = loadingScreenObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.95f); // Almost black

        // Add CanvasGroup for fade effects
        CanvasGroup canvasGroup = loadingScreenObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        // Create Loading Text
        GameObject loadingTextObj = new GameObject("LoadingText", typeof(RectTransform));
        loadingTextObj.transform.SetParent(loadingScreenObj.transform, false);
        
        RectTransform loadingTextRect = loadingTextObj.GetComponent<RectTransform>();
        loadingTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingTextRect.sizeDelta = new Vector2(800, 100);
        loadingTextRect.anchoredPosition = new Vector2(0, 200);

        TextMeshProUGUI loadingText = loadingTextObj.AddComponent<TextMeshProUGUI>();
        loadingText.text = "Loading...";
        loadingText.fontSize = 60;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.color = Color.white;

        // Create Progress Bar Background
        GameObject progressBgObj = new GameObject("ProgressBarBackground", typeof(RectTransform));
        progressBgObj.transform.SetParent(loadingScreenObj.transform, false);
        
        RectTransform progressBgRect = progressBgObj.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressBgRect.sizeDelta = new Vector2(600, 40);
        progressBgRect.anchoredPosition = new Vector2(0, 50);

        Image progressBgImage = progressBgObj.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Create Progress Bar Fill
        GameObject progressFillObj = new GameObject("ProgressBarFill", typeof(RectTransform));
        progressFillObj.transform.SetParent(progressBgObj.transform, false);
        
        RectTransform progressFillRect = progressFillObj.GetComponent<RectTransform>();
        progressFillRect.anchorMin = new Vector2(0, 0);
        progressFillRect.anchorMax = new Vector2(0, 1);
        progressFillRect.sizeDelta = new Vector2(600, 0);
        progressFillRect.anchoredPosition = Vector2.zero;
        progressFillRect.pivot = new Vector2(0, 0.5f);

        Image progressFillImage = progressFillObj.AddComponent<Image>();
        progressFillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillAmount = 0f;

        // Create Tip Text
        GameObject tipTextObj = new GameObject("TipText", typeof(RectTransform));
        tipTextObj.transform.SetParent(loadingScreenObj.transform, false);
        
        RectTransform tipTextRect = tipTextObj.GetComponent<RectTransform>();
        tipTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipTextRect.sizeDelta = new Vector2(1400, 100);
        tipTextRect.anchoredPosition = new Vector2(0, -150);

        TextMeshProUGUI tipText = tipTextObj.AddComponent<TextMeshProUGUI>();
        tipText.text = "Tip: Loading tips will appear here!";
        tipText.fontSize = 28;
        tipText.alignment = TextAlignmentOptions.Center;
        tipText.color = new Color(1f, 1f, 0.5f, 1f); // Light yellow
        tipText.enableWordWrapping = true;

        // Add LoadingScreen component
        LoadingScreen loadingScreen = loadingScreenObj.AddComponent<LoadingScreen>();
        
        // Use reflection to set private serialized fields
        var loadingScreenType = typeof(LoadingScreen);
        var canvasGroupField = loadingScreenType.GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var loadingTextField = loadingScreenType.GetField("loadingText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var progressBarField = loadingScreenType.GetField("progressBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tipTextField = loadingScreenType.GetField("tipText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (canvasGroupField != null) canvasGroupField.SetValue(loadingScreen, canvasGroup);
        if (loadingTextField != null) loadingTextField.SetValue(loadingScreen, loadingText);
        if (progressBarField != null) progressBarField.SetValue(loadingScreen, progressFillImage);
        if (tipTextField != null) tipTextField.SetValue(loadingScreen, tipText);

        // Select the created object
        Selection.activeGameObject = loadingScreenObj;

        // Mark scene as dirty
        EditorUtility.SetDirty(loadingScreenObj);
        
        // Disable the loading screen initially
        loadingScreenObj.SetActive(false);

        Debug.Log("<color=green>Loading Screen UI created successfully!</color>");
        Debug.Log("The LoadingScreen component has been automatically configured.");
        Debug.Log("You can customize colors, timings, and tips in the Inspector.");
    }
}
#endif
