using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{

    public static BossHealthBar Instance { get; private set; }
    [Header("UI References")]
    [SerializeField] private Slider bossHealthSlider;   
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text bossNameText;

    private float currentHealth;
    private float maxHealth;
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Ẩn thanh máu ban đầu
        gameObject.SetActive(false);

    }

    
    public void ShowHealthBar()
    {
        gameObject.SetActive(true);
    }

    public void HideHealthBar()
    {
        gameObject.SetActive(false);
    }

    public void Initialize(float health, string bossName)
    {
        maxHealth = health;
        currentHealth = health;
        bossNameText.text = bossName;

        bossHealthSlider.maxValue = health;
        bossHealthSlider.value = health;

        UpdateHealthUI(0);
        ShowHealthBar();
    }

    public void UpdateHealthUI(float damage)
    {
        currentHealth -= damage;
        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = currentHealth;
        }
        
        if (hpText != null)
        {
            hpText.text = $"{Mathf.FloorToInt(currentHealth)} / {maxHealth}";
        }

        
    }
}
