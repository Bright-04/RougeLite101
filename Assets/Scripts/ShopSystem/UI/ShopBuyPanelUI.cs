using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopBuyPanelUI : MonoBehaviour
{
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;

    [SerializeField]
    private Button buyBtn;

    [SerializeField]
    private Button exitBtn;

    public event Action OnShopBuyItemClicked;
    public event Action OnShopBuyGoBackClicked;

    public void Awake()
    {
        Hide();
    }

    private void Start()
    {
        buyBtn.onClick.AddListener(() => { OnShopBuyItemClicked?.Invoke(); });
        exitBtn.onClick.AddListener(() => { OnShopBuyGoBackClicked?.Invoke(); });
    }


    private void OnDestroy()
    {
        buyBtn.onClick.RemoveAllListeners();        
        exitBtn.onClick.RemoveAllListeners();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
