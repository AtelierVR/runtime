using System.Collections.Generic;

namespace api.nox.network {
	public class CacheManager {
		private readonly List<Cache> _caches = new();

		public Cache Get(int id)
			=> _caches.Find(c => c.Id == id && !c.IsExpired());

		public Cache Set(Request request) {
			if (request == null) return null;
			var id    = Cache.CalculateId(request);
			var cache = Get(id);
			if (cache != null)
				_caches.Remove(cache);
			cache = Cache.Create(request);
			_caches.Add(cache);
			return cache;
		}

		public void Dispose() {
			_caches.Clear();
		}
	}
}