using UnityEngine;
using RougeLite.Director;

public enum IntensityBand
{
    Low,
    Mid,
    High
}

public class AIDirector : MonoBehaviour
{
    public static AIDirector Instance { get; private set; }

    [Header("Intensity Scores")]
    [SerializeField] private float currentIntensity = 0f;
    [SerializeField] private IntensityBand currentBand = IntensityBand.Low;

    [Header("Hysteresis Thresholds")]
    [SerializeField] private float lowToMidThreshold = 35f;
    [SerializeField] private float midToLowThreshold = 25f;
    [SerializeField] private float midToHighThreshold = 75f;
    [SerializeField] private float highToMidThreshold = 65f;

    [Header("Decay settings")]
    [SerializeField] private float intensityDecayRate = 2f; // Intensity decays over time if nothing happens

    private RuntimeCombatData combatData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        combatData = new RuntimeCombatData();
    }

    private void Update()
    {
        // 1. Decay intensity naturally
        if (currentIntensity > 0)
        {
            currentIntensity -= intensityDecayRate * Time.deltaTime;
            currentIntensity = Mathf.Max(0, currentIntensity);
        }

        // 2. Evaluate Band Shifts
        EvaluateBands();
    }

    /// <summary>
    /// Call this from damage source triggers or player health scripts
    /// </summary>
    public void AddIntensity(float amount)
    {
        currentIntensity += amount;
        currentIntensity = Mathf.Clamp(currentIntensity, 0f, 100f);
    }

    private void EvaluateBands()
    {
        switch (currentBand)
        {
            case IntensityBand.Low:
                if (currentIntensity >= lowToMidThreshold)
                {
                    ChangeBand(IntensityBand.Mid);
                }
                break;

            case IntensityBand.Mid:
                if (currentIntensity <= midToLowThreshold)
                {
                    ChangeBand(IntensityBand.Low);
                }
                else if (currentIntensity >= midToHighThreshold)
                {
                    ChangeBand(IntensityBand.High);
                }
                break;

            case IntensityBand.High:
                if (currentIntensity <= highToMidThreshold)
                {
                    ChangeBand(IntensityBand.Mid);
                }
                break;
        }
    }

    private void ChangeBand(IntensityBand newBand)
    {
        if (currentBand == newBand) return;

        currentBand = newBand;
        Debug.Log($"<color=orange>[AI Director]</color> Intensity Band shifted to: {currentBand}");
        
        // You would fire an event here for the spawner system to change behavior
    }

    // --- Hooks for Runtime Combat Data Updates ---
    
    public void RecordDamageTaken(float damage)
    {
        combatData.damageTakenInLastXSeconds += damage;
        AddIntensity(damage * 1.5f); // taking damage causes huge intensity spikes
    }

    public void RecordDamageDealt(float damage)
    {
        combatData.damageDealtInLastXSeconds += damage;
        AddIntensity(damage * 0.5f); // dealing damage increases intensity slightly (action)
    }

    public void RecordEnemyKill()
    {
        combatData.enemiesKilledRecently += 1;
        AddIntensity(5f); // flat intensity bump per kill
    }

    // Basic GUI for debugging
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 300, 30), $"AI Intensity: {currentIntensity:F1}", style);
        GUI.Label(new Rect(10, 40, 300, 30), $"Band: {currentBand}", style);
    }
}
