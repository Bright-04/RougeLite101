using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndGameResultUI : MonoBehaviour
{
    [Header("Structure")]
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private bool usePreparedPanelVisuals;
    [SerializeField] private GameObject winOneStarPanel;
    [SerializeField] private GameObject winTwoStarPanel;
    [SerializeField] private GameObject winThreeStarPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private GameObject starsRoot;
    [SerializeField] private Image starImageOne;
    [SerializeField] private Image starImageTwo;
    [SerializeField] private Image starImageThree;
    [SerializeField] private GameObject loseIconRoot;
    [SerializeField] private Button restartButton;
    [SerializeField] private TMP_Text restartButtonText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private Button closeButton;

    public bool IsConfigured =>
        resultRoot != null &&
        winOneStarPanel != null &&
        winTwoStarPanel != null &&
        winThreeStarPanel != null &&
        losePanel != null &&
        titleText != null &&
        summaryText != null &&
        restartButton != null &&
        starsRoot != null &&
        starImageOne != null &&
        starImageTwo != null &&
        starImageThree != null;

    private void Awake()
    {
        if (resultRoot == null)
        {
            resultRoot = gameObject;
        }

        HideInstant();
    }

    public void HideInstant()
    {
        if (resultRoot != null)
        {
            resultRoot.SetActive(false);
        }
    }

    public bool TryShow(RunResultType resultType, int stars, bool showNextButton, bool showCloseButton, string summary)
    {
        if (!IsConfigured)
        {
            Debug.LogError("EndGameResultUI is missing one or more required references.", this);
            return false;
        }

        bool isWin = resultType == RunResultType.Win;
        int clampedStars = isWin ? Mathf.Clamp(stars, 1, 3) : 0;

        resultRoot.SetActive(true);
        SetPanelState(resultType, clampedStars);

        if (usePreparedPanelVisuals)
        {
            titleText.gameObject.SetActive(false);
            summaryText.gameObject.SetActive(false);
        }
        else
        {
            titleText.gameObject.SetActive(true);
            summaryText.gameObject.SetActive(true);
            titleText.text = isWin ? "Victory" : "Defeat";
            summaryText.text = string.IsNullOrWhiteSpace(summary)
                ? (isWin ? "The run is complete." : "The run has ended.")
                : summary;
        }

        if (usePreparedPanelVisuals)
        {
            starsRoot.SetActive(false);
            if (loseIconRoot != null)
            {
                loseIconRoot.SetActive(false);
            }
        }
        else
        {
            starsRoot.SetActive(isWin);
            if (loseIconRoot != null)
            {
                loseIconRoot.SetActive(!isWin);
            }

            if (isWin)
            {
                UpdateStarDisplay(clampedStars);
            }
        }

        restartButton.gameObject.SetActive(true);
        if (restartButtonText != null)
        {
            restartButtonText.gameObject.SetActive(!usePreparedPanelVisuals);
            restartButtonText.text = usePreparedPanelVisuals
                ? string.Empty
                : "Return To Hub";
        }

        if (nextButton != null)
        {
            bool shouldShowNextButton = usePreparedPanelVisuals
                ? (isWin && showNextButton)
                : showNextButton;

            nextButton.gameObject.SetActive(shouldShowNextButton);
            if (nextButtonText != null)
            {
                nextButtonText.gameObject.SetActive(!usePreparedPanelVisuals);
                nextButtonText.text = usePreparedPanelVisuals ? string.Empty : "Next";
            }
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(showCloseButton);
        }

        return true;
    }

    private void SetPanelState(RunResultType resultType, int stars)
    {
        bool showLose = resultType == RunResultType.Lose;
        losePanel.SetActive(showLose);
        winOneStarPanel.SetActive(!showLose && stars == 1);
        winTwoStarPanel.SetActive(!showLose && stars == 2);
        winThreeStarPanel.SetActive(!showLose && stars == 3);
    }

    private void UpdateStarDisplay(int activeStars)
    {
        SetStarState(starImageOne, activeStars >= 1);
        SetStarState(starImageTwo, activeStars >= 2);
        SetStarState(starImageThree, activeStars >= 3);
    }

    private void SetStarState(Image starImage, bool isActive)
    {
        Color color = starImage.color;
        color.a = isActive ? 1f : 0.25f;
        starImage.color = color;
    }
}
