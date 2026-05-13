using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CRTFilterOverlay : MonoBehaviour
{
    private const string PlayerPrefsKey = "CRTFilterEnabled";
    private const string ShaderName = "Hidden/RougeLite101/ReadableCRT";

    [Header("Toggle")]
    [SerializeField] private Key toggleKey = Key.F10;
    [SerializeField] private bool enabledByDefault = true;

    [Header("Render")]
    [SerializeField] private int sortingOrder = -30000;
    [SerializeField] private float renderScale = 1f;

    [Header("Retro Post FX")]
    [ColorUsage(false)] [SerializeField] private Color warmTint = new Color(1.01f, 1.0f, 0.99f, 1f);
    [Range(0f, 1f)] [SerializeField] private float warmStrength = 0.06f;
    [SerializeField] private Vector2 curvature = new Vector2(8f, 8f);
    [Range(0f, 1f)] [SerializeField] private float shadowMaskStrength = 0.1f;
    [Range(0.5f, 3f)] [SerializeField] private float maskScale = 1f;
    [Range(0f, 0.2f)] [SerializeField] private float scanlineStrength = 0.11f;
    [Range(0f, 0.2f)] [SerializeField] private float blurBlend = 0.09f;
    [Range(0f, 1f)] [SerializeField] private float glowStrength = 0.3f;
    [Range(0.5f, 1f)] [SerializeField] private float bloomThreshold = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float bloomScatter = 0.45f;
    [Range(0f, 0.2f)] [SerializeField] private float colorBleedStrength = 0.07f;
    [Range(0f, 1f)] [SerializeField] private float phosphorMaskStrength = 0.12f;
    [Range(0f, 0.03f)] [SerializeField] private float chromaticAberration = 0.008f;
    [Range(0f, 0.08f)] [SerializeField] private float noiseStrength = 0.012f;
    [Range(0f, 0.25f)] [SerializeField] private float fadeAmount = 0.06f;
    [Range(0.8f, 1.3f)] [SerializeField] private float saturation = 1.05f;
    [Range(0.8f, 1.5f)] [SerializeField] private float contrast = 1.03f;
    [Range(0f, 0.3f)] [SerializeField] private float vignetteStrength = 0.06f;

    private Camera targetCamera;
    private RenderTexture renderTexture;
    private RenderTexture previousTargetTexture;
    private Canvas canvas;
    private Camera outputCamera;
    private RawImage outputImage;
    private Material crtMaterial;
    private bool isEnabled;
    private bool hasWarnedMissingCamera;
    private int textureWidth;
    private int textureHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        if (FindFirstObjectByType<CRTFilterOverlay>() != null)
        {
            return;
        }

        GameObject instance = new GameObject("CRTFilterOverlay");
        DontDestroyOnLoad(instance);
        instance.AddComponent<CRTFilterOverlay>();
    }

    private void Awake()
    {
        isEnabled = PlayerPrefs.GetInt(PlayerPrefsKey, enabledByDefault ? 1 : 0) == 1;
        BuildOutputCanvas();
        BuildMaterial();
        ApplyEnabledState();
    }

    private void LateUpdate()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            SetEnabled(!isEnabled);
        }

        if (!isEnabled)
        {
            return;
        }

        EnsureCamera();
        EnsureRenderTexture();
        UpdateMaterialProperties();
    }

    private void OnDisable()
    {
        RestoreCameraTarget();
    }

    private void OnDestroy()
    {
        RestoreCameraTarget();
        ReleaseRenderTexture();

        if (crtMaterial != null)
        {
            Destroy(crtMaterial);
        }
    }

    public void SetEnabled(bool value)
    {
        isEnabled = value;
        PlayerPrefs.SetInt(PlayerPrefsKey, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyEnabledState();
    }

    public void Toggle()
    {
        SetEnabled(!isEnabled);
    }

    private void BuildOutputCanvas()
    {
        BuildOutputCamera();

        GameObject canvasObject = new GameObject("CRT Output Canvas");
        canvasObject.transform.SetParent(transform, false);
        SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = outputCamera;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("CRT Output");
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        StretchToParent(rect);

        outputImage = imageObject.AddComponent<RawImage>();
        outputImage.raycastTarget = false;
    }

    private void BuildOutputCamera()
    {
        GameObject cameraObject = new GameObject("CRT Output Camera");
        cameraObject.transform.SetParent(transform, false);

        outputCamera = cameraObject.AddComponent<Camera>();
        outputCamera.depth = 1000f;
        outputCamera.clearFlags = CameraClearFlags.Depth;
        outputCamera.backgroundColor = Color.clear;
        outputCamera.cullingMask = LayerMask.GetMask("UI");
        outputCamera.orthographic = true;
        outputCamera.nearClipPlane = 0.01f;
        outputCamera.farClipPlane = 10f;
    }

    private void BuildMaterial()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"CRTFilterOverlay: Shader '{ShaderName}' was not found. CRT filter disabled.", this);
            isEnabled = false;
            return;
        }

        crtMaterial = new Material(shader)
        {
            name = "Runtime_ReadableCRT"
        };

        outputImage.material = crtMaterial;
    }

    private void ApplyEnabledState()
    {
        bool canRender = isEnabled && crtMaterial != null;

        if (canvas != null)
        {
            canvas.enabled = canRender;
        }

        if (outputCamera != null)
        {
            outputCamera.enabled = canRender;
        }

        if (canRender)
        {
            EnsureCamera();
            EnsureRenderTexture();
            UpdateMaterialProperties();
        }
        else
        {
            RestoreCameraTarget();
        }
    }

    private void EnsureCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        if (mainCamera == null)
        {
            if (!hasWarnedMissingCamera)
            {
                Debug.LogWarning("CRTFilterOverlay: No active camera was found. CRT output will wait until a camera exists.", this);
                hasWarnedMissingCamera = true;
            }

            return;
        }

        hasWarnedMissingCamera = false;

        if (targetCamera == mainCamera)
        {
            return;
        }

        RestoreCameraTarget();
        targetCamera = mainCamera;
        previousTargetTexture = targetCamera.targetTexture;
    }

    private void EnsureRenderTexture()
    {
        if (targetCamera == null || outputImage == null)
        {
            return;
        }

        int width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * Mathf.Max(0.1f, renderScale)));
        int height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * Mathf.Max(0.1f, renderScale)));

        if (renderTexture == null || textureWidth != width || textureHeight != height)
        {
            ReleaseRenderTexture();

            textureWidth = width;
            textureHeight = height;

            GraphicsFormat depthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt;
            if (!SystemInfo.IsFormatSupported(depthStencilFormat, FormatUsage.Render))
            {
                depthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
                if (!SystemInfo.IsFormatSupported(depthStencilFormat, FormatUsage.Render))
                {
                    depthStencilFormat = GraphicsFormat.D16_UNorm;
                }
            }

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(textureWidth, textureHeight)
            {
                graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR),
                depthStencilFormat = depthStencilFormat,
                msaaSamples = 1,
                volumeDepth = 1,
                mipCount = 1,
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
            };

            renderTexture = new RenderTexture(descriptor)
            {
                name = "CRT_GameView_RT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };

            renderTexture.Create();
            outputImage.texture = renderTexture;
        }

        if (targetCamera.targetTexture != renderTexture)
        {
            targetCamera.targetTexture = renderTexture;
        }
    }

    private void RestoreCameraTarget()
    {
        if (targetCamera != null && targetCamera.targetTexture == renderTexture)
        {
            targetCamera.targetTexture = previousTargetTexture;
        }
    }

    private void ReleaseRenderTexture()
    {
        if (renderTexture == null)
        {
            return;
        }

        if (outputImage != null && outputImage.texture == renderTexture)
        {
            outputImage.texture = null;
        }

        renderTexture.Release();
        Destroy(renderTexture);
        renderTexture = null;
    }

    private void UpdateMaterialProperties()
    {
        if (crtMaterial == null)
        {
            return;
        }

        crtMaterial.SetFloat("_ScanlineStrength", scanlineStrength);
        crtMaterial.SetFloat("_BlurBlend", blurBlend);
        crtMaterial.SetFloat("_GlowStrength", glowStrength);
        crtMaterial.SetFloat("_BloomThreshold", bloomThreshold);
        crtMaterial.SetFloat("_BloomScatter", bloomScatter);
        crtMaterial.SetVector("_Curvature", new Vector4(Mathf.Max(0.1f, curvature.x), Mathf.Max(0.1f, curvature.y), 0f, 0f));
        crtMaterial.SetFloat("_ShadowMaskStrength", shadowMaskStrength);
        crtMaterial.SetFloat("_MaskScale", maskScale);
        crtMaterial.SetFloat("_ColorBleedStrength", colorBleedStrength);
        crtMaterial.SetFloat("_PhosphorMaskStrength", phosphorMaskStrength);
        crtMaterial.SetFloat("_ChromaticStrength", chromaticAberration);
        crtMaterial.SetFloat("_NoiseStrength", noiseStrength);
        crtMaterial.SetFloat("_FadeAmount", fadeAmount);
        crtMaterial.SetFloat("_Saturation", saturation);
        crtMaterial.SetFloat("_Contrast", contrast);
        crtMaterial.SetFloat("_VignetteStrength", vignetteStrength);
        crtMaterial.SetColor("_WarmTint", warmTint);
        crtMaterial.SetFloat("_WarmStrength", warmStrength);
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        root.layer = layer;
        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
        }
    }
}
