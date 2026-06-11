using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [Min(2f)]
    [SerializeField] private float minimumLoadingSeconds = 2f;

    private IEnumerator Start()
    {
        string targetSceneName = SceneLoadingRequest.TargetSceneName;
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("LoadingScreenController: no target scene was requested.", this);
            yield break;
        }

        float startedAt = Time.unscaledTime;
        float requiredLoadingSeconds = Mathf.Max(2f, minimumLoadingSeconds);

        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);
        if (loadOperation == null)
        {
            Debug.LogError($"LoadingScreenController: failed to load scene '{targetSceneName}'.", this);
            SceneLoadingRequest.Clear();
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        while (Time.unscaledTime - startedAt < requiredLoadingSeconds)
        {
            yield return null;
        }

        SceneLoadingRequest.Clear();
        loadOperation.allowSceneActivation = true;
    }
}
