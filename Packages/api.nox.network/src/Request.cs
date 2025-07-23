using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Network;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network {
	public class Request : INoxObject, IRequest {
		public Request()
			=> SetHeaders(
				new Dictionary<string, string> {
					{
						"User-Agent",
						string.Join(
							' ',
							$"{Application.productName}/{Application.version}",
							$"{Constants.ProtocolIdentifier}/{Constants.ProtocolVersion}",
							$"(en={EngineExtensions.CurrentEngine.GetEngineName()}; pn={PlatformExtensions.CurrentPlatform.GetPlatformName()})"
						)
					}, {
						"X-UUID",
						SystemInfo.deviceUniqueIdentifier
					}, {
						"X-Nox-User",
						Main.UserAPI?.GetCurrent()?.ToIdentifier().ToString()
						?? string.Empty
					}, {
						"X-Nox-Mods",
						string.Join(
							"; ",
							Main.Instance.CoreAPI.ModAPI.GetMods()
								.Where(mod => mod != null && mod.IsLoaded())
								.Select(m => m.GetMetadata())
								.Select(metadata => $"{metadata.GetId()}/{metadata.GetVersion()}")
						)
					}, {
						"X-Powered-By",
						"Nox"
					}
				}
			);

		internal readonly UnityWebRequest RequestObject = new() {
			downloadHandler = new DownloadHandlerBuffer(),
			timeout         = 30
		};

		internal readonly Dictionary<string, string> RequestHeaders = new();

		public Dictionary<string, string> GetHeaders()
			=> RequestHeaders;

		public void SetHeader(string key, string value) {
			key = key?.ToLowerInvariant().Trim();
			if (string.IsNullOrEmpty(key)) {
				Logger.LogError("Header key cannot be null or empty.");
				return;
			}

			RequestHeaders[key] = value;
		}

		public void SetHeaders(Dictionary<string, string> headers) {
			if (headers == null) {
				Logger.LogError("Headers cannot be null.");
				return;
			}

			foreach (var header in headers)
				SetHeader(header.Key, header.Value);
		}

		public void RemoveHeader(string key) {
			key = key?.ToLowerInvariant().Trim();
			if (string.IsNullOrEmpty(key)) {
				Logger.LogError("Header key cannot be null or empty.");
				return;
			}

			if (RequestHeaders.ContainsKey(key))
				RequestHeaders.Remove(key);
			else Logger.LogWarning($"Header '{key}' does not exist and cannot be removed.");
		}

		public string GetUrl()
			=> RequestObject.url;

		public void SetUrl(string url)
			=> RequestObject.url = url;

		public async UniTask<bool> SetMasterUrl(string address, string path) {
			var gateway = await Discover.GetGateway(address);

			if (gateway == null) {
				Logger.LogError($"No gateway found for the address: {address}");
				return false;
			}

			SetUrl(MergeUrl(gateway, path));

			var token = Main.UserAPI != null
				? await Main.UserAPI.GetToken(address)
				: null;

			if (token == null) {
				Logger.LogWarning("No token found for the specified address.");
				return true;
			}

			SetHeader("Authorization", token.ToHeader());
			return true;
		}

		private Cache _responseCache;

		public async UniTask Send(bool force = false, CancellationToken token = default) {
			if (!force && CacheDuration > 0 && _responseCache != null) {
				_responseCache = Main.Instance.Cache.Get(Cache.CalculateId(this));
				if (_responseCache != null) return;
			} else _responseCache = null;

			foreach (var header in GetHeaders().Where(header => !string.IsNullOrEmpty(header.Value)))
				RequestObject.SetRequestHeader(header.Key, header.Value);

			try {
				Logger.Log($"Sending request to {GetMethod()} {RequestObject.url}...");

				await RequestObject.SendWebRequest().WithCancellation(token);
			} catch (Exception e) {
				Logger.LogError($"Failed to send request to {RequestObject.url}: {e.Message}");
			}

			_responseCache = CacheDuration > 0
				? Main.Instance.Cache.Set(this)
				: null;
		}

		public ushort GetStatus()
			=> (ushort)RequestObject.responseCode;

		public IResponse<T> GetMasterResponse<T>() {
			var res = GetResponse<Response<T>>();
			if (res == null)
				return new Response<T> { error = new Error { message = "Failed to parse response.", code = 500 } };
			if (res.HasError())
				return res;
			return !res.HasData()
				? new Response<T> { error = new Error { message = "Response does not contain data.", code = 500 } }
				: res;
		}

		public T GetResponse<T>() {
			var data = _responseCache?.Data ?? RequestObject.downloadHandler.data;
			if (data == null || data.Length == 0)
				return default;

			if (typeof(T) == typeof(Texture2D)) {
				var texture = new Texture2D(2, 2);
				if (texture.LoadImage(data))
					return (T)(object)texture;
				Logger.LogError("Failed to load texture from response data.");
				return default;
			}

			if (typeof(T) == typeof(string))
				return (T)(object)System.Text.Encoding.UTF8.GetString(data);
			if (typeof(T) == typeof(byte[]))
				return (T)(object)data;

			try {
				var json = System.Text.Encoding.UTF8.GetString(data);
				return string.IsNullOrEmpty(json) ? default : JsonUtility.FromJson<T>(json);
			} catch {
				// ignored
			}

			return default;
		}

		public void SetBody(string text, string contentType = null) {
			if (!string.IsNullOrEmpty(contentType))
				RequestObject.SetRequestHeader("Content-Type", contentType);
			RequestObject.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(text ?? ""));
		}

		public void SetBody(byte[] data, string contentType = null) {
			if (!string.IsNullOrEmpty(contentType))
				RequestObject.SetRequestHeader("Content-Type", contentType);
			RequestObject.uploadHandler = new UploadHandlerRaw(data ?? Array.Empty<byte>());
		}

		public void SetMethod(string method) {
			RequestObject.method = method.ToUpperInvariant();
		}

		public string GetMethod()
			=> RequestObject.method;

		internal int CacheDuration = Cache.DefaultCacheDuration;

		public int GetCacheDuration()
			=> CacheDuration;

		public void SetCacheDuration(int cacheTime = -1) {
			if (cacheTime < 0)
				CacheDuration = Cache.DefaultCacheDuration;
			else if (cacheTime > 0)
				CacheDuration  = cacheTime;
			else CacheDuration = 0; // Disable caching
		}

		/// <summary>
		/// Gets the current download progress of the request.
		/// Returns a value between 0.0 and 1.0, where 1.0 means download is complete.
		/// </summary>
		/// <returns>The download progress as a float between 0.0 and 1.0</returns>
		public float GetDownloadProgress()
			=> RequestObject.downloadProgress;

		/// <summary>
		/// Gets the current upload progress of the request.
		/// Returns a value between 0.0 and 1.0, where 1.0 means upload is complete.
		/// </summary>
		/// <returns>The upload progress as a float between 0.0 and 1.0</returns>
		public float GetUploadProgress()
			=> RequestObject.uploadProgress;

		public static string MergeUrl(Uri url, string path)
			=> MergeUrl(url.ToString(), path);

		public static string MergeUrl(string url, string path) {
			if (url.EndsWith("/"))
				url = url[..^1];
			if (path.StartsWith("/"))
				path = path[1..];
			return $"{url}/{path}";
		}

		public void SetDownloadHandler(DownloadHandler handler) {
			if (handler == null) return;
			RequestObject.downloadHandler = handler;
		}
	}
}