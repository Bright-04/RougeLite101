using UnityEngine;

public enum RunResultPreviewMode
{
    Hidden,
    Victory1Star,
    Victory2Stars,
    Victory3Stars,
    Defeat
}

public class RunResultUIPreview : MonoBehaviour
{
    [SerializeField] private EndGameResultUI resultUI;
    [SerializeField] private RunResultPreviewMode previewMode = RunResultPreviewMode.Hidden;
    [SerializeField] private string previewSummary = "Preview Summary";

    public EndGameResultUI ResultUI => resultUI;
    public RunResultPreviewMode PreviewMode => previewMode;
    public string PreviewSummary => previewSummary;

    public bool ApplyPreview()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("RunResultUIPreview: Preview actions are disabled during Play Mode.", this);
            return false;
        }

        EndGameResultUI resolvedResultUI = ResolveResultUI();
        if (resolvedResultUI == null)
        {
            Debug.LogWarning("RunResultUIPreview: EndGameResultUI reference is missing.", this);
            return false;
        }

        switch (previewMode)
        {
            case RunResultPreviewMode.Hidden:
                resolvedResultUI.HideInstant();
                return true;

            case RunResultPreviewMode.Victory1Star:
                return resolvedResultUI.TryShow(RunResultType.Win, 1, showNextButton: false, showCloseButton: false, previewSummary);

            case RunResultPreviewMode.Victory2Stars:
                return resolvedResultUI.TryShow(RunResultType.Win, 2, showNextButton: false, showCloseButton: false, previewSummary);

            case RunResultPreviewMode.Victory3Stars:
                return resolvedResultUI.TryShow(RunResultType.Win, 3, showNextButton: false, showCloseButton: false, previewSummary);

            case RunResultPreviewMode.Defeat:
                return resolvedResultUI.TryShow(RunResultType.Lose, 0, showNextButton: false, showCloseButton: false, previewSummary);

            default:
                return false;
        }
    }

    public bool HidePreview()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("RunResultUIPreview: Preview actions are disabled during Play Mode.", this);
            return false;
        }

        EndGameResultUI resolvedResultUI = ResolveResultUI();
        if (resolvedResultUI == null)
        {
            Debug.LogWarning("RunResultUIPreview: EndGameResultUI reference is missing.", this);
            return false;
        }

        resolvedResultUI.HideInstant();
        return true;
    }

    private EndGameResultUI ResolveResultUI()
    {
        if (resultUI == null)
        {
            resultUI = GetComponent<EndGameResultUI>();
        }

        return resultUI;
    }
}
