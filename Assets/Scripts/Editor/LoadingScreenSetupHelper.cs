using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor utility to quickly create a complete loading screen setup
/// </summary>
public class LoadingScreenSetupHelper : EditorWindow
{
    [MenuItem("Tools/UI/Setup Loading Screen")]
    public static void ShowWindow()
    {
        GetWindow<LoadingScreenSetupHelper>("Loading Screen Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Loading Screen Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This will create a complete loading screen canvas with:\n" +
            "• Canvas with CanvasGroup\n" +
            "• Black fade panel\n" +
            "• Loading text (TextMeshPro)\n" +
            "• Progress bar\n" +
            "• LoadingScreenManager component",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Create Loading Screen Canvas", GUILayout.Height(40)))
        {
            CreateLoadingScreen();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "After creation:\n" +
            "1. Add the canvas to GameManager's persistent objects array\n" +
            "2. Customize colors and messages in the Inspector\n" +
            "3. Test by entering/exiting rooms or changing scenes",
            MessageType.Warning);
    }

    private void CreateLoadingScreen()
    {
        // Check if one already exists
        var existing = GameObject.Find("LoadingScreenCanvas");
        if (existing != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Loading Screen Exists",
                "A LoadingScreenCanvas already exists. Do you want to delete it and create a new one?",
                "Yes, Replace",
                "Cancel");

            if (overwrite)
            {
                DestroyImmediate(existing);
            }
            else
            {
                return;
            }
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("LoadingScreenCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Create Fade Panel
        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panel = panelObj.AddComponent<Image>();
        panel.color = Color.black;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // Create Loading Text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Loading...";
        text.fontSize = 48;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0, 150);
        textRect.sizeDelta = new Vector2(800, 100);

        // Create Progress Bar Container
        GameObject progressContainer = new GameObject("ProgressBarContainer");
        progressContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = progressContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0, 80);
        containerRect.sizeDelta = new Vector2(600, 30);

        // Create Progress Bar
        GameObject sliderObj = new GameObject("ProgressBar");
        sliderObj.transform.SetParent(progressContainer.transform, false);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-10, -10);

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.3f, 1f); // Green fill

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        // Add LoadingScreenManager component
        LoadingScreenManager manager = canvasObj.AddComponent<LoadingScreenManager>();

        // Use reflection to set private serialized fields
        var managerType = typeof(LoadingScreenManager);
        
        SetPrivateField(manager, "canvasGroup", canvasGroup);
        SetPrivateField(manager, "fadePanel", panel);
        SetPrivateField(manager, "loadingText", text);
        SetPrivateField(manager, "progressBar", slider);
        SetPrivateField(manager, "progressBarObject", progressContainer);
        SetPrivateField(manager, "fadeInDuration", 0.5f);
        SetPrivateField(manager, "fadeOutDuration", 0.5f);
        SetPrivateField(manager, "minimumDisplayTime", 0.5f);
        
        // Set loading messages array
        string[] messages = new string[]
        {
            "Loading...",
            "Preparing the dungeon...",
            "Summoning enemies...",
            "Lighting torches...",
            "Polishing treasures...",
            "Sharpening swords...",
            "Brewing potions..."
        };
        SetPrivateField(manager, "loadingMessages", messages);

        // Mark as modified
        EditorUtility.SetDirty(canvasObj);
        
        // Select the created canvas
        Selection.activeGameObject = canvasObj;

        Debug.Log("<color=green>✓ Loading Screen Canvas created successfully!</color>");
        EditorUtility.DisplayDialog(
            "Success!",
            "Loading Screen Canvas has been created.\n\n" +
            "Next steps:\n" +
            "1. Add this canvas to GameManager's Persistent Objects array\n" +
            "2. Customize colors and messages in Inspector\n" +
            "3. Test the transitions!",
            "OK");
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"Could not find field: {fieldName}");
        }
    }
}
#endif
