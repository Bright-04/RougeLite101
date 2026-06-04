using UnityEngine;

[CreateAssetMenu(fileName = "NewSin", menuName = "Roguelite/Sin Data")]
public class SinData : ScriptableObject
{
    public string sinName;
    [TextArea] public string description;

    [Header("Stat Modifiers (Debuffs)")]
    [Tooltip("Percentage extra damage taken (0.2 = 20% extra damage)")]
    public float damageTakenMultiplier = 0f;
    [Tooltip("Percentage to reduce mana regen by (0.5 = 50% slower)")]
    public float manaRegenPenaltyMultiplier = 0f;
    [Tooltip("Increase dash cooldown by this flat amount (seconds)")]
    public float dashCooldownIncrease = 0f;
    [Tooltip("Reduce healing effectiveness by this percentage (0.3 = 30% less healing)")]
    public float healingReductionMultiplier = 0f;

    // You can add more modular effects here later using an Event/Action system
}
