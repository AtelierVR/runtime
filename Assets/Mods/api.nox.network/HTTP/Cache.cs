using System;
using System.Collections.Generic;
using UnityEngine;

namespace api.nox.network
{
    public class NetCache
    {
        public static List<ICached> Caches = new();

        public static void Set(ICached cache)
        {
            if (Has(cache)) Remove(cache);
            Caches.Add(cache);
        }

        public static void Clear()
        {
            foreach (var cache in Caches.ToArray())
                if (cache is IDisposable disposable)
                    disposable.Dispose();
            Caches.Clear();
        }

        public static int Count() => Caches.Count;

        public static int IndexOf<T>(T cache) where T : ICached => Caches.FindIndex(c => c.GetCacheKey() == cache.GetCacheKey() && c.GetType() == cache.GetType());
        public static int IndexOf<T>(string key) where T : ICached => Caches.FindIndex(c => c.GetCacheKey() == key && c.GetType() == typeof(T));
        
        public static bool Has<T>(T cache) where T : ICached => IndexOf(cache) != -1;
        public static bool Has<T>(string key) where T : ICached => IndexOf<T>(key) != -1;

        public static T Get<T>(string key) where T : ICached => (T)Caches.Find(c => c.GetCacheKey() == key && c.GetType() == typeof(T));
        public static T Get<T>(T cache) where T : ICached => (T)Caches.Find(c => c.GetCacheKey() == cache.GetCacheKey() && c.GetType() == cache.GetType());

        public static void Remove<T>(T cache) where T : ICached => Caches.RemoveAll(c => c.GetCacheKey() == cache.GetCacheKey() && c.GetType() == cache.GetType());
        public static void Remove<T>(string key) where T : ICached => Caches.RemoveAll(c => c.GetCacheKey() == key && c.GetType() == typeof(T));
    }

    public interface ICached
    {
        public string GetCacheKey();
    }
}