using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoadingRequest
{
    public const string LoadingSceneName = "LoadingScene";

    public static string TargetSceneName { get; private set; }

    public static void LoadSceneWithLoading(string targetSceneName)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("SceneLoadingRequest: target scene name is missing.");
            return;
        }

        TargetSceneName = targetSceneName;
        SceneManager.LoadScene(LoadingSceneName);
    }

    public static void Clear()
    {
        TargetSceneName = null;
    }
}
