using System.Collections.Generic;
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.world.cache {
	public class Cache {
		internal readonly List<Caching> Cachings = new();

		public Caching GetDownload(string identifier, uint assetId)
			=> Cachings.Find(c => c.Identifier == identifier && c.AssetId == assetId);

		public Caching AddDownload(string identifier, uint assetId, string hash = null) {
			var existing = GetDownload(identifier, assetId);
			return existing ?? new Caching(this, identifier, assetId, hash);
		}

		public string CachePath {
			get {
				var dir = Path.Combine(Constants.CachePath, "worlds");
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				return dir;
			}
		}

		public string Get(string hash)
			=> Path.Combine(CachePath, hash);

		public bool Has(string hash)
			=> File.Exists(Get(hash));

		public void Clear() {
			if (Directory.Exists(CachePath)) Directory.Delete(CachePath, true);
		}

		public void Clear(string hash) {
			if (Has(hash)) File.Delete(Get(hash));
		}

		public async UniTask Save(string hash, byte[] data)
			=> await File.WriteAllBytesAsync(Get(hash), data);

		public void Save(string hash, string path)
			=> File.Copy(path, Get(hash));

		public void Dispose() {
			foreach (var caching in Cachings)
				caching.Cancel();
			Cachings.Clear();
		}
	}
}