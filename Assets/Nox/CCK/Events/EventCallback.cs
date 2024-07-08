using System;
using UnityEngine;

namespace Nox.Events
{
    public class EventCallback<T>
    {
        private Action<T> _callback;
        private string _customId;

        public string Id => _customId;
        public Action<T> Callback => _callback;

        public EventCallback(Action<T> callback, string customId = null)
        {
            _callback = callback;
            if (string.IsNullOrEmpty(customId))
                customId = Guid.NewGuid().ToString("N");
            _customId = customId;
        }

        public void Invoke(T arg)
        {
            _callback(arg);
        }
    }
}