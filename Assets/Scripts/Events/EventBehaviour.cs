using UnityEngine;

namespace RougeLite.Events
{
    /// <summary>
    /// Helper component that provides easy access to the event system
    /// Can be inherited by MonoBehaviours that need to send/receive events
    /// </summary>
    public abstract class EventBehaviour : MonoBehaviour
    {
        [Header("Event System")]
        [SerializeField] private bool autoCreateEventManager = true;
        /// <summary>
        /// Reference to the event manager (cached for performance)
        /// </summary>
        protected EventManager eventManager;

        protected virtual void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                autoCreateEventManager = false;
            }
            // Cache the event manager reference
            eventManager = EventManager.Instance;
            
            // If EventManager doesn't exist, try to find it
            if (eventManager == null)
            {
                eventManager = FindFirstObjectByType<EventManager>();
                
                if (eventManager == null)
                {
                    if (autoCreateEventManager)
                    {
                        Debug.LogWarning($"{GetType().Name}: EventManager not found! Creating one automatically.", this);
                        CreateEventManager();
                    }
                    else
                    {
                        Debug.LogError($"{GetType().Name}: EventManager not found and auto-create is disabled. Please add an EventManager to the scene.", this);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an EventManager if one doesn't exist
        /// </summary>
        private void CreateEventManager()
        {
            var eventManagerGO = new GameObject("EventManager");
            eventManager = eventManagerGO.AddComponent<EventManager>();
        }

        /// <summary>
        /// Convenience method to broadcast an event
        /// </summary>
        /// <typeparam name="T">Type of event</typeparam>
        /// <param name="eventInstance">Event to broadcast</param>
        protected void BroadcastEvent<T>(T eventInstance) where T : GameEvent
        {
            if (eventManager != null)
            {
                eventManager.Broadcast(eventInstance);
            }
            else
            {
                Debug.LogError($"{GetType().Name}: Cannot broadcast event - EventManager is null!", this);
            }
        }

        /// <summary>
        /// Convenience method to queue an event
        /// </summary>
        /// <typeparam name="T">Type of event</typeparam>
        /// <param name="eventInstance">Event to queue</param>
        protected void QueueEvent<T>(T eventInstance) where T : GameEvent
        {
            if (eventManager != null)
            {
                eventManager.QueueEvent(eventInstance);
            }
            else
            {
                Debug.LogError($"{GetType().Name}: Cannot queue event - EventManager is null!", this);
            }
        }

        /// <summary>
        /// Convenience method to subscribe to events
        /// </summary>
        /// <typeparam name="T">Type of event to listen for</typeparam>
        /// <param name="listener">Listener to register</param>
        protected void SubscribeToEvent<T>(IEventListener<T> listener) where T : GameEvent
        {
            if (eventManager != null)
            {
                eventManager.Subscribe(listener);
            }
            else
            {
                Debug.LogError($"{GetType().Name}: Cannot subscribe to event - EventManager is null!", this);
            }
        }

        /// <summary>
        /// Convenience method to unsubscribe from events
        /// </summary>
        /// <typeparam name="T">Type of event to stop listening for</typeparam>
        /// <param name="listener">Listener to unregister</param>
        protected void UnsubscribeFromEvent<T>(IEventListener<T> listener) where T : GameEvent
        {
            if (eventManager != null)
            {
                eventManager.Unsubscribe(listener);
            }
        }

        /// <summary>
        /// Convenience method to register for events using Action delegates
        /// This provides a simpler way to listen for events without implementing interfaces
        /// </summary>
        /// <typeparam name="T">Type of event to listen for</typeparam>
        /// <param name="callback">Action to call when event is received</param>
        protected void RegisterForEvent<T>(System.Action<T> callback) where T : GameEvent
        {
            if (eventManager != null)
            {
                eventManager.RegisterAction(callback);
            }
            else
            {
                Debug.LogError($"{GetType().Name}: Cannot register for event - EventManager is null!", this);
            }
        }

        /// <summary>
        /// Convenience method to unregister from events using Action delegates
        /// </summary>
        /// <typeparam name="T">Type of event to stop listening for</typeparam>
        /// <param name="callback">Action to unregister</param>
        protected void UnregisterFromEvent<T>(System.Action<T> callback) where T : GameEvent
        {
            if (eventManager != null)
            {
                eventManager.UnregisterAction(callback);
            }
        }

        /// <summary>
        /// Called when this object is destroyed - override to handle cleanup
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Override in derived classes to unsubscribe from events
        }
    }

    /// <summary>
    /// Simple event listener component that can be configured in the Inspector
    /// Useful for prototyping and simple event handling
    /// </summary>
    public class SimpleEventListener : EventBehaviour, 
        IEventListener<PlayerDamagedEvent>,
        IEventListener<PlayerDeathEvent>,
        IEventListener<EnemyDeathEvent>,
        IEventListener<SpellCastEvent>
    {
        [Header("Event Response Settings")]
        [SerializeField] private bool logPlayerDamage = true;
        [SerializeField] private bool logPlayerDeath = true;
        [SerializeField] private bool logEnemyDeath = true;
        [SerializeField] private bool logSpellCast = true;

        protected override void Awake()
        {
            base.Awake();
            
            // Subscribe to events
            SubscribeToEvent<PlayerDamagedEvent>(this);
            SubscribeToEvent<PlayerDeathEvent>(this);
            SubscribeToEvent<EnemyDeathEvent>(this);
            SubscribeToEvent<SpellCastEvent>(this);
        }

        protected override void OnDestroy()
        {
            // Unsubscribe from events
            UnsubscribeFromEvent<PlayerDamagedEvent>(this);
            UnsubscribeFromEvent<PlayerDeathEvent>(this);
            UnsubscribeFromEvent<EnemyDeathEvent>(this);
            UnsubscribeFromEvent<SpellCastEvent>(this);
            
            base.OnDestroy();
        }

        public void OnEventReceived(PlayerDamagedEvent eventData)
        {
            if (logPlayerDamage)
            {
                Debug.Log($"Player took {eventData.Data.damage} damage! Health: {eventData.Data.currentHealth}/{eventData.Data.maxHealth}", this);
            }
        }

        public void OnEventReceived(PlayerDeathEvent eventData)
        {
            if (logPlayerDeath)
            {
                Debug.Log("Player has died!", this);
            }
        }

        public void OnEventReceived(EnemyDeathEvent eventData)
        {
            if (logEnemyDeath)
            {
                Debug.Log($"Enemy {eventData.Data.enemyType} has been defeated!", this);
            }
        }

        public void OnEventReceived(SpellCastEvent eventData)
        {
            if (logSpellCast)
            {
                Debug.Log($"Spell '{eventData.Data.spellName}' cast by {eventData.Data.caster.name}!", this);
            }
        }
    }
}
