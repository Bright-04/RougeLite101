using UnityEngine;

public class ShopNPC : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ShopInventorySO shopData;
    [SerializeField]
    private ShopController shopController;

    private void Awake()
    {
        shopController = FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
    }

    public void Interact(GameObject interactor)
    {
        Debug.Log("INTERACT SHOP NPC");
        shopController.OpenShop(shopData, interactor);
    }

    public string GetInteractionText()
    {
        return "[F] Open Shop";
    }
}
