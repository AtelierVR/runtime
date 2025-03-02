using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.game.Worlds
{
    public class DownloadWorldResult
    {
        public bool success;
        public string hash;
        public string url;
        public string error;
    }

    internal class WorldCache
    {
        public static string WorldPath(string hash) => Path.Combine(Constants.GameAppDataPath, "cache", "worlds", hash);
        public static bool HasWorldInCache(string hash) => File.Exists(WorldPath(hash));

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

        public static string GetWorldFromCache(string hash)
        {
            if (!HasWorldInCache(hash))
                return null;
            return WorldPath(hash);
        }

        public static void DeleteWorldFromCache(string hash)
        {
            if (HasWorldInCache(hash))
                File.Delete(WorldPath(hash));
        }

        public static void ClearCache()
        {
            if (Directory.Exists(Path.Combine(Constants.GameAppDataPath, "cache", "worlds")))
                Directory.Delete(Path.Combine(Constants.GameAppDataPath, "cache", "worlds"), true);
        }


        public static async UniTask<DownloadWorldResult> DownloadWorld(string hash, string url,
            Action<float, ulong> progress = null, CancellationToken token = default)
        {
            // Check if world is already in cache
            if (HasWorldInCache(hash))
                return new DownloadWorldResult { success = true, hash = hash, url = url };

            // Download world
            var t0 = DateTime.Now;

            var res = await GameClientSystem.NetworkAPI.CallAsyncMethod<string>("DownloadFile",
                url, hash, null,
                new Action<float, ulong>((p, b) => progress?.Invoke(p, b)),
                token
            );

            var t1 = DateTime.Now;

            if (res == null)
                return new DownloadWorldResult
                    { success = false, hash = hash, url = url, error = "Failed to download world" };

            // Check if downloaded file is correct
            if (Hashing.HashFile(res) != hash)
            {
                File.Delete(res);
                return new DownloadWorldResult
                    { success = false, hash = hash, url = url, error = "Downloaded world hash mismatch" };
            }

            // Save world to cache
            SaveWorldToCache(hash, res);

            var t2 = DateTime.Now;

            Logger.Log($"Downloaded world {hash} from {url} in {t1 - t0} and saved in cache in {t2 - t1}");

            return new DownloadWorldResult { success = true, hash = hash, url = url };
        }
    }
}