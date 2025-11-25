using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flash : MonoBehaviour
{
    [Header("Material Flash (Optional)")] 
    [SerializeField] private Material whiteFlashMat; // Optional: can be null
    [SerializeField] private Material defaultMat;    // Original material (assign in Inspector if using material swapping)
    [SerializeField] private float restoreDefaultMatTime = .2f;

    [Header("Color Flash Fallback")] 
    [SerializeField] private Color flashColor = Color.white; // Used if material not valid
    [SerializeField] private bool forceColorFlash = false;   // Force using color-based flash even if materials exist

    private SpriteRenderer spriteRenderer;
    private MonoBehaviour healthComponent;
    private Color originalColor;

    private void Awake()
    {
        // Detect health component (extend as needed)
        healthComponent = GetComponent<SlimeHealth>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Subscribe to scene loaded event
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
        if (!spriteRenderer) return;

        if (CanUseMaterialFlash())
        {
            // Restore material
            if (defaultMat != null)
            {
                spriteRenderer.material = defaultMat;
            }
        }
        else
        {
            // Restore color
            spriteRenderer.color = originalColor;
        }
    }

    private bool CanUseMaterialFlash()
    {
        if (forceColorFlash) return false;
        if (!spriteRenderer) return false;
        if (whiteFlashMat == null || defaultMat == null) return false;
        if (whiteFlashMat.shader == null || defaultMat.shader == null) return false;
        // Unity shows pink (error) when shader unsupported; guard against that
        if (!whiteFlashMat.shader.isSupported || !defaultMat.shader.isSupported) return false;
        return true;
    }

    public IEnumerator FlashRoutine()
    {
        if (!spriteRenderer)
            yield break;

        if (CanUseMaterialFlash())
        {
            spriteRenderer.material = whiteFlashMat;
            yield return new WaitForSeconds(restoreDefaultMatTime);
            spriteRenderer.material = defaultMat;
        }
        else
        {
            // Color flash fallback (no shader swap, avoids pink error material)
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(restoreDefaultMatTime);
            spriteRenderer.color = originalColor;
        }

        // Death detection (extend for other health types as needed)
        if (healthComponent is SlimeHealth slimeHealth)
        {
            slimeHealth.DetectDeath();
        }
    }

    public void ResetMaterial()
    {
        StopAllCoroutines();
        ResetVisualState();
    }
}
