using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PickUpSystem : MonoBehaviour
{
    [SerializeField] private InventoryController inventoryController;

    [SerializeField] private InventorySO inventoryData;

    private void Start()
    {
        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Item item = collision.GetComponent<Item>();
        if (item != null)
        {
            inventoryData = inventoryController.CurrentInventoryData;
            int reminder = inventoryData.AddItem(item.InventoryItem, item.Quantity);
            if (reminder == 0) 
            {
                item.DestroyItem();
            }
            else
            {
                item.Quantity = reminder;
            }
                
        }
    }
}
