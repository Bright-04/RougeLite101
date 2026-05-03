using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    private InventoryItemUI itemPrefab;

    [SerializeField]
    private RectTransform contentPanel;

    List<InventoryItemUI> itemsList = new List<InventoryItemUI>(); //list of  item   

    private bool inventoryActive = false;
    public bool IsInventoryActive() => inventoryActive;

    [SerializeField]
    private InventoryDescriptionUI itemDescription;

    public event Action<int> OnDescriptionRequested,
                OnItemActionRequested,
                OnStartDragging;

    [SerializeField]
    private MouseFollower mouseFollower;

    private int currentlyDraggedItemIndex = -1;

    public event Action<int, int> OnSwapItems;

    //[SerializeField]
    //private ItemActionPanel actionPanel;

    private void Awake()
    {
        //HideInventory();
        mouseFollower.Toggle(false);
        itemDescription.ResetDescription();
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

    public void InitializedInventoryUI(int inventorySize)
    {     
        for (int i = 0; i < inventorySize; i++)
        {
            InventoryItemUI itemUI = Instantiate(itemPrefab);
            itemUI.transform.SetParent(contentPanel, false);
            itemsList.Add(itemUI);
            itemUI.OnItemClicked += HandleItemSelection;
            itemUI.OnItemBeginDrag += HandleBeginDrag;
            itemUI.OnItemDroppedOn += HandleSwap;
            itemUI.OnItemEndDrag += HandleEndDrag;
            itemUI.OnRightMouseBtnClick += HandleShowItemActions;
        }

    }

    internal void ResetAllItems()
    {
        foreach (var item in itemsList)
        {
            item.ResetData();
            item.Deselect();
        }
    }

    internal void UpdateDescription(int itemIndex, Sprite itemImage, string name, string description)
    {
        itemDescription.SetDescription(itemImage, name, description);
        DeselectAllItems();
        itemsList[itemIndex].Select();
    }

    public void UpdateData(int itemIndex,
        Sprite itemImage, int itemQuantity)
    {
        if (itemsList.Count > itemIndex)
        {
            itemsList[itemIndex].SetData(itemImage, itemQuantity);
        }
    }

    private void HandleShowItemActions(InventoryItemUI inventoryItemUI)
    {
        int index = itemsList.IndexOf(inventoryItemUI);
        if (index == -1)
        {
            return;
        }
        OnItemActionRequested?.Invoke(index);
        
    }

    private void HandleEndDrag(InventoryItemUI inventoryItemUI)
    {
        ResetDraggedItem();
    }

    private void HandleSwap(InventoryItemUI inventoryItemUI)
    {
        int index = itemsList.IndexOf(inventoryItemUI);
        if (index == -1)
        {         
            return;
        }
        OnSwapItems?.Invoke(currentlyDraggedItemIndex, index);
        HandleItemSelection(inventoryItemUI);    
    }

    private void ResetDraggedItem()
    {
        mouseFollower.Toggle(false);
        currentlyDraggedItemIndex = -1;
    }

    private void HandleBeginDrag(InventoryItemUI inventoryItemUI)
    {
        int index = itemsList.IndexOf(inventoryItemUI);
        if (index == -1)
        {
            return;
        }          
        currentlyDraggedItemIndex = index;
        HandleItemSelection(inventoryItemUI);
        OnStartDragging?.Invoke(index);     
    }

    public void CreateDraggedItem(Sprite sprite, int quantity)
    {
        mouseFollower.Toggle(true);
        mouseFollower.SetData(sprite, quantity);
    }

    private void HandleItemSelection(InventoryItemUI inventoryItemUI)
    {
        int index = itemsList.IndexOf(inventoryItemUI);
        if (index == -1)
        {
            return;
        }      
        OnDescriptionRequested?.Invoke(index);
    }

    public void ShowInventory()
    {
        inventoryActive = true;
        itemDescription.ResetDescription();
        gameObject.SetActive(true);
        ResetSelection();


    }

    public void HideInventory()
    {
        inventoryActive = false;
        gameObject.SetActive(false);
    }

    public void ResetSelection()
    {
        itemDescription.ResetDescription();
        DeselectAllItems();
    }

    //public void AddAction(string actionName, Action performAction)
    //{
    //    actionPanel.AddButon(actionName, performAction);
    //}

    //public void ShowItemAction(int itemIndex)
    //{
    //    actionPanel.Toggle(true);
    //    actionPanel.transform.position = listOfUIItems[itemIndex].transform.position;
    //}

    private void DeselectAllItems()
    {
        foreach (InventoryItemUI item in itemsList)
        {
            item.Deselect();
        }
        //actionPanel.Toggle(false);
    }



}
