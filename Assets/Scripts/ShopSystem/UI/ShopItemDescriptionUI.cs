using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemDescriptionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;

    public void Awake()
    {
        ResetDescription();
    }

    public void ResetDescription()
    {
        gameObject.SetActive(false);
        title.text = "";
        description.text = "";
    }

    public void SetDescription(string itemName, string itemDescription)
    {
        gameObject.SetActive(true);     
        title.text = itemName;
        description.text = itemDescription;
    }
}
