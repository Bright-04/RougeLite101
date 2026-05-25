using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class RunResultController : MonoBehaviour
{
    public static RunResultController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] public EndGameResultUI resultUI;

    [Header("Star Rating")]
    [SerializeField] private RunStarRatingCalculator starRatingCalculator = new RunStarRatingCalculator();

    [Header("Scene Flow")]
    [SerializeField] private string hubSceneName = "GameHome";
    [SerializeField] private Vector3 hubSpawnPosition = new Vector3(0f, 9f, 0f);

    public bool IsResultActive { get; private set; }
    public bool IsRunFinished { get; private set; }

    private BossEncounterController subscribedBossEncounterController;
    private Coroutine pendingBossClearRoutine;

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
        SubscribeToBossEncounter();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        UnsubscribeFromBossEncounter();
        CancelPendingBossClear();
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

        if (playerStats == null || playerStats.IsDead)
        {
            Debug.Log("RunResultController: Ignoring win because the player is dead or missing.", this);
            return false;
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

        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.ResetTransientState();
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
        AutoSaveManager.TrySaveActiveSceneState();

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
        CancelPendingBossClear();
        IsResultActive = false;
        IsRunFinished = false;

        if (resultUI != null)
        {
            resultUI.HideInstant();
        }
    }

    private void OnBossCleared()
    {
        if (IsRunFinished)
        {
            return;
        }

        UnsubscribeFromBossEncounter();

        if (pendingBossClearRoutine != null)
        {
            StopCoroutine(pendingBossClearRoutine);
        }

        pendingBossClearRoutine = StartCoroutine(ResolveBossClearAtEndOfFrame());
    }

    private void SubscribeToBossEncounter()
    {
        UnsubscribeFromBossEncounter();

        if (IsRunFinished)
        {
            return;
        }

        BossEncounterController bossEncounterController = BossEncounterController.Instance;
        if (bossEncounterController == null)
        {
            return;
        }

        subscribedBossEncounterController = bossEncounterController;
        subscribedBossEncounterController.BossCleared += OnBossCleared;
    }

    private void UnsubscribeFromBossEncounter()
    {
        if (subscribedBossEncounterController == null)
        {
            return;
        }

        subscribedBossEncounterController.BossCleared -= OnBossCleared;
        subscribedBossEncounterController = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetViewState();
        RestoreGameplayState();
        SubscribeToBossEncounter();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        UnsubscribeFromBossEncounter();
    }

    private IEnumerator ResolveBossClearAtEndOfFrame()
    {
        yield return null;
        pendingBossClearRoutine = null;

        if (IsRunFinished)
        {
            yield break;
        }

        DungeonManager dungeonManager = FindAnyObjectByType<DungeonManager>();
        if (dungeonManager == null)
        {
            Debug.LogWarning("RunResultController: Boss cleared but DungeonManager was not found.");
            yield break;
        }

        if (!dungeonManager.IsCurrentFloorBossFloor)
        {
            Debug.LogWarning($"RunResultController: Ignoring BossCleared on non-boss floor {dungeonManager.currentFloor}.", this);
            yield break;
        }

        if (dungeonManager.currentFloor < dungeonManager.maxFloor)
        {
            Debug.Log($"RunResultController: Boss on floor {dungeonManager.currentFloor} cleared. Continuing run until final floor {dungeonManager.maxFloor}.");
            yield break;
        }

        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats == null || playerStats.IsDead || playerStats.currentHP <= 0f)
        {
            Debug.Log("RunResultController: Boss cleared event arrived, but player is already dead.");
            yield break;
        }

        ShowWin(playerStats);
    }

    private void CancelPendingBossClear()
    {
        if (pendingBossClearRoutine == null)
        {
            return;
        }

        StopCoroutine(pendingBossClearRoutine);
        pendingBossClearRoutine = null;
    }
}
