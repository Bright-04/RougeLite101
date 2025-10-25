using System;
using System.Collections.Generic;
using UnityEngine;

namespace RougeLite.Events
{
    /// <summary>
    /// Centralized event manager for handling all game events
    /// Implements the Observer pattern for loose coupling between systems
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        // Dictionary to store event listeners by event type
        private readonly Dictionary<Type, List<IEventListener>> eventListeners = 
            new Dictionary<Type, List<IEventListener>>();

        // Queue for events that should be processed next frame (optional buffering)
        // Uses typed dispatch wrappers to avoid reflection overhead
        private readonly Queue<IQueuedEvent> eventQueue = new Queue<IQueuedEvent>();

        [Header("Debug Settings")]
        [SerializeField] private bool logEvents = false;
        [SerializeField] private bool logSubscriptions = false;

        private void Awake()
        {
            // Singleton pattern with proper cleanup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (logSubscriptions)
                {
                    Debug.Log("EventManager: Initialized successfully.", this);
                }
            }
            else
            {
                Debug.LogWarning("EventManager: Multiple instances detected! Destroying duplicate.", this);
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Process queued events (if using buffered events)
            ProcessQueuedEvents();
        }

        /// <summary>
        /// Subscribe to events of a specific type
        /// </summary>
        /// <typeparam name="T">Event type to listen for</typeparam>
        /// <param name="listener">The listener to register</param>
        public void Subscribe<T>(IEventListener<T> listener) where T : GameEvent
        {
            var eventType = typeof(T);
            
            if (!eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = new List<IEventListener>();
            }

            if (!eventListeners[eventType].Contains(listener))
            {
                eventListeners[eventType].Add(listener);
                
                if (logSubscriptions)
                {
                    Debug.Log($"EventManager: {listener.GetType().Name} subscribed to {eventType.Name}", this);
                }
            }
            else
            {
                Debug.LogWarning($"EventManager: {listener.GetType().Name} tried to subscribe to {eventType.Name} multiple times!", this);
            }
        }

        /// <summary>
        /// Unsubscribe from events of a specific type
        /// </summary>
        /// <typeparam name="T">Event type to stop listening for</typeparam>
        /// <param name="listener">The listener to unregister</param>
        public void Unsubscribe<T>(IEventListener<T> listener) where T : GameEvent
        {
            var eventType = typeof(T);
            
            if (eventListeners.ContainsKey(eventType))
            {
                if (eventListeners[eventType].Remove(listener))
                {
                    if (logSubscriptions)
                    {
                        Debug.Log($"EventManager: {listener.GetType().Name} unsubscribed from {eventType.Name}", this);
                    }
                }
                
                // Clean up empty lists
                if (eventListeners[eventType].Count == 0)
                {
                    eventListeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Register an Action delegate to listen for events (simpler than implementing interfaces)
        /// </summary>
        /// <typeparam name="T">Type of event to listen for</typeparam>
        /// <param name="callback">Action to call when event is received</param>
        public void RegisterAction<T>(System.Action<T> callback) where T : GameEvent
        {
            if (callback == null)
            {
                Debug.LogError("EventManager: Cannot register null callback!", this);
                return;
            }

            // Create a wrapper that implements IEventListener
            var wrapper = new ActionEventListener<T>(callback);
            Subscribe<T>(wrapper);
        }

        /// <summary>
        /// Unregister an Action delegate from listening for events
        /// </summary>
        /// <typeparam name="T">Type of event to stop listening for</typeparam>
        /// <param name="callback">Action to unregister</param>
        public void UnregisterAction<T>(System.Action<T> callback) where T : GameEvent
        {
            if (callback == null) return;

            var eventType = typeof(T);
            if (!eventListeners.ContainsKey(eventType)) return;

            // Find and remove the wrapper for this callback
            var listenersToRemove = new List<IEventListener>();
            foreach (var listener in eventListeners[eventType])
            {
                if (listener is ActionEventListener<T> wrapper && wrapper.Callback == callback)
                {
                    listenersToRemove.Add(listener);
                }
            }

            foreach (var listener in listenersToRemove)
            {
                eventListeners[eventType].Remove(listener);
            }

            // Clean up empty lists
            if (eventListeners[eventType].Count == 0)
            {
                eventListeners.Remove(eventType);
            }
        }

        /// <summary>
        /// Immediately broadcast an event to all registered listeners
        /// </summary>
        /// <typeparam name="T">Type of event to broadcast</typeparam>
        /// <param name="eventInstance">The event instance to broadcast</param>
        public void Broadcast<T>(T eventInstance) where T : GameEvent
        {
            if (eventInstance == null)
            {
                Debug.LogError("EventManager: Attempted to broadcast null event!", this);
                return;
            }

            var eventType = typeof(T);
            
            if (logEvents)
            {
                Debug.Log($"EventManager: Broadcasting {eventInstance.GetDebugInfo()}", this);
            }

            if (eventListeners.ContainsKey(eventType))
            {
                // Create a copy of the list to avoid modification during iteration
                var listeners = new List<IEventListener>(eventListeners[eventType]);
                
                foreach (var listener in listeners)
                {
                    try
                    {
                        if (listener != null && listener is IEventListener<T> typedListener)
                        {
                            typedListener.OnEventReceived(eventInstance);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EventManager: Error in event listener {listener?.GetType().Name}: {ex.Message}", this);
                    }
                }
            }
            else
            {
                if (logEvents)
                {
                    Debug.Log($"EventManager: No listeners for {eventType.Name}", this);
                }
            }
        }

        /// <summary>
        /// Queue an event to be processed next frame (useful for avoiding immediate recursion)
        /// </summary>
        /// <typeparam name="T">Type of event to queue</typeparam>
        /// <param name="eventInstance">The event instance to queue</param>
        public void QueueEvent<T>(T eventInstance) where T : GameEvent
        {
            if (eventInstance == null)
            {
                Debug.LogError("EventManager: Attempted to queue null event!", this);
                return;
            }

            eventQueue.Enqueue(new QueuedEvent<T>(eventInstance));
            
            if (logEvents)
            {
                Debug.Log($"EventManager: Queued {eventInstance.GetDebugInfo()}", this);
            }
        }

        /// <summary>
        /// Process all queued events
        /// </summary>
        private void ProcessQueuedEvents()
        {
            while (eventQueue.Count > 0)
            {
                var queued = eventQueue.Dequeue();
                queued?.Dispatch(this);
            }
        }

        /// <summary>
        /// Get the number of listeners for a specific event type (useful for debugging)
        /// </summary>
        /// <typeparam name="T">Event type to check</typeparam>
        /// <returns>Number of registered listeners</returns>
        public int GetListenerCount<T>() where T : GameEvent
        {
            var eventType = typeof(T);
            return eventListeners.ContainsKey(eventType) ? eventListeners[eventType].Count : 0;
        }

        /// <summary>
        /// Clear all event listeners (useful for scene transitions)
        /// </summary>
        public void ClearAllListeners()
        {
            eventListeners.Clear();
            eventQueue.Clear();
            
            if (logSubscriptions)
            {
                Debug.Log("EventManager: Cleared all event listeners", this);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Debug method to log all current subscriptions
        /// </summary>
        [ContextMenu("Debug Log All Subscriptions")]
        public void DebugLogAllSubscriptions()
        {
            Debug.Log("=== EventManager Subscriptions ===", this);
            
            if (eventListeners.Count == 0)
            {
                Debug.Log("No event subscriptions", this);
                return;
            }

            foreach (var kvp in eventListeners)
            {
                Debug.Log($"{kvp.Key.Name}: {kvp.Value.Count} listeners", this);
                foreach (var listener in kvp.Value)
                {
                    Debug.Log($"  - {listener.GetType().Name}", this);
                }
            }
        }
    }

    /// <summary>
    /// Internal interface for queued events to enable non-reflection dispatch
    /// </summary>
    internal interface IQueuedEvent
    {
        void Dispatch(EventManager manager);
    }

    /// <summary>
    /// Generic queued event wrapper that calls the strongly-typed Broadcast
    /// </summary>
    internal class QueuedEvent<T> : IQueuedEvent where T : GameEvent
    {
        private readonly T _event;

        public QueuedEvent(T gameEvent)
        {
            _event = gameEvent;
        }

        public void Dispatch(EventManager manager)
        {
            manager.Broadcast(_event);
        }
    }
 
    /// <summary>
    /// Internal wrapper class to allow Action delegates to work with the event system
    /// </summary>
    internal class ActionEventListener<T> : IEventListener<T> where T : GameEvent
    {
        public System.Action<T> Callback { get; private set; }

        public ActionEventListener(System.Action<T> callback)
        {
            Callback = callback;
        }

        public void OnEventReceived(T eventData)
        {
            Callback?.Invoke(eventData);
        }
    }
}
