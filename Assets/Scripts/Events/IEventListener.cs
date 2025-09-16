namespace RougeLite.Events
{
    /// <summary>
    /// Base interface for all event listeners
    /// </summary>
    public interface IEventListener
    {
        // Marker interface for type safety
    }

    /// <summary>
    /// Generic interface for typed event listeners
    /// Implement this interface to receive specific event types
    /// </summary>
    /// <typeparam name="T">The type of event this listener handles</typeparam>
    public interface IEventListener<in T> : IEventListener where T : GameEvent
    {
        /// <summary>
        /// Called when an event of type T is received
        /// </summary>
        /// <param name="eventData">The event data</param>
        void OnEventReceived(T eventData);
    }
}