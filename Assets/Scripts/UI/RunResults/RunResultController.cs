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
    [SerializeField] private string runResultSceneName = "RunResultScene";
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
        if (!RunResultRules.CanShowLose(IsRunFinished))
        {
            return true;
        }

        string summary = RunResultRules.BuildSummaryText(RunResultType.Lose, 0, playerStats);
        return TransitionToRunResultScene(RunResultType.Lose, 0, summary, playerStats);
    }

    public bool ShowWin(PlayerStats playerStats)
    {
        if (IsRunFinished)
        {
            return true;
        }

        if (!RunResultRules.CanShowWin(IsRunFinished, playerStats))
        {
            Debug.Log("RunResultController: Ignoring win because the player is dead or missing.", this);
            return false;
        }

        int stars = starRatingCalculator.CalculateStars(playerStats);
        string summary = RunResultRules.BuildSummaryText(RunResultType.Win, stars, playerStats);

        return TransitionToRunResultScene(RunResultType.Win, stars, summary, playerStats);
    }

    public bool TryCompleteRunFromExitPortal()
    {
        if (IsRunFinished)
        {
            return true;
        }

        DungeonManager dungeonManager = FindAnyObjectByType<DungeonManager>();
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();

        if (dungeonManager == null)
        {
            Debug.LogError("RunResultController: Cannot complete the run from the exit portal because DungeonManager was not found.", this);
            return false;
        }

        if (!RunResultRules.ShouldCompleteRunFromExitPortal(dungeonManager.currentFloor, dungeonManager.maxFloor))
        {
            Debug.LogWarning($"RunResultController: Exit portal completion was requested before the final floor. Current floor {dungeonManager.currentFloor}, max floor {dungeonManager.maxFloor}.", this);
            return false;
        }

        if (!RunResultRules.CanShowWin(IsRunFinished, playerStats))
        {
            Debug.Log("RunResultController: Ignoring exit portal completion because the player is dead or missing.", this);
            return false;
        }

        int stars = starRatingCalculator.CalculateStars(playerStats);
        string summary = RunResultRules.BuildSummaryText(RunResultType.Win, stars, playerStats);
        return TransitionToRunResultScene(RunResultType.Win, stars, summary, playerStats);
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

    private void ReturnToHub()
    {
        // UI reset, scene flow, and persistence orchestration stay here.
        ResetViewState();
        RestoreGameplayState();
        Time.timeScale = 1f;
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
        if (Time.timeScale != 1f)
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
        if (RunResultRules.ShouldIgnoreBossClear(IsRunFinished))
        {
            return;
        }



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

        DungeonManager dungeonManager = FindAnyObjectByType<DungeonManager>();
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();

        if (RunResultRules.TryGetBossClearBlockReason(IsRunFinished, dungeonManager, playerStats, out string logMessage, out bool isWarning))
        {
            if (!string.IsNullOrEmpty(logMessage))
            {
                if (isWarning) Debug.LogWarning(logMessage, this);
                else Debug.Log(logMessage);
            }
            yield break;
        }

        if (RunResultRules.ShouldContinueRunAfterBossClear(dungeonManager.currentFloor, dungeonManager.maxFloor))
        {
            Debug.Log($"RunResultController: Boss on floor {dungeonManager.currentFloor} cleared. Continuing run until final floor {dungeonManager.maxFloor}.");
            yield break;
        }

        Debug.Log($"RunResultController: Final boss on floor {dungeonManager.currentFloor} cleared. Run will complete when the player enters the exit portal.");
    }

    private bool TransitionToRunResultScene(RunResultType resultType, int stars, string summary, PlayerStats playerStats)
    {
        if (playerStats != null)
        {
            playerStats.ResetTransientState();
        }

        IsRunFinished = true;
        IsResultActive = true;

        PauseMenu pauseMenu = FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pauseMenu != null)
        {
            pauseMenu.HideForSystemOverlay();
        }

        RestoreGameplayState();
        RunResultSession.SetResult(resultType, stars, summary);
        SceneManager.LoadScene(runResultSceneName);
        return true;
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
