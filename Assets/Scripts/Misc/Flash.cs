using System.Collections;
using UnityEngine;

public class Flash : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    [SerializeField] private float restoreDefaultMatTime = .2f;

    private Material defaultMat;
    private SpriteRenderer spriteRenderer;
    private MonoBehaviour healthComponent;

    private void Awake()
    {
        // Try to find any health component that has a DetectDeath method
        healthComponent = GetComponent<SlimeHealth>();
        // Could be extended to search for other health components in the future
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMat = spriteRenderer.material;
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
}
