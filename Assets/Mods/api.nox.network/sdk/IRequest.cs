using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.Network {
	public interface IRequest {
		public Dictionary<string, string> GetHeaders();

		public void SetHeader(string key, string value);

		public void SetHeaders(Dictionary<string, string> headers);

		public void RemoveHeader(string key);

		public string GetUrl();

		public void SetUrl(string url);

		public UniTask<bool> SetMasterUrl(string address, string path);

		public UniTask Send(bool force = false, CancellationToken token = default);

		public ushort GetStatus();

		public IResponse<T> GetMasterResponse<T>();

		public T GetResponse<T>();

		public void SetBody(string text, string contentType = null);

		public void SetMethod(string method);

		public string GetMethod();

		/// <summary>
		/// Gets the cache duration for the request.
		/// Refer to <see cref="SetCacheDuration(int)"/> for more details.
		/// </summary>
		/// <returns></returns>
		public int GetCacheDuration();

		/// <summary>
		/// Sets the cache duration for the request.
		/// If the value is -1, it will use default cache duration.
		/// If the value is 0, it will not cache the response.
		/// </summary>
		/// <param name="cacheTime"></param>
		public void SetCacheDuration(int cacheTime = -1);
	}
}