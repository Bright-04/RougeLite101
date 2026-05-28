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
            if (nearbyItem != null && nearbyItem != item)
            {
                nearbyItem.ShowPrompt(false);
            }

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

        ItemSO inventoryItem = nearbyItem.InventoryItem;
        if (inventoryItem == null)
        {
            Debug.LogWarning($"PickUpSystem: Pickup '{nearbyItem.gameObject.name}' has no InventoryItem assigned.", nearbyItem);
            return;
        }

        if (nearbyItem.Quantity <= 0)
        {
            Debug.LogWarning(
                $"PickUpSystem: Pickup '{nearbyItem.gameObject.name}' for item '{inventoryItem.name}' has invalid Quantity {nearbyItem.Quantity}.",
                nearbyItem);
            return;
        }

        int effectiveQuantity = nearbyItem.Quantity;
        if (inventoryItem.IsStackable == false && effectiveQuantity > 1)
        {
            // Normal world Item pickups represent a single world pickup. Future bundle/chest rewards
            // should use a separate reward container system instead of non-stackable Item.Quantity > 1.
            Debug.LogWarning(
                $"PickUpSystem: Pickup '{nearbyItem.gameObject.name}' authored non-stackable item '{inventoryItem.name}' with Quantity {nearbyItem.Quantity}; clamping pickup to 1.",
                nearbyItem);
            effectiveQuantity = 1;
        }

        // World weapons use the same generic item pickup path as any other item and enter InventorySO first.
        int reminder = inventoryData.AddItem(inventoryItem, effectiveQuantity);
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
