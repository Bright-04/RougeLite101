using System.Collections;
using UnityEngine;
using RougeLite.Combat.Damage;

[DisallowMultipleComponent]
public sealed class ScarecrowTestDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 9999;
    [SerializeField] private bool resetHealthWhenDepleted = true;
    [SerializeField] private bool logHits = false;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = new Color(1f, 0.65f, 0.65f, 1f);

    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private bool hasCapturedOriginalColor;
    private Coroutine flashRoutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && !hasCapturedOriginalColor)
        {
            originalColor = spriteRenderer.color;
            hasCapturedOriginalColor = true;
        }

        ResetHealth();
    }

    private void OnEnable()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        ResetHealth();
    }

    private void OnValidate()
    {
        if (maxHealth < 1)
        {
            maxHealth = 1;
        }

        if (flashDuration < 0f)
        {
            flashDuration = 0f;
        }
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (logHits)
        {
            Debug.Log($"[ScarecrowTestDummy] Took {damage} damage. {currentHealth}/{maxHealth} HP remaining.", this);
        }

        if (spriteRenderer != null)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (currentHealth > 0 || !resetHealthWhenDepleted)
        {
            return;
        }

        ResetHealth();
    }

    private void ResetHealth()
    {
        currentHealth = maxHealth;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
        {
            flashRoutine = null;
            yield break;
        }

        spriteRenderer.color = flashColor;

        if (flashDuration > 0f)
        {
            yield return new WaitForSeconds(flashDuration);
        }

        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }
}
