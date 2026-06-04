using UnityEngine;

public enum DdaDifficultyTier
{
    Recovery,
    Easy,
    Normal,
    Pressure
}

[System.Serializable]
public sealed class DdaScoreCalculator
{
    [Header("Weights")]
    [SerializeField] private float hpWeight = 0.45f;
    [SerializeField] private float damageTakenWeight = 0.25f;
    [SerializeField] private float clearTimeWeight = 0.20f;
    [SerializeField] private float killWeight = 0.10f;

    [Header("Clear Time Benchmarks")]
    [SerializeField] private float normalRoomFastClearSeconds = 20f;
    [SerializeField] private float normalRoomSlowClearSeconds = 75f;
    [SerializeField] private float bossRoomFastClearSeconds = 45f;
    [SerializeField] private float bossRoomSlowClearSeconds = 150f;

    [Header("Kill Benchmarks")]
    [SerializeField] private float normalRoomExpectedKills = 4f;

    public float Calculate(DdaTelemetryService.RoomTelemetrySample sample)
    {
        if (sample == null)
        {
            return 0f;
        }

        float maxHp = Mathf.Max(1f, sample.playerMaxHp);
        float hpRatio = Mathf.Clamp01(sample.playerHpEnd / maxHp);
        float damageTakenScore = 1f - Mathf.Clamp01(sample.damageTaken / maxHp);
        float clearTimeScore = CalculateClearTimeScore(sample.clearTime, sample.isBossRoom);
        float killScore = CalculateKillScore(sample.enemiesKilled, sample.isBossRoom);

        float weightedScore =
            (hpRatio * hpWeight) +
            (damageTakenScore * damageTakenWeight) +
            (clearTimeScore * clearTimeWeight) +
            (killScore * killWeight);

        return Mathf.Clamp(weightedScore * 100f, 0f, 100f);
    }

    public DdaDifficultyTier GetTier(float score)
    {
        if (score <= 24f) return DdaDifficultyTier.Recovery;
        if (score <= 49f) return DdaDifficultyTier.Easy;
        if (score <= 74f) return DdaDifficultyTier.Normal;
        return DdaDifficultyTier.Pressure;
    }

    private float CalculateClearTimeScore(float clearTime, bool isBossRoom)
    {
        float fast = isBossRoom ? bossRoomFastClearSeconds : normalRoomFastClearSeconds;
        float slow = isBossRoom ? bossRoomSlowClearSeconds : normalRoomSlowClearSeconds;

        if (clearTime <= fast)
        {
            return 1f;
        }

        if (clearTime >= slow)
        {
            return 0f;
        }

        return 1f - ((clearTime - fast) / Mathf.Max(0.01f, slow - fast));
    }

    private float CalculateKillScore(int enemiesKilled, bool isBossRoom)
    {
        if (isBossRoom)
        {
            return enemiesKilled > 0 ? 1f : 0f;
        }

        return Mathf.Clamp01(enemiesKilled / Mathf.Max(1f, normalRoomExpectedKills));
    }
}
