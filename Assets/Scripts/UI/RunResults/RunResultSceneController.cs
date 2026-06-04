using UnityEngine;
using UnityEngine.SceneManagement;
using RougeLite.Run;

public class RunResultSceneController : MonoBehaviour
{
    [SerializeField] private EndGameResultUI resultUI;
    [SerializeField] private string hubSceneName = "GameHome";
    [SerializeField] private Vector3 hubSpawnPosition = Vector3.zero;
    [SerializeField] private bool logRunResultSummary = false;

    private void Start()
    {
        Time.timeScale = 1f;
        if (logRunResultSummary)
        {
            Debug.Log($"RunResultSceneController: Start with HasResult={RunResultSession.HasResult}, ResultType={RunResultSession.ResultType}, Stars={RunResultSession.Stars}.");
        }

        if (!RunResultSession.HasResult)
        {
            Debug.LogError("RunResultSceneController: No run result session was available. Returning to hub.", this);
            ReturnToHub();
            return;
        }

        if (resultUI == null)
        {
            resultUI = GetComponentInChildren<EndGameResultUI>(true);
        }

        if (resultUI == null)
        {
            Debug.LogError("RunResultSceneController: EndGameResultUI reference is missing. Returning to hub.", this);
            ReturnToHub();
            return;
        }

        bool showNextButton = RunResultSession.ResultType == RunResultType.Win;
        if (!resultUI.TryShow(RunResultSession.ResultType, RunResultSession.Stars, showNextButton, showCloseButton: false, RunResultSession.Summary))
        {
            Debug.LogError("RunResultSceneController: EndGameResultUI failed to show the result. Returning to hub.", this);
            ReturnToHub();
        }
    }

    public void ReturnToHub()
    {
        RunResultSession.Clear();
        Time.timeScale = 1f;

        MovePlayerToHubSpawn();

        SceneManager.LoadScene(hubSceneName);
    }

    private void MovePlayerToHubSpawn()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("RunResultSceneController: Could not find Player before returning to hub.", this);
            return;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = hubSpawnPosition;
        }

        player.transform.position = hubSpawnPosition;

        if (logRunResultSummary)
        {
            Debug.Log($"RunResultSceneController: Moved Player to hub spawn {hubSpawnPosition}.", this);
        }
    }
}
