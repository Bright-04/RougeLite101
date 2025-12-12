using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a healthbar above an enemy that follows the enemy's position
/// and updates based on health changes.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject healthBarContainer;
    
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private bool alwaysFaceCamera = true;

    private Transform enemyTransform;
    private Camera mainCamera;
    private float maxHealth;
    private float currentHealth;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        if (hideWhenFull && healthBarContainer != null)
        {
            healthBarContainer.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (enemyTransform == null) return;

        // Position the healthbar above the enemy
        transform.position = enemyTransform.position + offset;

        // Make healthbar face the camera
        if (alwaysFaceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    /// <summary>
    /// Initialize the healthbar with the enemy's transform and max health
    /// </summary>
    public void Initialize(Transform enemy, float maxHP)
    {
        enemyTransform = enemy;
        maxHealth = maxHP;
        currentHealth = maxHP;
        UpdateHealthBar();
    }

    /// <summary>
    /// Update the healthbar display based on current health
    /// </summary>
    public void UpdateHealth(float health)
    {
        currentHealth = health;
        UpdateHealthBar();

        // Show healthbar when damaged
        if (hideWhenFull && healthBarContainer != null)
        {
            if (currentHealth < maxHealth)
            {
                healthBarContainer.SetActive(true);
            }
            else
            {
                healthBarContainer.SetActive(false);
            }
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null && maxHealth > 0)
        {
            float fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
            healthBarFill.fillAmount = fillAmount;

            // Optional: Change color based on health percentage
            if (fillAmount > 0.5f)
            {
                healthBarFill.color = Color.green;
            }
            else if (fillAmount > 0.25f)
            {
                healthBarFill.color = Color.yellow;
            }
            else
            {
                healthBarFill.color = Color.red;
            }
        }
    }
}
