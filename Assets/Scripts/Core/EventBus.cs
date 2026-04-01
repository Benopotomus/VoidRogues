using System.Collections.Generic;

namespace VoidRogues.Core
{
    /// <summary>
    /// Lightweight global event bus for decoupled cross-system communication.
    /// Systems subscribe to typed events and receive them without holding direct
    /// references to unrelated managers or components.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<System.Type, System.Delegate> _handlers
            = new Dictionary<System.Type, System.Delegate>();

        /// <summary>Publish an event to all registered subscribers.</summary>
        public static void Publish<TEvent>(TEvent evt) where TEvent : struct
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var del))
            {
                ((System.Action<TEvent>)del)?.Invoke(evt);
            }
        }

        /// <summary>Subscribe to events of type <typeparamref name="TEvent"/>.</summary>
        public static void Subscribe<TEvent>(System.Action<TEvent> handler) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_handlers.TryGetValue(type, out var existing))
            {
                _handlers[type] = (System.Action<TEvent>)existing + handler;
            }
            else
            {
                _handlers[type] = handler;
            }
        }

        /// <summary>
        /// Unsubscribe from events of type <typeparamref name="TEvent"/>.
        /// Always call this in <c>OnDestroy</c> to prevent stale delegate leaks.
        /// </summary>
        public static void Unsubscribe<TEvent>(System.Action<TEvent> handler) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var updated = (System.Action<TEvent>)existing - handler;
                if (updated == null)
                    _handlers.Remove(type);
                else
                    _handlers[type] = updated;
            }
        }

        /// <summary>Clear all subscriptions. Call only in test teardown.</summary>
        public static void Clear() => _handlers.Clear();
    }
}
