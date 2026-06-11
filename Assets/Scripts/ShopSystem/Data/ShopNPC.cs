using UnityEngine;

public class ShopNPC : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ShopInventorySO shopData;
    [SerializeField]
    private ShopController shopController; 

    public void Interact(GameObject interactor)
    {
        Debug.Log("INTERACT SHOP NPC: " + shopData.greeting);
        shopController = FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
        shopController.OpenShop(shopData, interactor);
    }

    public string GetInteractionText(GameObject interactor)
    {
        return $"[F] Open {shopData.shopName} Shop";
    }
}
