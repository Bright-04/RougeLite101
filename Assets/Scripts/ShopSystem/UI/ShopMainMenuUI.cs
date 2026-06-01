using System;
using UnityEngine;
using UnityEngine.UI;

public class ShopMainMenuUI : MonoBehaviour
{
    [SerializeField]
    private Button buyBtn;

    [SerializeField]
    private Button sellBtn;

    [SerializeField]
    private Button exitBtn;

    public event Action OnShopBuyClicked;
    public event Action OnShopSellClicked;
    public event Action OnShopExitClicked;

    private void Start()
    {
        buyBtn.onClick.AddListener(() => { OnShopBuyClicked?.Invoke();});
        sellBtn.onClick.AddListener(() => { OnShopSellClicked?.Invoke();});
        exitBtn.onClick.AddListener(() => { OnShopExitClicked?.Invoke();});
    }


    private void OnDestroy()
    {
        buyBtn.onClick.RemoveAllListeners();
        sellBtn.onClick.RemoveAllListeners();
        exitBtn.onClick.RemoveAllListeners();
    }   
}
