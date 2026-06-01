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
    //Bottom left
    [SerializeField] private DialogueUI dialoguePanel;
    [SerializeField] private GameObject itemListPanel;


    //bottom right
    [SerializeField] private ShopMainMenuUI mainMenuUI;
    [SerializeField] private GameObject itemDescriptionPanel;
    [SerializeField] private ShopItemDescriptionUI itemDescription;
    [SerializeField] private ShopSellPanelUI shopSellPanelUI;
    [SerializeField] private ShopBuyPanelUI shopBuyPanelUI;

    private ShopState currentState;
    private string shopGreetingTxt;

    public List<ShopItemUI> itemsList = new List<ShopItemUI>();

    public event Action<int> OnShopItemDescriptionRequested;
    public event Action<ShopState> OnInitiateShopItemList;
    public event Action OnClosingShop;

    private void Awake()
    {
        mainMenuUI.OnShopBuyClicked += HandleBuy;
        mainMenuUI.OnShopSellClicked += HandleSell;
        mainMenuUI.OnShopExitClicked += HandleExit;

        shopSellPanelUI.OnShopSellItemClicked += OnSellItemRequested;
        shopSellPanelUI.OnShopSellGoBackClicked += OnShopGoBackRequested;

        shopBuyPanelUI.OnShopBuyItemClicked += OnBuyItemRequested;
        shopBuyPanelUI.OnShopBuyGoBackClicked += OnShopGoBackRequested;


    }

    private void OnDestroy()
    {
        mainMenuUI.OnShopBuyClicked -= HandleBuy;
        mainMenuUI.OnShopSellClicked -= HandleSell;
        mainMenuUI.OnShopExitClicked -= HandleExit;

        shopSellPanelUI.OnShopSellItemClicked -= OnSellItemRequested;
        shopSellPanelUI.OnShopSellGoBackClicked -= OnShopGoBackRequested;

        shopBuyPanelUI.OnShopBuyItemClicked -= OnBuyItemRequested;
        shopBuyPanelUI.OnShopBuyGoBackClicked -= OnShopGoBackRequested;
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

    public void ResetAllItems()
    {
        foreach (var item in itemsList)
        {
            item.ResetData();
            item.Deselect();
        }
    }

    public void ResetSelection()
    {
        if (itemDescription != null)
        {
            itemDescription.ResetDescription();
        }
        DeselectAllItems();
    }

    private void DeselectAllItems()
    {
        foreach (ShopItemUI item in itemsList)
        {
            item.Deselect();
        }     
    }

    public void UpdateData(int itemIndex, Sprite itemImage, int itemCurrentStock, int itemMaxStock)
    {
        if (itemsList.Count > itemIndex)
        {
            itemsList[itemIndex].SetData(itemImage, itemCurrentStock, itemMaxStock);
        }
    }

    public void UpdateData(int itemIndex, Sprite itemImage, int itemQuantity)
    {
        if (itemsList.Count > itemIndex)
        {
            itemsList[itemIndex].SetData(itemImage, itemQuantity);
        }
    }

    public void UpdateDescription(int itemIndex, string name, string description)
    {
        itemDescription.SetDescription(name, description);
        DeselectAllItems();
        itemsList[itemIndex].Select();
    }

    private void OnSellItemRequested()
    {
        Debug.Log("OnSellItemRequested");
    }

    private void OnBuyItemRequested()
    {
        Debug.Log("OnBuyItemRequested");
    }

    private void OnShopGoBackRequested()
    {
        Debug.Log("return to shop Main menu");
        SetState(ShopState.MainMenu);
    }

    private void HandleBuy()
    {
        Debug.Log("BUY MENU");
        SetState(ShopState.Buy);
        OnInitiateShopItemList?.Invoke(ShopState.Buy);
    }

    private void HandleSell()
    {
        Debug.Log("SELL MENU");
        SetState(ShopState.Sell);
        OnInitiateShopItemList?.Invoke(ShopState.Sell);
    }

    private void HandleExit()
    {
        OnClosingShop?.Invoke();
    }

    public void ShowShop(ShopInventorySO shopData)
    {  
        gameObject.SetActive(true);     
        shopGreetingTxt = shopData.greeting;
        SetState(ShopState.MainMenu);
    }

    public void SetState(ShopState currentState)
    {
        this.currentState = currentState;

        switch(currentState)
        {
            case ShopState.MainMenu:
                if (dialoguePanel != null) dialoguePanel.Show(shopGreetingTxt);
                if (itemListPanel != null) itemListPanel.SetActive(false);
                if (itemDescription != null) itemDescription.ResetDescription();
                if (itemDescriptionPanel != null) itemDescriptionPanel.SetActive(false);
                if (mainMenuUI != null) mainMenuUI.Show();
                break;

            case ShopState.Buy:
                if (dialoguePanel != null) dialoguePanel.Hide();
                if (itemListPanel != null) itemListPanel.SetActive(true);
                if (itemDescriptionPanel != null) itemDescriptionPanel.SetActive(true);
                if (itemDescription != null) itemDescription.ResetDescription();
                if (shopBuyPanelUI != null) shopBuyPanelUI.Show();
                if (shopSellPanelUI != null) shopSellPanelUI.Hide();
                if (mainMenuUI != null) mainMenuUI.Hide();
                break;

            case ShopState.Sell:
                if (dialoguePanel != null) dialoguePanel.Hide();
                if (itemListPanel != null) itemListPanel.SetActive(true);
                if (itemDescriptionPanel != null) itemDescriptionPanel.SetActive(true);
                if (itemDescription != null) itemDescription.ResetDescription();
                if (shopBuyPanelUI != null) shopBuyPanelUI.Hide();
                if (shopSellPanelUI != null) shopSellPanelUI.Show();
                if (mainMenuUI != null) mainMenuUI.Hide();
                break;

        }
        Debug.Log("Current Shop state: " + currentState);
    }

    public void HideShop()
    {      
        gameObject.SetActive(false);
        ClearInventoryUI();
    }
}
