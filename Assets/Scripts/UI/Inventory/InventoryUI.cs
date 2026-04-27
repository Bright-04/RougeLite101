using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using static UnityEditor.Progress;

public enum InventoryType
{
    Safe,
    Dungeon
}

public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    private InventoryItemUI itemPrefab;

    [SerializeField]
    private RectTransform contentPanel;

    List<InventoryItemUI> safeItemsList = new List<InventoryItemUI>(); //list of safe item
    List<InventoryItemUI> dungeonItemsList = new List<InventoryItemUI>(); //list of dungeon item

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
    public void ShowInventory(InventoryType type)
    {
        ClearUI();

        List<InventoryItemUI> targetList = (type == InventoryType.Safe)
            ? safeItemsList
            : dungeonItemsList;

        foreach (var item in targetList)
        {
            InventoryItemUI itemUI = Instantiate(itemPrefab, contentPanel);
            itemUI.transform.SetParent(contentPanel, false);
            safeItemsList.Add(itemUI);

            itemUI.OnItemClicked += HandleItemSelection;
            itemUI.OnItemBeginDrag += HandleBeginDrag;
            itemUI.OnItemDroppedOn += HandleSwap;
            itemUI.OnItemEndDrag += HandleEndDrag;
            itemUI.OnRightMouseBtnClick += HandleShowItemActions;
        }
    }

    void ClearUI()
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
    }

    public void InitializedInventoryUI(int safeInventorySize, int dungeonInventorySize)
    {
        for (int i = 0; i < safeInventorySize; i++)
        {

            InventoryItemUI itemUI = Instantiate(itemPrefab);
            itemUI.transform.SetParent(contentPanel, false);
            safeItemsList.Add(itemUI);
            itemUI.OnItemClicked += HandleItemSelection;
            itemUI.OnItemBeginDrag += HandleBeginDrag;
            itemUI.OnItemDroppedOn += HandleSwap;
            itemUI.OnItemEndDrag += HandleEndDrag;
            itemUI.OnRightMouseBtnClick += HandleShowItemActions;
        }



    }

    internal void ResetAllItems()
    {
        foreach (var item in safeItemsList)
        {
            item.ResetData();
            item.Deselect();
        }
    }

    

    public void UpdateData(int itemIndex,
        Sprite itemImage, int itemQuantity)
    {
        if (safeItemsList.Count > itemIndex)
        {
            safeItemsList[itemIndex].SetData(itemImage, itemQuantity);
        }
    }

    private void HandleShowItemActions(InventoryItemUI inventoryItemUI)
    {
        int index = safeItemsList.IndexOf(inventoryItemUI);
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
        int index = safeItemsList.IndexOf(inventoryItemUI);
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
        int index = safeItemsList.IndexOf(inventoryItemUI);
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
        int index = safeItemsList.IndexOf(inventoryItemUI);
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
        foreach (InventoryItemUI item in safeItemsList)
        {
            item.Deselect();
        }
        //actionPanel.Toggle(false);
    }



}
