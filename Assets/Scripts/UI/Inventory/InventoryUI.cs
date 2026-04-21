using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    private InventoryItemUI itemPrefab;

    [SerializeField]
    private RectTransform contentPanel;

    List<InventoryItemUI> listOfItems = new List<InventoryItemUI>(); //list of item

    private bool inventoryActive = false;
    public bool IsInventoryActive() => inventoryActive;

    public void InitializedInventoryUI(int inventorySize)
    {
        for (int i = 0; i< inventorySize; i++)
        {
            InventoryItemUI itemUI = Instantiate(itemPrefab);
            itemUI.transform.SetParent(contentPanel, false);
            listOfItems.Add(itemUI);
        }
    }

    public void ShowInventory()
    {
        inventoryActive = true;
        gameObject.SetActive(true);
    }

    public void HideInventory()
    {
        inventoryActive = false;
        gameObject.SetActive(false);
    }



}
