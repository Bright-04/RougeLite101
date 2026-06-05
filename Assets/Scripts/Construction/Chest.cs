using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Chest : MonoBehaviour
{
    [Header("Chest Settings")]
    [SerializeField] private GameObject pickableItemPrefab;
    [SerializeField] private ItemSO[] possibleItems;

    [Header("Chest Tier")]
    [SerializeField] private ChestTier chestTier = ChestTier.Tier1;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private float dropDelay = 0.4f;

    private bool playerInRange;
    private bool opened;
    private PlayerControls playerControls;
    private GameObject player;


    private void Start()
    {
        playerControls = InputManager.Instance.Controls;
        playerControls.Combat.Interact.performed += OnInteract;
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        Debug.Log("Chest animator = " + animator);
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

        StartCoroutine(OpenChestRoutine());
    }

    private void OpenChest()
    {
        if (pickableItemPrefab == null || possibleItems == null || possibleItems.Length == 0)
            return;


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

        switch (chestTier)
        {
            case ChestTier.Tier1:
                // Legendary 0.5%, Epic 4.5%, Rare 15%, Uncommon 30%, Common 50%
                if (roll < 0.5f) return Rarity.Legendary;
                if (roll < 5f) return Rarity.Epic;
                if (roll < 20f) return Rarity.Rare;
                if (roll < 50f) return Rarity.Uncommon;
                return Rarity.Common;

            case ChestTier.Tier2:
                // Legendary 1%, Epic 9%, Rare 20%, Uncommon 30%, Common 40%
                if (roll < 1f) return Rarity.Legendary;
                if (roll < 10f) return Rarity.Epic;
                if (roll < 30f) return Rarity.Rare;
                if (roll < 60f) return Rarity.Uncommon;
                return Rarity.Common;

            case ChestTier.Tier3:
                // Legendary 3%, Epic 17%, Rare 30%, Uncommon 30%, Common 20%
                if (roll < 3f) return Rarity.Legendary;
                if (roll < 20f) return Rarity.Epic;
                if (roll < 50f) return Rarity.Rare;
                if (roll < 80f) return Rarity.Uncommon;
                return Rarity.Common;
        }

        return Rarity.Common;
    }

    private IEnumerator OpenChestRoutine()
    {
        opened = true;

        if (animator != null)
        {
            animator.SetTrigger(openTriggerName);
        }

        yield return new WaitForSeconds(dropDelay);

        OpenChest();
    }
}