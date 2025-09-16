using System;
using UnityEngine;

namespace RougeLite.Events
{
    /// <summary>
    /// Base class for all game events
    /// Provides common functionality and ensures type safety
    /// </summary>
    public abstract class GameEvent
    {
        /// <summary>
        /// Timestamp when the event was created
        /// </summary>
        public float Timestamp { get; private set; }
        
        /// <summary>
        /// Optional GameObject that triggered this event
        /// </summary>
        public GameObject Source { get; private set; }

        protected GameEvent(GameObject source = null)
        {
            Timestamp = Time.time;
            Source = source;
        }

        /// <summary>
        /// Override to provide debug information about the event
        /// </summary>
        public virtual string GetDebugInfo()
        {
            return $"{GetType().Name} at {Timestamp:F2}s" + 
                   (Source != null ? $" from {Source.name}" : "");
        }
    }

    /// <summary>
    /// Generic base class for typed events
    /// Allows for strongly-typed event data
    /// </summary>
    /// <typeparam name="T">The event data type</typeparam>
    public abstract class GameEvent<T> : GameEvent
    {
        public T Data { get; private set; }

        protected GameEvent(T data, GameObject source = null) : base(source)
        {
            Data = data;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" with data: {Data}";
        }
    }
}