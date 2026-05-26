using UnityEngine;
using UnityEngine.SceneManagement;
using RougeLite.Run;

public class RunResultSceneController : MonoBehaviour
{
    [SerializeField] private EndGameResultUI resultUI;
    [SerializeField] private string hubSceneName = "GameHome";

    private void Start()
    {
        Time.timeScale = 1f;
        Debug.Log($"RunResultSceneController: Start with HasResult={RunResultSession.HasResult}, ResultType={RunResultSession.ResultType}, Stars={RunResultSession.Stars}.");

        if (!RunResultSession.HasResult)
        {
            Debug.LogError("RunResultSceneController: No run result session was available. Returning to hub.", this);
            SceneManager.LoadScene(hubSceneName);
            return;
        }

        if (resultUI == null)
        {
            resultUI = GetComponentInChildren<EndGameResultUI>(true);
        }

        if (resultUI == null)
        {
            Debug.LogError("RunResultSceneController: EndGameResultUI reference is missing. Returning to hub.", this);
            SceneManager.LoadScene(hubSceneName);
            return;
        }

        bool showNextButton = RunResultSession.ResultType == RunResultType.Win;
        if (!resultUI.TryShow(RunResultSession.ResultType, RunResultSession.Stars, showNextButton, showCloseButton: false, RunResultSession.Summary))
        {
            Debug.LogError("RunResultSceneController: EndGameResultUI failed to show the result. Returning to hub.", this);
            SceneManager.LoadScene(hubSceneName);
        }
    }

    public void ReturnToHub()
    {
        RunResultSession.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(hubSceneName);
    }
}
