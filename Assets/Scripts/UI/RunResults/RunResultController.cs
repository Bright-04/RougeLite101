using UnityEngine;
using UnityEngine.SceneManagement;

public class RunResultController : MonoBehaviour
{
    public static RunResultController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private EndGameResultUI resultUI;

    [Header("Star Rating")]
    [SerializeField] private RunStarRatingCalculator starRatingCalculator = new RunStarRatingCalculator();

    [Header("Scene Flow")]
    [SerializeField] private string hubSceneName = "GameHome";
    [SerializeField] private Vector3 hubSpawnPosition = new Vector3(0f, 9f, 0f);

    public bool IsResultActive { get; private set; }
    public bool IsRunFinished { get; private set; }

    private EnemyDeathNotifier subscribedBossDeathNotifier;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (resultUI == null)
        {
            resultUI = GetComponentInChildren<EndGameResultUI>(true);
        }

        ResetViewState();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void Start()
    {
        RefreshBossDeathSubscription();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        UnsubscribeBossDeathNotifier();
        RestoreGameplayState();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool ShowLose(PlayerStats playerStats)
    {
        if (IsRunFinished)
        {
            return true;
        }

        string summary = BuildSummaryText(playerStats, RunResultType.Lose, 0);
        return ShowResult(RunResultType.Lose, 0, summary);
    }

    public bool ShowWin(PlayerStats playerStats)
    {
        if (IsRunFinished)
        {
            return true;
        }

        int stars = starRatingCalculator.CalculateStars(playerStats);
        string summary = BuildSummaryText(playerStats, RunResultType.Win, stars);

        if (!ShowResult(RunResultType.Win, stars, summary))
        {
            Debug.LogError("RunResultController could not display the win screen. Falling back to hub return.", this);
            ReturnToHub();
            return false;
        }

        return true;
    }

    public void RefreshBossDeathSubscription()
    {
        UnsubscribeBossDeathNotifier();

        if (IsRunFinished)
        {
            return;
        }

        DungeonManager dungeonManager = FindAnyObjectByType<DungeonManager>();
        if (dungeonManager == null || !dungeonManager.IsCurrentFloorBossFloor())
        {
            return;
        }

        EnemyDeathNotifier bossNotifier = dungeonManager.GetBossDeathNotifier();
        if (bossNotifier == null)
        {
            return;
        }

        subscribedBossDeathNotifier = bossNotifier;
        subscribedBossDeathNotifier.Died += OnBossDeathNotifierDied;
    }

    public void OnRestartPressed()
    {
        ReturnToHub();
    }

    public void OnNextPressed()
    {
        ReturnToHub();
    }

    public void OnClosePressed()
    {
        ReturnToHub();
    }

    private bool ShowResult(RunResultType resultType, int stars, string summary)
    {
        if (resultUI == null)
        {
            Debug.LogError("RunResultController is missing EndGameResultUI.", this);
            return false;
        }

        if (!resultUI.TryShow(resultType, stars, showNextButton: false, showCloseButton: false, summary))
        {
            return false;
        }

        IsRunFinished = true;
        IsResultActive = true;
        Time.timeScale = 0f;

        PauseMenu pauseMenu = FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pauseMenu != null)
        {
            pauseMenu.HideForSystemOverlay();
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.EnableUIMap();
        }

        return true;
    }

    private string BuildSummaryText(PlayerStats playerStats, RunResultType resultType, int stars)
    {
        if (playerStats == null)
        {
            return resultType == RunResultType.Win ? $"Stars Earned: {stars}" : "Try again from the hub.";
        }

        int currentHp = Mathf.Max(0, Mathf.RoundToInt(playerStats.currentHP));
        int maxHp = Mathf.Max(1, Mathf.RoundToInt(playerStats.GetTotalMaxHP()));

        if (resultType == RunResultType.Win)
        {
            return $"Stars Earned: {stars}\nHP Remaining: {currentHp}/{maxHp}";
        }

        return $"HP Remaining: {currentHp}/{maxHp}";
    }

    private void ReturnToHub()
    {
        ResetViewState();
        RestoreGameplayState();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = hubSpawnPosition;
        }

        SceneManager.LoadScene(hubSceneName);
    }

    private void RestoreGameplayState()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.DisableUIMap();
        }
    }

    private void ResetViewState()
    {
        IsResultActive = false;
        IsRunFinished = false;

        if (resultUI != null)
        {
            resultUI.HideInstant();
        }
    }

    private void OnBossDeathNotifierDied(EnemyDeathNotifier notifier)
    {
        if (notifier != subscribedBossDeathNotifier || IsRunFinished)
        {
            return;
        }

        UnsubscribeBossDeathNotifier();
        ShowWin(FindAnyObjectByType<PlayerStats>());
    }

    private void UnsubscribeBossDeathNotifier()
    {
        if (subscribedBossDeathNotifier == null)
        {
            return;
        }

        subscribedBossDeathNotifier.Died -= OnBossDeathNotifierDied;
        subscribedBossDeathNotifier = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetViewState();
        RestoreGameplayState();
        RefreshBossDeathSubscription();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        UnsubscribeBossDeathNotifier();
    }
}
