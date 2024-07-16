using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using NUnit.Framework;
using UnityEngine;

namespace api.nox.game
{
    internal class WorldManager
    {
        internal static GameClientSystem _gameClientSystem;
        internal static Dictionary<string, AssetBundle> _loadedWorlds = new();
        public static string WorldPath(string hash) => Path.Combine(Constants.GameAppDataPath, "cache", "worlds", hash);
        public static bool HasWorldInCache(string hash) => File.Exists(WorldPath(hash));
        public static AssetBundle GetLoadedWorld(string hash) => _loadedWorlds.ContainsKey(hash) ? _loadedWorlds[hash] : null;
        public static string[] GetLoaddedWorlds() => _loadedWorlds.Keys.ToArray();

        public static void SaveWorldToCache(string hash, byte[] data)
        {
            if (!Directory.Exists(Path.Combine(Constants.GameAppDataPath, "cache", "worlds")))
                Directory.CreateDirectory(Path.Combine(Constants.GameAppDataPath, "cache", "worlds"));
            File.WriteAllBytes(WorldPath(hash), data);
        }

        public static void SaveWorldToCache(string hash, string path)
        {
            if (!Directory.Exists(Path.Combine(Constants.GameAppDataPath, "cache", "worlds")))
                Directory.CreateDirectory(Path.Combine(Constants.GameAppDataPath, "cache", "worlds"));
            File.Copy(path, WorldPath(hash));
        }

        public static AssetBundle GetOrLoadWorld(string hash)
        {
            if (_loadedWorlds.ContainsKey(hash))
                return _loadedWorlds[hash];
            if (!HasWorldInCache(hash))
                return null;
            var bundle = AssetBundle.LoadFromFile(WorldPath(hash));
            _loadedWorlds[hash] = bundle;
            return bundle;
        }

        public static void DeleteWorldFromCache(string hash)
        {
            if (_loadedWorlds.ContainsKey(hash))
                throw new System.Exception("World is loaded, unload it first");
            if (HasWorldInCache(hash))
                File.Delete(WorldPath(hash));
        }

        public static void ClearWorldCache()
        {
            var files = Directory.GetFiles(Path.Combine(Constants.GameAppDataPath, "cache", "worlds"));
            foreach (var file in files)
            {
                if (_loadedWorlds.ContainsValue(AssetBundle.LoadFromFile(file))) continue;
                File.Delete(file);
            }
        }

        public static void UnloadWorld(string hash)
        {
            if (!_loadedWorlds.ContainsKey(hash))
                throw new System.Exception("World is not loaded");
            _loadedWorlds[hash].Unload(true);
            _loadedWorlds.Remove(hash);
        }

        public static void UnloadAllWorlds()
        {
            foreach (var world in _loadedWorlds)
                world.Value.Unload(true);
            _loadedWorlds.Clear();
        }

        public static async UniTask<DownloadWorldResult> DownloadWorld(string hash, string url, Action<float, ulong> progress = null)
        {
            if (HasWorldInCache(hash))
                return new DownloadWorldResult { success = true, hash = hash, url = url };
            var eventsub = _gameClientSystem.coreAPI.EventAPI.Subscribe("network.download", (context) =>
            {
                if (context.Data[0] as string != url) return;
                progress?.Invoke((float)context.Data[1], (ulong)context.Data[2]);
            });
            var res = await _gameClientSystem.NetworkAPI.DownloadFile(url, hash);
            _gameClientSystem.coreAPI.EventAPI.Unsubscribe(eventsub);
            progress?.Invoke(1, 0);
            if (res == null)
                return new DownloadWorldResult { success = false, hash = hash, url = url, error = "Download failed" };
            SaveWorldToCache(hash, res);
            return new DownloadWorldResult { success = true, hash = hash, url = url };
        }
    }

    public class DownloadWorldResult
    {
        public string url;
        public string hash;
        public bool success;
        public string error;
    }
}