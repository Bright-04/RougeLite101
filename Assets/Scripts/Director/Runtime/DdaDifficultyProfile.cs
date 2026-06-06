using System;
using UnityEngine;

public enum DdaProfileType
{
    Assist,
    Balanced,
    Challenge
}

/// <summary>
/// Bounded rule-based combat-pressure profile selected by the AI Director.
/// This is explainable DDA tuning, not machine learning.
/// </summary>
[Serializable]
public sealed class DdaDifficultyProfile
{
    public const float MinMultiplier = 0.80f;
    public const float MaxMultiplier = 1.25f;

    public DdaProfileType profileType = DdaProfileType.Balanced;
    public string profileName = nameof(DdaProfileType.Balanced);
    public string designIntent = "Preserve default gameplay pressure.";
    public string reason = "Default profile.";
    public float sourceScore;

    public float chaseSpeedMultiplier = 1f;
    public float attackCooldownMultiplier = 1f;
    public float recoveryTimeMultiplier = 1f;
    public float detectionRangeMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float damageCooldownMultiplier = 1f;

    public DdaDifficultyProfile()
    {
    }

    public DdaDifficultyProfile(
        DdaProfileType profileType,
        float chaseSpeedMultiplier,
        float attackCooldownMultiplier,
        float recoveryTimeMultiplier,
        float detectionRangeMultiplier,
        float damageMultiplier,
        float damageCooldownMultiplier,
        string reason = null,
        float sourceScore = 0f)
    {
        this.profileType = profileType;
        profileName = profileType.ToString();
        designIntent = GetDesignIntent(profileType);
        this.reason = string.IsNullOrWhiteSpace(reason) ? GetDefaultReason(profileType) : reason;
        this.sourceScore = sourceScore;
        this.chaseSpeedMultiplier = ClampChaseSpeed(chaseSpeedMultiplier);
        this.attackCooldownMultiplier = ClampAttackCooldown(attackCooldownMultiplier);
        this.recoveryTimeMultiplier = ClampRecoveryTime(recoveryTimeMultiplier);
        this.detectionRangeMultiplier = ClampDetectionRange(detectionRangeMultiplier);
        this.damageMultiplier = ClampDamage(damageMultiplier);
        this.damageCooldownMultiplier = ClampDamageCooldown(damageCooldownMultiplier);
    }

    public static DdaDifficultyProfile Assist(string reason = null, float sourceScore = 0f)
    {
        return new DdaDifficultyProfile(
            DdaProfileType.Assist,
            0.90f,
            1.25f,
            1.20f,
            0.90f,
            0.95f,
            1.20f,
            reason,
            sourceScore);
    }

    public static DdaDifficultyProfile Balanced(string reason = null, float sourceScore = 0f)
    {
        return new DdaDifficultyProfile(
            DdaProfileType.Balanced,
            1.00f,
            1.00f,
            1.00f,
            1.00f,
            1.00f,
            1.00f,
            reason,
            sourceScore);
    }

    public static DdaDifficultyProfile Challenge(string reason = null, float sourceScore = 0f)
    {
        return new DdaDifficultyProfile(
            DdaProfileType.Challenge,
            1.08f,
            0.85f,
            0.85f,
            1.15f,
            1.05f,
            0.90f,
            reason,
            sourceScore);
    }

    public static float ClampMultiplier(float value)
    {
        return Mathf.Clamp(value, MinMultiplier, MaxMultiplier);
    }

    public static float ClampChaseSpeed(float value)
    {
        return Mathf.Clamp(ClampMultiplier(value), 0.90f, 1.12f);
    }

    public static float ClampAttackCooldown(float value)
    {
        return Mathf.Clamp(ClampMultiplier(value), 0.85f, 1.25f);
    }

    public static float ClampRecoveryTime(float value)
    {
        return Mathf.Clamp(ClampMultiplier(value), 0.85f, 1.25f);
    }

    public static float ClampDetectionRange(float value)
    {
        return Mathf.Clamp(ClampMultiplier(value), 0.90f, 1.15f);
    }

    public static float ClampDamage(float value)
    {
        return Mathf.Clamp(ClampMultiplier(value), 0.95f, 1.05f);
    }

    public static float ClampDamageCooldown(float value)
    {
        return Mathf.Clamp(ClampMultiplier(value), 0.90f, 1.20f);
    }

    public DdaDifficultyProfile ClampedCopy()
    {
        return new DdaDifficultyProfile(
            profileType,
            chaseSpeedMultiplier,
            attackCooldownMultiplier,
            recoveryTimeMultiplier,
            detectionRangeMultiplier,
            damageMultiplier,
            damageCooldownMultiplier,
            reason,
            sourceScore);
    }

    public string ToDebugString()
    {
        return
            $"DDA Profile: {profileName}\n" +
            $"Design Intent: {designIntent}\n" +
            $"Performance Score: {sourceScore:0.##}\n" +
            $"Reason: {reason}\n" +
            "Applied Enemy Tuning:\n" +
            $"Chase Speed x{chaseSpeedMultiplier:0.##}\n" +
            $"Attack Cooldown x{attackCooldownMultiplier:0.##}\n" +
            $"Recovery Time x{recoveryTimeMultiplier:0.##}\n" +
            $"Detection Range x{detectionRangeMultiplier:0.##}\n" +
            $"Damage x{damageMultiplier:0.##}\n" +
            $"Damage Cooldown x{damageCooldownMultiplier:0.##}";
    }

    private static string GetDesignIntent(DdaProfileType profileType)
    {
        switch (profileType)
        {
            case DdaProfileType.Assist:
                return "Reduce combat pressure when the player is struggling.";
            case DdaProfileType.Challenge:
                return "Increase combat pressure when the player is performing well.";
            case DdaProfileType.Balanced:
            default:
                return "Preserve default gameplay pressure.";
        }
    }

    private static string GetDefaultReason(DdaProfileType profileType)
    {
        switch (profileType)
        {
            case DdaProfileType.Assist:
                return "Player performance indicates high pressure.";
            case DdaProfileType.Challenge:
                return "Player performance indicates low pressure.";
            case DdaProfileType.Balanced:
            default:
                return "Player performance is within the target band.";
        }
    }
}
