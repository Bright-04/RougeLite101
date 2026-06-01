using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class ShopUI : MonoBehaviour
{
    [SerializeField]
    private ShopItemUI itemPrefab;

    [SerializeField]
    private RectTransform contentPanel;
    //Bottom right
    [SerializeField]
    private ShopMainMenuUI mainMenuUI;

    public List<ShopItemUI> itemsList = new List<ShopItemUI>();

    public event Action<int> OnShopItemDescriptionRequested;
    public event Action OnClosingShop;

    private void Awake()
    {
        mainMenuUI.OnShopBuyClicked += HandleBuy;
        mainMenuUI.OnShopSellClicked += HandleSell;
        mainMenuUI.OnShopExitClicked += HandleExit;
    }

    private void OnDestroy()
    {
        mainMenuUI.OnShopBuyClicked -= HandleBuy;
        mainMenuUI.OnShopSellClicked -= HandleSell;
        mainMenuUI.OnShopExitClicked -= HandleExit;
    }

    public void ClearInventoryUI()
    {
        foreach (var item in itemsList)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }

        }
        itemsList.Clear();
    }

    public void InitializedShopInventoryUI(int shopInventorySize)
    {
        for (int i = 0; i < shopInventorySize; i++)
        {
            ShopItemUI itemUI = Instantiate(itemPrefab);
            itemUI.transform.SetParent(contentPanel, false);
            itemsList.Add(itemUI);
            itemUI.OnShopItemClicked += HandleItemSelection;          
        }

    }

    private void HandleItemSelection(ShopItemUI shopItemUI)
    {
        int index = itemsList.IndexOf(shopItemUI);
        if (index == -1)
        {
            return;
        }
        OnShopItemDescriptionRequested?.Invoke(index);
    }

    private void HandleBuy()
    {
        Debug.Log("BUY MENU");
    }

    private void HandleSell()
    {
        Debug.Log("SELL MENU");
    }

    private void HandleExit()
    {
        OnClosingShop?.Invoke();
    }

    public void ShowShop()
    {       
        gameObject.SetActive(true);
    }

    public void HideShop()
    {      
        gameObject.SetActive(false);
    }
}
