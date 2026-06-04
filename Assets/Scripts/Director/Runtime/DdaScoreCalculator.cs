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
    public readonly struct ScoreBreakdown
    {
        public readonly float hpScore;
        public readonly float damageScore;
        public readonly float hitsScore;
        public readonly float clearTimeScore;
        public readonly float killScore;
        public readonly float finalScore;
        public readonly DdaDifficultyTier tier;

        public ScoreBreakdown(
            float hpScore,
            float damageScore,
            float hitsScore,
            float clearTimeScore,
            float killScore,
            float finalScore,
            DdaDifficultyTier tier)
        {
            this.hpScore = hpScore;
            this.damageScore = damageScore;
            this.hitsScore = hitsScore;
            this.clearTimeScore = clearTimeScore;
            this.killScore = killScore;
            this.finalScore = finalScore;
            this.tier = tier;
        }
    }

    [Header("Weights")]
    [SerializeField] private float hpWeight = 0.25f;
    [SerializeField] private float damageTakenWeight = 0.30f;
    [SerializeField] private float hitsTakenWeight = 0.25f;
    [SerializeField] private float clearTimeWeight = 0.15f;
    [SerializeField] private float killWeight = 0.05f;

    [Header("Clear Time Benchmarks")]
    [SerializeField] private float normalRoomFastClearSeconds = 20f;
    [SerializeField] private float normalRoomSlowClearSeconds = 75f;
    [SerializeField] private float bossRoomFastClearSeconds = 45f;
    [SerializeField] private float bossRoomSlowClearSeconds = 150f;

    [Header("Kill Benchmarks")]
    [SerializeField] private float normalRoomExpectedKills = 4f;

    [Header("Hits Taken Benchmarks")]
    [SerializeField] private float normalRoomExpectedHitsTaken = 6f;
    [SerializeField] private float bossRoomExpectedHitsTaken = 10f;

    public float Calculate(DdaTelemetryService.RoomTelemetrySample sample)
    {
        return CalculateBreakdown(sample).finalScore;
    }

    public ScoreBreakdown CalculateBreakdown(DdaTelemetryService.RoomTelemetrySample sample)
    {
        if (sample == null)
        {
            return new ScoreBreakdown(0f, 0f, 0f, 0f, 0f, 0f, DdaDifficultyTier.Recovery);
        }

        float maxHp = Mathf.Max(1f, sample.playerMaxHp);
        float hpRatio = Mathf.Clamp01(sample.playerHpEnd / maxHp);
        float damageTakenScore = 1f - Mathf.Clamp01(sample.damageTaken / maxHp);
        float hitsTakenScore = CalculateHitsTakenScore(sample.hitsTaken, sample.isBossRoom);
        float clearTimeScore = CalculateClearTimeScore(sample.clearTime, sample.isBossRoom);
        float killScore = CalculateKillScore(sample.enemiesKilled, sample.isBossRoom);

        float weightedScore =
            (hpRatio * hpWeight) +
            (damageTakenScore * damageTakenWeight) +
            (hitsTakenScore * hitsTakenWeight) +
            (clearTimeScore * clearTimeWeight) +
            (killScore * killWeight);

        float finalScore = Mathf.Clamp(weightedScore * 100f, 0f, 100f);
        DdaDifficultyTier tier = GetTier(finalScore);

        return new ScoreBreakdown(
            hpRatio,
            damageTakenScore,
            hitsTakenScore,
            clearTimeScore,
            killScore,
            finalScore,
            tier);
    }

    public DdaDifficultyTier GetTier(float score)
    {
        if (score <= 24f) return DdaDifficultyTier.Recovery;
        if (score <= 49f) return DdaDifficultyTier.Easy;
        if (score <= 84f) return DdaDifficultyTier.Normal;
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

    private float CalculateHitsTakenScore(int hitsTaken, bool isBossRoom)
    {
        float expectedHits = isBossRoom ? bossRoomExpectedHitsTaken : normalRoomExpectedHitsTaken;
        return 1f - Mathf.Clamp01(hitsTaken / Mathf.Max(1f, expectedHits));
    }
}
