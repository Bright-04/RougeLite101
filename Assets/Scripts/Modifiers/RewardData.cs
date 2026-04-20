using UnityEngine;

[CreateAssetMenu(fileName = "NewReward", menuName = "Roguelite/Reward Data")]
public class RewardData : ScriptableObject
{
    public string rewardName;
    [TextArea] public string description;

    [Header("Stat Modifiers (Buffs)")]
    [Tooltip("Reduce attack cooldown by flat amount (seconds)")]
    public float attackSpeedBoost = 0f;
    [Tooltip("Flat mana regen per second increase")]
    public float flatManaRegenIncrease = 0f;
    [Tooltip("Flat defense increase (reduces incoming damage)")]
    public float flatDefenseIncrease = 0f;
    [Tooltip("Increase movement speed flat value")]
    public float moveSpeedIncrease = 0f;
    [Tooltip("Increase dash distance multiplier (1.2 = 20% further)")]
    public float dashDistanceMultiplier = 1f;
}
