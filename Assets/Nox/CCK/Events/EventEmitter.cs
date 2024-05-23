namespace Nox.Events
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class EventEmitter
    {
        private static Dictionary<string, List<EventCallback<object>>> _events = new();

        public static void On<T>(string eventName, EventCallback<T> callback)
        {
            if (!_events.ContainsKey(eventName))
                _events[eventName] = new List<EventCallback<object>>();
            _events[eventName].Add(callback as EventCallback<object>);
        }

        public static void Off<T>(string eventName, EventCallback<T> callback)
        {
            if (_events.ContainsKey(eventName))
                _events[eventName].Remove(callback as EventCallback<object>);
            if (_events[eventName].Count == 0)
                _events.Remove(eventName);
        }

        public static void Emit<T>(string eventName, T arg = default)
        {
            Debug.Log($"Emitting event {eventName}");
            if (_events.ContainsKey(eventName))
                foreach (var callback in _events[eventName])
                    callback.Invoke(arg);
        }

        public static void Clear() => _events.Clear();
    }
}