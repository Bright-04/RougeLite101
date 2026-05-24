using UnityEngine;
using UnityEngine.InputSystem;

public class PickUpSystem : MonoBehaviour
{
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private InventorySO inventoryData;

    private Item nearbyItem;
    private PlayerControls playerControls;

    private void Start()
    {
        if (inventoryController == null)
            inventoryController = GetComponent<InventoryController>();

        playerControls = InputManager.Instance.Controls;

        playerControls.NavigateUI.Pickup.performed += OnPickupPerformed;
    }

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.NavigateUI.Pickup.performed -= OnPickupPerformed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Item item = collision.GetComponent<Item>();

        if (item != null)
        {
            nearbyItem = item;

            nearbyItem.ShowPrompt(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Item item = collision.GetComponent<Item>();

        if (item != null && item == nearbyItem)
        {
            nearbyItem.ShowPrompt(false);
            nearbyItem = null;
        }
    }

    private void OnPickupPerformed(InputAction.CallbackContext ctx)
    {
        if (nearbyItem == null)
            return;

        inventoryData = inventoryController.CurrentInventoryData;

        int reminder = inventoryData.AddItem(
            nearbyItem.InventoryItem,
            nearbyItem.Quantity
        );

        if (reminder == 0)
        {
            nearbyItem.ShowPrompt(false);
            nearbyItem.DestroyItem();
            nearbyItem = null;
        }
        else
        {
            nearbyItem.Quantity = reminder;
        }
    }
}