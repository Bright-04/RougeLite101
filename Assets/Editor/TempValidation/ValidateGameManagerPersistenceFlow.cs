using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidateGameManagerPersistenceFlow
{
    private const string LogPrefix = "[GMValidation]";
    private const int SettleFrames = 5;

    private static readonly Queue<ValidationStep> Steps = new Queue<ValidationStep>();

    private static bool isRunning;
    private static string expectedSceneName;
    private static int framesRemaining;
    private static ValidationStep currentStep;

    [MenuItem("Tools/Validate GameManager Persistence Flow")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError($"{LogPrefix} Must be executed in Play Mode.");
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning($"{LogPrefix} Validation is already running.");
            return;
        }

        Steps.Clear();
        EnqueueSteps();

        isRunning = true;
        currentStep = default;
        expectedSceneName = null;
        framesRemaining = 0;

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;

        Debug.Log($"{LogPrefix} Starting GameManager persistence flow validation.");
    }

    private static void EnqueueSteps()
    {
        Steps.Enqueue(new ValidationStep
        {
            stageName = "GameHome initial",
            expectedScene = "GameHome",
            captureOnly = true
        });
        Steps.Enqueue(new ValidationStep
        {
            stageName = "Dungeon first load",
            expectedScene = "Dungeon",
            sceneToLoad = "Dungeon"
        });
        Steps.Enqueue(new ValidationStep
        {
            stageName = "GameHome return",
            expectedScene = "GameHome",
            sceneToLoad = "GameHome"
        });
        Steps.Enqueue(new ValidationStep
        {
            stageName = "Dungeon second load",
            expectedScene = "Dungeon",
            sceneToLoad = "Dungeon"
        });
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            StopValidation("Play Mode exited before validation completed.");
            return;
        }

        if (framesRemaining > 0)
        {
            framesRemaining--;
            return;
        }

        if (expectedSceneName != null)
        {
            if (SceneManager.GetActiveScene().name != expectedSceneName)
            {
                return;
            }

            expectedSceneName = null;
            framesRemaining = SettleFrames;
            return;
        }

        if (!currentStep.Equals(default(ValidationStep)))
        {
            CaptureSnapshot(currentStep.stageName);
            currentStep = default;
        }

        if (Steps.Count == 0)
        {
            StopValidation("Validation completed.");
            return;
        }

        currentStep = Steps.Dequeue();

        if (!string.IsNullOrEmpty(currentStep.sceneToLoad))
        {
            SceneManager.LoadScene(currentStep.sceneToLoad);
        }

        expectedSceneName = currentStep.expectedScene;
    }

    private static void CaptureSnapshot(string stageName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string loadedScenes = string.Join(", ", Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt)
            .Where(scene => scene.IsValid())
            .Select(scene => $"{scene.name}[active={scene == activeScene}]"));

        List<GameManager> gameManagers = FindRuntimeComponents<GameManager>();
        List<AutoSaveManager> saveManagers = FindRuntimeComponents<AutoSaveManager>();
        List<RunResultController> runResultControllers = FindRuntimeComponents<RunResultController>();
        List<InputManager> inputManagers = FindRuntimeComponents<InputManager>();
        List<CursorManager> cursorManagers = FindRuntimeComponents<CursorManager>();

        List<PlayerStats> playerStatsObjects = FindRuntimeComponents<PlayerStats>();
        List<GameObject> playerTagObjects = FindRuntimeGameObjects()
            .Where(go => go.CompareTag("Player"))
            .ToList();
        List<GameObject> playerRootNameObjects = FindRuntimeGameObjects()
            .Where(go => go.transform.parent == null && go.name == "Player")
            .ToList();

        List<GameObject> canvasUiRootObjects = FindRuntimeGameObjects()
            .Where(go => go.transform.parent == null && go.name == "Canvas_UI")
            .ToList();

        List<string> duplicateFindings = new List<string>();
        AddDuplicateFindingIfNeeded(duplicateFindings, "GameManager", gameManagers.Count);
        AddDuplicateFindingIfNeeded(duplicateFindings, "SaveManager", saveManagers.Count);
        AddDuplicateFindingIfNeeded(duplicateFindings, "PlayerStats", playerStatsObjects.Count);
        AddDuplicateFindingIfNeeded(duplicateFindings, "PlayerTag", playerTagObjects.Count);
        AddDuplicateFindingIfNeeded(duplicateFindings, "PlayerRootName", playerRootNameObjects.Count);
        AddDuplicateFindingIfNeeded(duplicateFindings, "Canvas_UI", canvasUiRootObjects.Count);
        AddDuplicateFindingIfNeeded(duplicateFindings, "RunResultController", runResultControllers.Count);
        AddDuplicateFindingIfNeeded(duplicateFindings, "InputManager", inputManagers.Count);
        AddDuplicateFindingIfNeeded(duplicateFindings, "CursorManager", cursorManagers.Count);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{LogPrefix} Stage: {stageName}");
        sb.AppendLine($"Loaded scene(s): {loadedScenes}");
        sb.AppendLine($"GameManager count: {gameManagers.Count}");
        sb.AppendLine($"SaveManager count: {saveManagers.Count}");
        sb.AppendLine($"Player count: {playerStatsObjects.Count} (method: PlayerStats={playerStatsObjects.Count}, PlayerTag={playerTagObjects.Count}, RootName={playerRootNameObjects.Count})");
        sb.AppendLine($"Canvas_UI count: {canvasUiRootObjects.Count} (method: root object name cross-check)");
        sb.AppendLine($"RunResultController count: {runResultControllers.Count}");
        sb.AppendLine($"InputManager count: {inputManagers.Count}");
        sb.AppendLine($"CursorManager count: {cursorManagers.Count}");
        sb.AppendLine($"GameManager details: {FormatComponentDetails(gameManagers)}");
        sb.AppendLine($"SaveManager details: {FormatComponentDetails(saveManagers)}");
        sb.AppendLine($"Player details: {FormatGameObjectDetails(playerRootNameObjects)}");
        sb.AppendLine($"Canvas_UI details: {FormatGameObjectDetails(canvasUiRootObjects)}");
        sb.AppendLine($"RunResultController details: {FormatComponentDetails(runResultControllers)}");
        sb.AppendLine($"InputManager details: {FormatComponentDetails(inputManagers)}");
        sb.AppendLine($"CursorManager details: {FormatComponentDetails(cursorManagers)}");
        sb.AppendLine($"GameManager.Instance: {FormatSingleton(GameManager.Instance)}");
        sb.AppendLine($"RunResultController.Instance: {FormatSingleton(RunResultController.Instance)}");
        sb.AppendLine($"Unexpected duplicates: {(duplicateFindings.Count > 0 ? string.Join("; ", duplicateFindings) : "None")}");
        sb.AppendLine($"Notes: Active scene is {activeScene.name}.");

        Debug.Log(sb.ToString());
    }

    private static List<T> FindRuntimeComponents<T>() where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(component => component != null && component.gameObject != null && component.gameObject.scene.IsValid())
            .ToList();
    }

    private static List<GameObject> FindRuntimeGameObjects()
    {
        return Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(go => go != null && go.scene.IsValid())
            .ToList();
    }

    private static string FormatComponentDetails<T>(IEnumerable<T> components) where T : Component
    {
        List<string> details = components
            .Select(component => $"{component.gameObject.name}[active={component.gameObject.activeInHierarchy}, scene={component.gameObject.scene.name}]")
            .ToList();

        return details.Count > 0 ? string.Join(", ", details) : "None";
    }

    private static string FormatGameObjectDetails(IEnumerable<GameObject> objects)
    {
        List<string> details = objects
            .Select(go => $"{go.name}[active={go.activeInHierarchy}, scene={go.scene.name}]")
            .ToList();

        return details.Count > 0 ? string.Join(", ", details) : "None";
    }

    private static void AddDuplicateFindingIfNeeded(List<string> findings, string label, int count)
    {
        if (count > 1)
        {
            findings.Add($"{label}={count}");
        }
    }

    private static string FormatSingleton(Component component)
    {
        if (component == null || component.gameObject == null)
        {
            return "None";
        }

        return $"{component.gameObject.name}[active={component.gameObject.activeInHierarchy}, scene={component.gameObject.scene.name}]";
    }

    private static void StopValidation(string message)
    {
        isRunning = false;
        expectedSceneName = null;
        framesRemaining = 0;
        currentStep = default;
        Steps.Clear();
        EditorApplication.update -= Tick;
        Debug.Log($"{LogPrefix} {message}");
    }

    private struct ValidationStep
    {
        public string stageName;
        public string expectedScene;
        public string sceneToLoad;
        public bool captureOnly;
    }
}
