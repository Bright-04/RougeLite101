using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Chest : MonoBehaviour
{
    [Header("Chest Settings")]
    [SerializeField] private GameObject pickableItemPrefab;
    [SerializeField] private ItemSO[] possibleItems;

    private bool playerInRange;
    private bool opened;
    private PlayerControls playerControls;
    private GameObject player;

    private void Start()
    {
        playerControls = InputManager.Instance.Controls;
        playerControls.Combat.Interact.performed += OnInteract;
    }

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.Combat.Interact.performed -= OnInteract;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            player = collision.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!playerInRange || opened)
            return;

        OpenChest();
    }

    private void OpenChest()
    {
        if (pickableItemPrefab == null || possibleItems == null || possibleItems.Length == 0)
            return;

        opened = true;

        ItemSO chosenItem = GetRandomItemByRarity(player);

        GameObject obj = Instantiate(
            pickableItemPrefab,
            transform.position,
            Quaternion.identity
        );

        Item item = obj.GetComponent<Item>();

        if (item != null)
        {
            item.InventoryItem = chosenItem;
            item.Quantity = 1;
        }

        Debug.Log($"Chest dropped item: {chosenItem.Name} | Rarity: {chosenItem.Rarity}");
    }

    private ItemSO GetRandomItemByRarity(GameObject player)
    {
        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        int rollCount = 1;

        if (playerStats != null)
        {
            rollCount += Mathf.FloorToInt(playerStats.GetLuck());
        }

        Rarity bestRarity = Rarity.Common;

        for (int i = 0; i < rollCount; i++)
        {
            Rarity rolledRarity = RollRarity();

            if (rolledRarity > bestRarity)
            {
                bestRarity = rolledRarity;
            }

            if (bestRarity >= Rarity.Epic)
            {
                break;
            }
        }

        List<ItemSO> candidates = new List<ItemSO>();

        foreach (ItemSO item in possibleItems)
        {
            if (item != null && item.Rarity == bestRarity)
            {
                candidates.Add(item);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("No item found for rarity: " + bestRarity);
            return possibleItems[Random.Range(0, possibleItems.Length)];
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private Rarity RollRarity()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 1f)
            return Rarity.Legendary;

        if (roll < 10f)
            return Rarity.Epic;

        if (roll < 30f)
            return Rarity.Rare;

        if (roll < 60f)
            return Rarity.Uncommon;

        return Rarity.Common;
    }
}