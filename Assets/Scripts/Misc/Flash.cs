using System.Collections;
using UnityEngine;
using RougeLite.Enemies;

namespace RougeLite.Misc
{
public class Flash : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    [SerializeField] private float restoreDefaultMatTime = .2f;

    private Material defaultMat;
    private SpriteRenderer spriteRenderer;
    private SlimeHealth health;

    private void Awake()
    {
        health = GetComponent<SlimeHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMat = spriteRenderer.material;

    }

    public IEnumerator FlashRoutine()
    {
        spriteRenderer.material = whiteFlashMat;
        yield return new WaitForSeconds(restoreDefaultMatTime);
        spriteRenderer.material = defaultMat;
        health.DetectDeath();
    }
}
}
