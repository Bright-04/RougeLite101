using System.Collections;
using UnityEngine;

public class SlashAnim : MonoBehaviour
{
    private SlashEffectPool pool;
    private SpriteRenderer spriteRenderer;
    private Coroutine playbackRoutine;
    private Color baseColor = Color.white;
    private bool released;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }
    }

    public void Initialize(SlashEffectPool owner)
    {
        pool = owner;
    }

    private void OnEnable()
    {
        released = false;
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
            spriteRenderer.color = baseColor;
        }
    }

    public void Play(float lifetime, Vector3 startScale, Vector3 endScale, Quaternion rotation, bool fadeOut)
    {
        transform.rotation = rotation;
        transform.localScale = startScale;

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
        }

        playbackRoutine = StartCoroutine(PlayRoutine(
            Mathf.Max(0.01f, lifetime),
            startScale,
            endScale,
            fadeOut));
    }

    private IEnumerator PlayRoutine(float lifetime, Vector3 startScale, Vector3 endScale, bool fadeOut)
    {
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, EaseOutCubic(t));

            if (fadeOut && spriteRenderer != null)
            {
                Color color = baseColor;
                color.a = Mathf.Lerp(baseColor.a, 0f, EaseOutCubic(t));
                spriteRenderer.color = color;
            }

            yield return null;
        }

        transform.localScale = endScale;
        DestroySelf();
    }

    public void DestroySelf()
    {
        if (released)
        {
            return;
        }

        released = true;
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }
}
