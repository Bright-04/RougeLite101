using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text dialogueText;

    private void Awake()
    {
        Hide();
    }

    public void Show(string text)
    {
        Debug.Log("DialogueUI show: " + text);
        gameObject.SetActive(true);
        
        SetText(text);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetText(string text)
    {
        Debug.Log("DialogueUI SetText: " + text);
        if (dialogueText != null) dialogueText.text = text;
            
    }
}
