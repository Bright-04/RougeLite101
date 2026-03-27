using System.Collections;
using UnityEngine;

public class ChangeSceneOnCollide : MonoBehaviour
{
    [SerializeField] string changeSceneName;
    [Header("Transition Override")]
    [SerializeField] private bool overrideTransitionTiming = false;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.14f;
    [SerializeField, Min(0f)] private float holdDuration = 0.08f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.18f;
    [SerializeField] private bool showLoadingText = true;
    [SerializeField] private string loadingText = "Loading...";

    private bool _loading;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || _loading)
        {
            return;
        }

        StartCoroutine(LoadWithTransition());
    }

    private IEnumerator LoadWithTransition()
    {
        _loading = true;
        if (overrideTransitionTiming)
        {
            yield return ScreenTransition.PlaySceneLoad(
                changeSceneName,
                fadeOutDuration,
                holdDuration,
                fadeInDuration,
                showLoadingText,
                loadingText
            );
        }
        else
        {
            yield return ScreenTransition.PlaySceneLoad(changeSceneName);
        }
        _loading = false;
    }
}
