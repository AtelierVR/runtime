using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.world {
	public class DownloadWorldResult {
		public bool   Success;
		public string Hash;
		public string URL;
		public string Error;
	}

	public class WorldCache {
		public static string WorldPath(string hash)
			=> Path.Combine(Constants.AppPath, "cache", "worlds", hash);

		public static bool HasWorldInCache(string hash)
			=> File.Exists(WorldPath(hash));

		public static void SaveWorldToCache(string hash, byte[] data) {
			if (!Directory.Exists(Path.Combine(Constants.AppPath, "cache", "worlds")))
				Directory.CreateDirectory(Path.Combine(Constants.AppPath, "cache", "worlds"));
			File.WriteAllBytes(WorldPath(hash), data);
		}

		public static void SaveWorldToCache(string hash, string path) {
			if (!Directory.Exists(Path.Combine(Constants.AppPath, "cache", "worlds")))
				Directory.CreateDirectory(Path.Combine(Constants.AppPath, "cache", "worlds"));
			File.Copy(path, WorldPath(hash));
		}

		public static string GetWorldFromCache(string hash) {
			if (!HasWorldInCache(hash))
				return null;
			return WorldPath(hash);
		}

		public static void DeleteWorldFromCache(string hash) {
			if (HasWorldInCache(hash))
				File.Delete(WorldPath(hash));
		}

		public static void ClearCache() {
			if (Directory.Exists(Path.Combine(Constants.AppPath, "cache", "worlds")))
				Directory.Delete(Path.Combine(Constants.AppPath, "cache", "worlds"), true);
		}
	}
}