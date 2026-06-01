using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSellPanelUI : MonoBehaviour
{
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;

    [SerializeField]
    private Button sellBtn;

    [SerializeField]
    private Button exitBtn;

    public event Action OnShopSellItemClicked;
    public event Action OnShopSellGoBackClicked;

    public void Awake()
    {
        Hide();
    }

    private void Start()
    {
        sellBtn.onClick.AddListener(() => { OnShopSellItemClicked?.Invoke(); });
        exitBtn.onClick.AddListener(() => { OnShopSellGoBackClicked?.Invoke(); });
    }


    private void OnDestroy()
    {
        sellBtn.onClick.RemoveAllListeners();
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
