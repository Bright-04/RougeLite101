using UnityEngine;
using UnityEngine.InputSystem;

public class PickUpSystem : MonoBehaviour
{
    [SerializeField]
    private InventoryController inventoryController;

    [SerializeField]
    private InventorySO inventoryData;

    private Item nearbyItem;
    private InputAction interactAction;

    private void Start()
    {
        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }

        if (InputManager.Instance == null)
        {
            Debug.LogWarning("PickUpSystem: InputManager not found.", this);
            return;
        }

        interactAction = InputManager.Instance.Controls.asset.FindAction("Combat/Interact");
        if (interactAction != null)
        {
            interactAction.performed += OnInteractPerformed;
        }
    }

    private void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
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
        if (item == null || item != nearbyItem)
        {
            return;
        }

        nearbyItem.ShowPrompt(false);
        nearbyItem = null;
    }

    private void OnInteractPerformed(InputAction.CallbackContext _)
    {
        if (nearbyItem == null)
        {
            return;
        }

        if (inventoryController != null && inventoryController.CurrentInventoryData != null)
        {
            inventoryData = inventoryController.CurrentInventoryData;
        }

        if (inventoryData == null)
        {
            return;
        }

        int reminder = inventoryData.AddItem(nearbyItem.InventoryItem, nearbyItem.Quantity);
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
