using System;
using System.Collections.Generic;
using UnityEngine;

namespace api.nox.network
{
    public class NetCache
    {
        public static List<ICached> Caches = new();

        public class CacheSetEvent : UnityEngine.Events.UnityEvent<ICached> { }
        public static CacheSetEvent OnCacheSet = new();

        public class CacheRemoveEvent : UnityEngine.Events.UnityEvent<ICached> { }
        public static CacheRemoveEvent OnCacheRemove = new();




        public static void Set(ICached cache)
        {
            if (Has(cache)) Remove(cache);
            Caches.Add(cache);
            OnCacheSet.Invoke(cache);
        }

        public static void Clear()
        {
            foreach (var cache in Caches.ToArray())
            {
                if (cache is IDisposable disposable)
                    disposable.Dispose();
                OnCacheRemove.Invoke(cache);
            }
            Caches.Clear();
        }

        public static int Count() => Caches.Count;

        public static int IndexOf<T>(T cache) where T : ICached => Caches.FindIndex(c => c.GetCacheKey() == cache.GetCacheKey() && c.GetType() == cache.GetType());
        public static int IndexOf<T>(string key) where T : ICached => Caches.FindIndex(c => c.GetCacheKey() == key && c.GetType() == typeof(T));

        public static bool Has<T>(T cache) where T : ICached => IndexOf(cache) != -1;
        public static bool Has<T>(string key) where T : ICached => IndexOf<T>(key) != -1;

        public static T Get<T>(string key) where T : ICached => (T)Caches.Find(c => c.GetCacheKey() == key && c.GetType() == typeof(T));
        public static T Get<T>(T cache) where T : ICached => (T)Caches.Find(c => c.GetCacheKey() == cache.GetCacheKey() && c.GetType() == cache.GetType());

        public static void Remove<T>(T cache) where T : ICached
        {
            if (Has(cache))
            {
                Caches.RemoveAt(IndexOf(cache));
                OnCacheRemove.Invoke(cache);
            }
        }
        public static void Remove<T>(string key) where T : ICached
        {
            if (Has<T>(key))
            {
                var cache = Get<T>(key);
                Caches.RemoveAt(IndexOf<T>(key));
                OnCacheRemove.Invoke(cache);
            }
        }
    }

    public interface ICached
    {
        public string GetCacheKey();
    }
}