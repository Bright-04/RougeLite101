using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flash : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    [SerializeField] private Material defaultMat;
    [SerializeField] private float restoreDefaultMatTime = .2f;

    private SpriteRenderer spriteRenderer;
    private MonoBehaviour healthComponent;

    private void Awake()
    {
        // Try to find any health component that has a DetectDeath method
        healthComponent = GetComponent<SlimeHealth>();
        // Could be extended to search for other health components in the future
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnEnable()
    {
        // Always reset to default material when enabled (including after scene loads)
        ResetToDefaultMaterial();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset material whenever a scene is loaded
        ResetToDefaultMaterial();
    }

    private void ResetToDefaultMaterial()
    {
        if (spriteRenderer != null && defaultMat != null)
        {
            spriteRenderer.material = defaultMat;
        }
    }

    public IEnumerator FlashRoutine()
    {
        spriteRenderer.material = whiteFlashMat;
        yield return new WaitForSeconds(restoreDefaultMatTime);       
        spriteRenderer.material = defaultMat;
        
        // Call DetectDeath if health component exists and has the method
        if (healthComponent is SlimeHealth slimeHealth)
        {
            slimeHealth.DetectDeath();
        }
        // Can be extended for other health component types
    }

    public void ResetMaterial()
    {
        // Stop any ongoing flash coroutines
        StopAllCoroutines();
        
        // Reset to default material
        if (spriteRenderer != null && defaultMat != null)
        {
            spriteRenderer.material = defaultMat;
        }
    }

}
