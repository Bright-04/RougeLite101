using UnityEngine;
using RougeLite.Events;

namespace RougeLite.Examples
{
    /// <summary>
    /// Simple test script to verify the Action-based event registration works correctly
    /// This demonstrates the fixed RegisterForEvent functionality
    /// </summary>
    public class EventRegistrationTest : EventBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTests = true;
        [SerializeField] private float testDelay = 1f;

        private void Start()
        {
            if (runTests)
            {
                TestEventRegistration();
                InvokeRepeating(nameof(FireTestEvent), testDelay, testDelay * 2f);
            }
        }

        private void TestEventRegistration()
        {
            // Test the fixed RegisterForEvent method
            RegisterForEvent<PlayerDamagedEvent>(OnPlayerDamaged);
            RegisterForEvent<PlayerJumpEvent>(OnPlayerJump);
            RegisterForEvent<LootDroppedEvent>(OnLootDropped);
            
            Debug.Log("EventRegistrationTest: Successfully registered for events using Action delegates");
        }

        protected override void OnDestroy()
        {
            if (runTests)
            {
                // Test unregistration
                UnregisterFromEvent<PlayerDamagedEvent>(OnPlayerDamaged);
                UnregisterFromEvent<PlayerJumpEvent>(OnPlayerJump);
                UnregisterFromEvent<LootDroppedEvent>(OnLootDropped);
                
                Debug.Log("EventRegistrationTest: Successfully unregistered from events");
            }
            
            base.OnDestroy();
        }

        private void FireTestEvent()
        {
            if (!runTests) return;

            // Fire a test event to verify listeners are working
            var healthData = new PlayerHealthData(75f, 100f, 25f, gameObject);
            BroadcastEvent(new PlayerDamagedEvent(healthData, gameObject));

            Debug.Log("EventRegistrationTest: Fired test PlayerDamagedEvent");
        }

        // Event handler methods (using Action delegates)
        private void OnPlayerDamaged(PlayerDamagedEvent eventData)
        {
            Debug.Log($"EventRegistrationTest: Received PlayerDamagedEvent - Damage: {eventData.Data.damage}, Health: {eventData.Data.currentHealth}/{eventData.Data.maxHealth}");
        }

        private void OnPlayerJump(PlayerJumpEvent eventData)
        {
            Debug.Log($"EventRegistrationTest: Received PlayerJumpEvent - Height: {eventData.JumpHeight}");
        }

        private void OnLootDropped(LootDroppedEvent eventData)
        {
            Debug.Log($"EventRegistrationTest: Received LootDroppedEvent - {eventData.Data.itemName} ({eventData.Data.rarity})");
        }

        // Context menu methods for manual testing
        [ContextMenu("Test Player Damage")]
        private void TestPlayerDamage()
        {
            var healthData = new PlayerHealthData(50f, 100f, 30f, gameObject);
            BroadcastEvent(new PlayerDamagedEvent(healthData, gameObject));
        }

        [ContextMenu("Test Player Jump")]
        private void TestPlayerJump()
        {
            BroadcastEvent(new PlayerJumpEvent(8f, true, gameObject));
        }

        [ContextMenu("Test Loot Drop")]
        private void TestLootDrop()
        {
            var lootData = new LootData
            {
                itemName = "Test Sword",
                rarity = "Epic",
                quantity = 1,
                dropPosition = transform.position,
                dropChance = 1f,
                droppedBy = "Test Enemy"
            };
            BroadcastEvent(new LootDroppedEvent(lootData, gameObject));
        }
    }
}