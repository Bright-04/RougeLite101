using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text promptText;

    private void Awake()
    {
        Hide();
    }

    public void Show(string text)
    {
        gameObject.SetActive(true);

        if (promptText != null)
        {
            promptText.text = text;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
