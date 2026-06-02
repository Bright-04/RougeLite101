using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flash : MonoBehaviour
{
    [Header("Material Flash (Optional)")]
    [SerializeField] private Material whiteFlashMat;
    [SerializeField] private Material defaultMat;
    [SerializeField] private float restoreDefaultMatTime = .2f;

    [Header("Color Flash Fallback")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private bool forceColorFlash = false;
    [SerializeField] private SpriteRenderer targetRenderer;

    private SpriteRenderer spriteRenderer;
    private MonoBehaviour healthComponent;
    private Color originalColor;

    public event Action StartedFlashing;
    public event Action FinishedFlashing;

    private void Awake()
    {
        healthComponent = GetComponent<SlimeHealth>();
        spriteRenderer = targetRenderer != null ? targetRenderer : GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnEnable()
    {
        ResetVisualState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetVisualState();
    }

    private void ResetVisualState()
    {
        if (!spriteRenderer)
        {
            return;
        }

        if (CanUseMaterialFlash())
        {
            if (defaultMat != null)
            {
                spriteRenderer.material = defaultMat;
            }
        }
        else
        {
            spriteRenderer.color = originalColor;
        }
    }

    private bool CanUseMaterialFlash()
    {
        if (forceColorFlash || !spriteRenderer)
        {
            return false;
        }

        if (whiteFlashMat == null || defaultMat == null)
        {
            return false;
        }

        if (whiteFlashMat.shader == null || defaultMat.shader == null)
        {
            return false;
        }

        return whiteFlashMat.shader.isSupported && defaultMat.shader.isSupported;
    }

    public IEnumerator FlashRoutine()
    {
        if (!spriteRenderer)
        {
            yield break;
        }

        StartedFlashing?.Invoke();

        if (CanUseMaterialFlash())
        {
            spriteRenderer.material = whiteFlashMat;
            yield return new WaitForSeconds(restoreDefaultMatTime);
            spriteRenderer.material = defaultMat;
        }
        else
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(restoreDefaultMatTime);
            spriteRenderer.color = originalColor;
        }

        if (healthComponent is SlimeHealth slimeHealth)
        {
            slimeHealth.DetectDeath();
        }

        FinishedFlashing?.Invoke();
    }

    public void ResetMaterial()
    {
        StopAllCoroutines();
        ResetVisualState();
    }
}
