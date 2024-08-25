using System.Collections.Generic;
// ReSharper disable All

namespace api.nox.network.Utils
{
    public class Manager<T>
    {
        public static List<T> Cache = new();
        public delegate void OnAdd(T item);
        public static event OnAdd OnAddEvent;
        public delegate void OnRemove(T item);
        public static event OnRemove OnRemoveEvent;
        public delegate void OnChange(T item);
        public static event OnChange OnChangeEvent;

        public static T Add(T item)
        {
            Cache.Add(item);
            OnAddEvent?.Invoke(item);
            return item;
        }

        public static T Remove(T item)
        {
            Cache.Remove(item);
            OnRemoveEvent?.Invoke(item);
            return item;
        }

        public static bool Has(T item) => Cache.FindIndex(i => i.Equals(item)) != -1;

        public static T Set(T item)
        {
            if (Has(item))
                Remove(item);
            Add(item);
            OnChangeEvent?.Invoke(item);
            return item;
        }

        public static void Clear() => Cache.ForEach(i => Remove(i));
    }
}