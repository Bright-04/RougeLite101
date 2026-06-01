using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopBuyPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject buyControls;
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;

    [SerializeField]
    private Button buyBtn;

    [SerializeField]
    private Button exitBtn;

    public event Action<int> OnShopBuyItemClicked;
    public event Action OnShopBuyGoBackClicked;

    public void Awake()
    {
        HideAmountControls();
        Hide();
    }

    private void Start()
    {
        amountSlider.onValueChanged.AddListener(UpdateAmountText);
        buyBtn.onClick.AddListener(() => { OnShopBuyItemClicked?.Invoke(GetAmount()); });
        exitBtn.onClick.AddListener(() => { OnShopBuyGoBackClicked?.Invoke(); });
    }


    private void OnDestroy()
    {
        amountSlider.onValueChanged.RemoveAllListeners();
        buyBtn.onClick.RemoveAllListeners();        
        exitBtn.onClick.RemoveAllListeners();
    }

    public void Setup(int maxAmount)
    {
        ShowAmountControls();
        amountSlider.minValue = 1;
        amountSlider.maxValue = maxAmount;
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
        buyControls.SetActive(true);
    }

    public void HideAmountControls()
    {
        buyControls.SetActive(false);
    }
}
