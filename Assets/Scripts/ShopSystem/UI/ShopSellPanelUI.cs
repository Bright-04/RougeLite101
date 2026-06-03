using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSellPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject sellControls;
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;

    [SerializeField]
    private Button sellBtn;

    [SerializeField]
    private Button exitBtn;

    public event Action<int> OnShopSellItemClicked;
    public event Action OnShopSellGoBackClicked;

    public void Awake()
    {
        HideAmountControls();
        Hide();
    }

    private void Start()
    {
        amountSlider.onValueChanged.AddListener(UpdateAmountText);
        sellBtn.onClick.AddListener(() => { OnShopSellItemClicked?.Invoke(GetAmount()); });
        exitBtn.onClick.AddListener(() => { OnShopSellGoBackClicked?.Invoke(); });
    }

    private void OnDestroy()
    {
        amountSlider.onValueChanged.RemoveAllListeners();
        sellBtn.onClick.RemoveAllListeners();
        exitBtn.onClick.RemoveAllListeners();
    }

    public void Setup(int maxAmount)
    {
        ShowAmountControls();
        amountSlider.minValue = 1;
        amountSlider.maxValue = Mathf.Max(1, maxAmount);
        amountSlider.wholeNumbers = true;
        amountSlider.value = 1;

        UpdateAmountText(1);
    }

    private void UpdateAmountText(float value)
    {
        amountText.text = ((int)value).ToString();
    }

    public int GetAmount()
    {
        return (int)amountSlider.value;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void ShowAmountControls()
    {
        sellControls.SetActive(true);
    }

    public void HideAmountControls()
    {
        sellControls.SetActive(false);
    }
}
