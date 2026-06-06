using UnityEngine;

public sealed class DdaTelemetryService : MonoBehaviour
{
    [System.Serializable]
    public sealed class RoomTelemetrySample
    {
        public int roomInstanceId;
        public string roomName;
        public bool isBossRoom;
        public float startTime;
        public float clearTime;
        public float playerHpStart;
        public float playerHpEnd;
        public float playerMaxHp;
        public float damageTaken;
        public float damageDealt;
        public int enemiesKilled;
        public int hitsTaken;
        public float score;
        public DdaDifficultyTier tier;
    }

    private static DdaTelemetryService _instance;

    public static DdaTelemetryService Instance
    {
        get
        {
            if (!Application.isPlaying)
            {
                return null;
            }

            if (_instance == null)
            {
                var serviceObject = new GameObject("DdaTelemetryService");
                _instance = serviceObject.AddComponent<DdaTelemetryService>();
            }

            return _instance;
        }
    }

    [Header("Debug")]
    [SerializeField] private bool enableDdaDebug = true;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Spawn Scaling")]
    [SerializeField] private bool enableDdaSpawnScaling = true;
    [SerializeField] private int recoverySpawnDelta = -1;
    [SerializeField] private int easySpawnDelta = 0;
    [SerializeField] private int normalSpawnDelta = 0;
    [SerializeField] private int pressureSpawnDelta = 1;
    [SerializeField] private int maxExtraSpawns = 2;

    [Header("Scoring")]
    [SerializeField] private DdaScoreCalculator scoreCalculator = new DdaScoreCalculator();

    [Header("Behavior Profiles")]
    [SerializeField] private bool enableDdaBehaviorProfiles = true;
    [SerializeField] private DdaProfileType currentProfileType = DdaProfileType.Balanced;
    [SerializeField] private bool forceDebugProfile = false;
    [SerializeField] private DdaProfileType forcedProfileType = DdaProfileType.Balanced;

    [Header("Behavior Profile Thresholds")]
    [SerializeField] private float assistEnterScore = 45f;
    [SerializeField] private float assistExitScore = 55f;
    [SerializeField] private float challengeEnterScore = 86f;
    [SerializeField] private float challengeExitScore = 78f;
    [SerializeField] private float lowHpAssistRatio = 0.35f;
    [SerializeField] private float highDamageAssistRatio = 0.45f;
    [SerializeField] private float challengeMinHpRatio = 0.75f;
    [SerializeField] private float challengeMaxDamageRatio = 0.20f;
    [SerializeField] private float challengeMinClearTimeScore = 0.65f;

    [SerializeField] private RoomTelemetrySample activeRoom;
    [SerializeField] private RoomTelemetrySample lastCompletedRoom;
    [SerializeField] private DdaDifficultyProfile currentProfile = DdaDifficultyProfile.Balanced();

    public RoomTelemetrySample ActiveRoom => activeRoom;
    public RoomTelemetrySample LastCompletedRoom => lastCompletedRoom;
    public DdaDifficultyProfile CurrentProfile => currentProfile;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        currentProfile = GetProfile(currentProfileType);

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public void BeginRoom(RoomTemplate roomTemplate)
    {
        if (roomTemplate == null)
        {
            return;
        }

        int roomInstanceId = roomTemplate.GetInstanceID();
        if (activeRoom != null && activeRoom.roomInstanceId == roomInstanceId)
        {
            return;
        }

        if (activeRoom != null)
        {
            Log($"[DDA] RoomStart replacing unfinished room={activeRoom.roomName}");
        }

        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        float currentHp = playerStats != null ? playerStats.GetCurrentHP() : 0f;
        float maxHp = playerStats != null ? Mathf.Max(1f, playerStats.GetMaxHP()) : 1f;

        activeRoom = new RoomTelemetrySample
        {
            roomInstanceId = roomInstanceId,
            roomName = roomTemplate.gameObject.name,
            isBossRoom = roomTemplate.isBossRoom,
            startTime = Time.time,
            clearTime = 0f,
            playerHpStart = currentHp,
            playerHpEnd = currentHp,
            playerMaxHp = maxHp,
            damageTaken = 0f,
            damageDealt = 0f,
            enemiesKilled = 0,
            hitsTaken = 0,
            score = 0f,
            tier = DdaDifficultyTier.Normal
        };

        Log(
            $"[DDA] RoomStart room={activeRoom.roomName} boss={activeRoom.isBossRoom} " +
            $"hpStart={activeRoom.playerHpStart:0.##}/{activeRoom.playerMaxHp:0.##} " +
            $"profile={currentProfile.profileName}");
    }

    public void EndRoom(RoomTemplate roomTemplate)
    {
        if (roomTemplate == null || activeRoom == null)
        {
            return;
        }

        if (activeRoom.roomInstanceId != roomTemplate.GetInstanceID())
        {
            return;
        }

        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            activeRoom.playerHpEnd = playerStats.GetCurrentHP();
            activeRoom.playerMaxHp = Mathf.Max(1f, playerStats.GetMaxHP());
        }

        activeRoom.clearTime = Mathf.Max(0f, Time.time - activeRoom.startTime);
        DdaScoreCalculator.ScoreBreakdown breakdown = scoreCalculator.CalculateBreakdown(activeRoom);
        activeRoom.score = breakdown.finalScore;
        activeRoom.tier = breakdown.tier;
        lastCompletedRoom = activeRoom;
        DdaDifficultyProfile resolvedProfile = ResolveProfile(activeRoom, breakdown);
        SetCurrentProfile(resolvedProfile, activeRoom, breakdown);

        Log(
            $"[DDA] RoomClear room={activeRoom.roomName} boss={activeRoom.isBossRoom} " +
            $"clearTime={activeRoom.clearTime:0.##} hp={activeRoom.playerHpStart:0.##}->{activeRoom.playerHpEnd:0.##} " +
            $"dmgTaken={activeRoom.damageTaken:0.##} dmgDealt={activeRoom.damageDealt:0.##} " +
            $"kills={activeRoom.enemiesKilled} hitsTaken={activeRoom.hitsTaken}");
        Log(
            $"[DDA] ScoreBreakdown hp={breakdown.hpScore:0.##} damage={breakdown.damageScore:0.##} " +
            $"hits={breakdown.hitsScore:0.##} clearTime={breakdown.clearTimeScore:0.##} " +
            $"kills={breakdown.killScore:0.##} final={breakdown.finalScore:0.##} tier={breakdown.tier}");
        Log($"[DDA] Score={activeRoom.score:0.##} Tier={activeRoom.tier}");

        activeRoom = null;
    }

    public void RecordDamageTaken(float actualDamage)
    {
        if (activeRoom == null || actualDamage <= 0f)
        {
            return;
        }

        activeRoom.damageTaken += actualDamage;
        activeRoom.hitsTaken += 1;

        Log(
            $"[DDA] DamageTaken room={activeRoom.roomName} amount={actualDamage:0.##} " +
            $"total={activeRoom.damageTaken:0.##} hits={activeRoom.hitsTaken}");
    }

    public void RecordDamageDealt(float actualDamage, string sourceName = null)
    {
        if (activeRoom == null || actualDamage <= 0f)
        {
            return;
        }

        activeRoom.damageDealt += actualDamage;

        Log(
            $"[DDA] DamageDealt room={activeRoom.roomName} amount={actualDamage:0.##} " +
            $"total={activeRoom.damageDealt:0.##} source={sourceName ?? "unknown"}");
    }

    public void RecordEnemyKilled(string enemyName = null)
    {
        if (activeRoom == null)
        {
            return;
        }

        activeRoom.enemiesKilled += 1;

        Log(
            $"[DDA] EnemyKilled room={activeRoom.roomName} enemy={enemyName ?? "unknown"} " +
            $"kills={activeRoom.enemiesKilled}");
    }

    public DdaDifficultyTier GetCurrentTier()
    {
        if (activeRoom != null)
        {
            return activeRoom.tier;
        }

        if (lastCompletedRoom != null)
        {
            return lastCompletedRoom.tier;
        }

        return DdaDifficultyTier.Normal;
    }

    public DdaDifficultyProfile GetCurrentProfile()
    {
        if (!enableDdaBehaviorProfiles)
        {
            return DdaDifficultyProfile.Balanced();
        }

        if (forceDebugProfile)
        {
            currentProfile = GetProfile(forcedProfileType, "Debug forced profile.", currentProfile != null ? currentProfile.sourceScore : 0f);
            currentProfileType = currentProfile.profileType;
            return currentProfile;
        }

        if (currentProfile == null)
        {
            currentProfile = GetProfile(currentProfileType);
        }

        return currentProfile;
    }

    public void ForceCurrentProfile(DdaProfileType profileType)
    {
        forceDebugProfile = true;
        forcedProfileType = profileType;
        currentProfile = GetProfile(profileType, "Debug forced profile.", currentProfile != null ? currentProfile.sourceScore : 0f);
        currentProfileType = currentProfile.profileType;
        Log($"[DDA] ForceProfile profile={currentProfile.profileName}");
    }

    public void ClearForcedProfile()
    {
        forceDebugProfile = false;
        Log($"[DDA] ForceProfile cleared current={GetCurrentProfile().profileName}");
    }

    public void ApplyCurrentProfileToEnemy(GameObject enemyRoot)
    {
        if (enemyRoot == null || !enableDdaBehaviorProfiles)
        {
            return;
        }

        DdaDifficultyProfile profile = GetCurrentProfile();
        IDdaAdaptiveEnemy[] adaptiveEnemies = enemyRoot.GetComponentsInChildren<IDdaAdaptiveEnemy>(true);
        foreach (IDdaAdaptiveEnemy adaptiveEnemy in adaptiveEnemies)
        {
            adaptiveEnemy.ApplyDdaProfile(profile);
        }

        if (adaptiveEnemies.Length > 0)
        {
            Log(
                $"[DDA] ApplyEnemyProfile enemy={enemyRoot.name} profile={profile.profileName} " +
                $"adaptiveComponents={adaptiveEnemies.Length}");
        }
    }

    public int GetSpawnCountDelta()
    {
        return GetSpawnCountDelta(GetCurrentTier());
    }

    public int AdjustRoomSpawnTotal(int baseTotal, bool isBossRoom, string roomName, bool hasSpawnProfile = true)
    {
        if (!hasSpawnProfile)
        {
            Log($"[DDA] ApplySpawn skipped room={roomName} reason=NoSpawnProfile");
            return baseTotal;
        }

        if (!enableDdaSpawnScaling)
        {
            Log($"[DDA] ApplySpawn skipped room={roomName} reason=Disabled");
            return baseTotal;
        }

        if (isBossRoom)
        {
            Log($"[DDA] ApplySpawn skipped room={roomName} reason=BossRoom");
            return baseTotal;
        }

        DdaDifficultyTier tier = GetCurrentTier();
        int delta = GetSpawnCountDelta(tier);
        int finalTotal = baseTotal + delta;

        if (baseTotal > 0)
        {
            finalTotal = Mathf.Max(1, finalTotal);
        }
        else
        {
            finalTotal = Mathf.Max(0, finalTotal);
        }

        finalTotal = Mathf.Min(finalTotal, baseTotal + Mathf.Max(0, maxExtraSpawns));
        int appliedDelta = finalTotal - baseTotal;

        Log(
            $"[DDA] ApplySpawnSummary room={roomName} tier={tier} baseTotal={baseTotal} " +
            $"delta={appliedDelta} finalTotal={finalTotal}");

        return finalTotal;
    }

    private int GetSpawnCountDelta(DdaDifficultyTier tier)
    {
        switch (tier)
        {
            case DdaDifficultyTier.Recovery:
                return recoverySpawnDelta;
            case DdaDifficultyTier.Easy:
                return easySpawnDelta;
            case DdaDifficultyTier.Pressure:
                return pressureSpawnDelta;
            case DdaDifficultyTier.Normal:
            default:
                return normalSpawnDelta;
        }
    }

    private void SetCurrentProfile(
        DdaDifficultyProfile profile,
        RoomTelemetrySample sample,
        DdaScoreCalculator.ScoreBreakdown breakdown)
    {
        currentProfile = (profile ?? DdaDifficultyProfile.Balanced()).ClampedCopy();
        currentProfileType = currentProfile.profileType;

        float hpRatio = sample.playerMaxHp > 0f ? Mathf.Clamp01(sample.playerHpEnd / sample.playerMaxHp) : 0f;
        Log(
            $"[DDA] ProfileResolved: {currentProfile.profileName} " +
            $"score={breakdown.finalScore:0.##} tier={breakdown.tier} " +
            $"damageTaken={sample.damageTaken:0.##} hpRatio={hpRatio:0.##} " +
            $"clearTime={sample.clearTime:0.##} reason={currentProfile.reason}");
        Log(currentProfile.ToDebugString());
    }

    private DdaDifficultyProfile ResolveProfile(
        RoomTelemetrySample sample,
        DdaScoreCalculator.ScoreBreakdown breakdown)
    {
        if (sample == null)
        {
            return DdaDifficultyProfile.Balanced();
        }

        float maxHp = Mathf.Max(1f, sample.playerMaxHp);
        float hpRatio = Mathf.Clamp01(sample.playerHpEnd / maxHp);
        float damageRatio = Mathf.Clamp01(sample.damageTaken / maxHp);
        float score = breakdown.finalScore;
        DdaProfileType previousProfile = currentProfileType;

        bool hardAssist =
            hpRatio <= lowHpAssistRatio ||
            damageRatio >= highDamageAssistRatio ||
            breakdown.tier == DdaDifficultyTier.Recovery;
        bool scoreAssist =
            score <= assistEnterScore ||
            (previousProfile == DdaProfileType.Assist && score < assistExitScore);
        bool challengeReady =
            score >= challengeEnterScore &&
            hpRatio >= challengeMinHpRatio &&
            damageRatio <= challengeMaxDamageRatio &&
            breakdown.clearTimeScore >= challengeMinClearTimeScore &&
            breakdown.killScore >= 0.75f;
        bool keepChallenge =
            previousProfile == DdaProfileType.Challenge &&
            score >= challengeExitScore &&
            hpRatio >= challengeMinHpRatio &&
            damageRatio <= challengeMaxDamageRatio;

        if (hardAssist || scoreAssist)
        {
            string reason = hardAssist
                ? $"High pressure: hpRatio={hpRatio:0.##}, damageRatio={damageRatio:0.##}, hits={sample.hitsTaken}."
                : $"Performance score below Assist band with hysteresis: score={score:0.##}, previous={previousProfile}.";
            return DdaDifficultyProfile.Assist(reason, score);
        }

        if (challengeReady || keepChallenge)
        {
            string reason = challengeReady
                ? $"Strong clear: high HP, low damage taken, fast clear, kills={sample.enemiesKilled}."
                : $"Maintaining Challenge via hysteresis: score={score:0.##}, previous={previousProfile}.";
            return DdaDifficultyProfile.Challenge(reason, score);
        }

        return DdaDifficultyProfile.Balanced(
            $"Performance stayed in target band: score={score:0.##}, hpRatio={hpRatio:0.##}, damageRatio={damageRatio:0.##}.",
            score);
    }

    private DdaDifficultyProfile GetProfile(DdaProfileType profileType, string reason = null, float sourceScore = 0f)
    {
        switch (profileType)
        {
            case DdaProfileType.Assist:
                return DdaDifficultyProfile.Assist(reason, sourceScore);
            case DdaProfileType.Challenge:
                return DdaDifficultyProfile.Challenge(reason, sourceScore);
            case DdaProfileType.Balanced:
            default:
                return DdaDifficultyProfile.Balanced(reason, sourceScore);
        }
    }

    private void Log(string message)
    {
        if (!enableDdaDebug)
        {
            return;
        }

        Debug.Log(message, this);
    }
}
