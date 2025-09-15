using UnityEngine;
using UnityEngine.UI;
using RougeLite.Events;

/// <summary>
/// Floating damage number that appears when damage is dealt
/// Listens to damage events and creates visual feedback
/// </summary>
public class DamageNumber : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Text damageText;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private Color normalDamageColor = Color.red;
    [SerializeField] private Color criticalDamageColor = Color.yellow;
    [SerializeField] private Color healColor = Color.green;
    
    private float timer;
    private Vector3 startPosition;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        startPosition = transform.position;
        timer = lifetime;
    }

    private void Update()
    {
        // Move upward
        transform.position = startPosition + Vector3.up * (floatSpeed * (lifetime - timer));
        
        // Fade out
        canvasGroup.alpha = timer / lifetime;
        
        timer -= Time.deltaTime;
        
        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Setup the damage number with damage amount and type
    /// </summary>
    public void Setup(float damage, bool isCritical = false, bool isHeal = false)
    {
        if (damageText != null)
        {
            damageText.text = Mathf.RoundToInt(damage).ToString();
            
            if (isHeal)
            {
                damageText.color = healColor;
                damageText.text = "+" + damageText.text;
            }
            else if (isCritical)
            {
                damageText.color = criticalDamageColor;
                damageText.fontSize = (int)(damageText.fontSize * 1.3f);
                damageText.text += "!";
            }
            else
            {
                damageText.color = normalDamageColor;
            }
        }
    }
}