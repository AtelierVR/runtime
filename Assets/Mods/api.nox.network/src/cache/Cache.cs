using System;
using System.Collections.Generic;

namespace api.nox.network {
	public class Cache {
		public const int  DefaultCacheDuration = 60; // seconds
		public const bool DefaultAllowCache    = true;

		// Response cache
		public byte[]                     Data;
		public Dictionary<string, string> Headers;
		public long                       Status;

		// References
		public int      Id;
		public DateTime Expiration;

		public bool IsExpired()
			=> DateTime.UtcNow > Expiration;
		
		public static int CalculateId(Request request) {
			var hash = request.RequestObject.url.GetHashCode();
			hash ^= request.RequestObject.method.GetHashCode();
			if (request.RequestObject.downloadHandler.data != null)
				hash ^= request.RequestObject.downloadHandler.data.GetHashCode();
			foreach (var kp in request.RequestHeaders)
				hash ^= kp.Key.GetHashCode() ^ kp.Value.GetHashCode();
			return hash;
		}

		public static Cache Create(Request request) {
			var cache = new Cache {
				Id         = CalculateId(request),
				Expiration = DateTime.UtcNow.AddSeconds(request.CacheDuration),
				Data       = request.RequestObject.downloadHandler?.data,
				Headers    = request.RequestObject.GetResponseHeaders(),
				Status     = request.RequestObject.responseCode,
			};
			return cache;
		}
	}
}