using UnityEngine;

public class SlashAnim : MonoBehaviour
{
    private SlashEffectPool pool;

    public void Initialize(SlashEffectPool owner)
    {
        pool = owner;
    }

    private void OnEnable()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }
    }

    public void DestroySelf()
    {
        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
