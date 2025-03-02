using System.Collections.Generic;
// ReSharper disable All

namespace api.nox.network.Utils
{
    public class Manager<T>
    {
        public static List<T> Cache = new();

        public class OnAddEvent : UnityEngine.Events.UnityEvent<T> { }
        public static OnAddEvent OnAdd = new();
        public class OnRemoveEvent : UnityEngine.Events.UnityEvent<T> { }
        public static OnRemoveEvent OnRemove = new();
        public class OnSetEvent : UnityEngine.Events.UnityEvent<T> { }
        public static OnSetEvent OnSet = new();

        public static T Add(T item)
        {
            Cache.Add(item);
            OnAdd.Invoke(item);
            return item;
        }

        public static T Remove(T item)
        {
            Cache.Remove(item);
            OnRemove.Invoke(item);
            return item;
        }

        public static bool Has(T item) => Cache.FindIndex(i => i.Equals(item)) != -1;

        public static T Set(T item)
        {
            if (Has(item))
                Remove(item);
            Add(item);
            OnSet.Invoke(item);
            return item;
        }

        public static void Clear() => Cache.ForEach(i => Remove(i));
    }
}