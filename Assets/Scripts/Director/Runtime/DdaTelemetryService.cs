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

    [Header("Scoring")]
    [SerializeField] private DdaScoreCalculator scoreCalculator = new DdaScoreCalculator();

    [SerializeField] private RoomTelemetrySample activeRoom;
    [SerializeField] private RoomTelemetrySample lastCompletedRoom;

    public RoomTelemetrySample ActiveRoom => activeRoom;
    public RoomTelemetrySample LastCompletedRoom => lastCompletedRoom;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

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
            $"hpStart={activeRoom.playerHpStart:0.##}/{activeRoom.playerMaxHp:0.##}");
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
        activeRoom.score = scoreCalculator.Calculate(activeRoom);
        activeRoom.tier = scoreCalculator.GetTier(activeRoom.score);
        lastCompletedRoom = activeRoom;

        Log(
            $"[DDA] RoomClear room={activeRoom.roomName} boss={activeRoom.isBossRoom} " +
            $"clearTime={activeRoom.clearTime:0.##} hp={activeRoom.playerHpStart:0.##}->{activeRoom.playerHpEnd:0.##} " +
            $"dmgTaken={activeRoom.damageTaken:0.##} dmgDealt={activeRoom.damageDealt:0.##} " +
            $"kills={activeRoom.enemiesKilled} hitsTaken={activeRoom.hitsTaken}");
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

    private void Log(string message)
    {
        if (!enableDdaDebug)
        {
            return;
        }

        Debug.Log(message, this);
    }
}
