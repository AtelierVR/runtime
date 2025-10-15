using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Network;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network {
	public class Request : INoxObject, IRequest {
		// Cache statiques pour éviter les allocations répétées
		private static readonly StringBuilder StringBuilder = new(1024);
		private static readonly UTF8Encoding UTF8Encoding = new(false, false);
		private static readonly Dictionary<Type, Func<byte[], object>> TypeConverters = new() {
			{ typeof(string), data => UTF8Encoding.GetString(data) },
			{ typeof(byte[]), data => data },
			{ typeof(Texture2D), data => {
				var texture = new Texture2D(2, 2);
				return texture.LoadImage(data) ? texture : null;
			}}
		};
		
		// Headers par défaut calculés une seule fois au démarrage statique
		private static readonly Dictionary<string, string> DefaultHeaders = new();
		private static bool _defaultHeadersInitialized;
		private static bool _sslInitialized = false;
		
		// Gestionnaire de certificats SSL pour Unity
		private static bool AcceptAllCertificates(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			#if UNITY_EDITOR || DEVELOPMENT_BUILD
			// En développement, on accepte tous les certificats pour éviter les problèmes SSL
			if (sslPolicyErrors != SslPolicyErrors.None) {
				Logger.LogWarning($"SSL Certificate validation failed: {sslPolicyErrors}. Accepting anyway in development mode.");
			}
			return true;
			#else
			// En production, on peut être plus strict
			if (sslPolicyErrors == SslPolicyErrors.None)
				return true;
				
			// Accepter seulement les erreurs de nom qui ne matchent pas (communes avec les API de test)
			if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch) {
				Logger.LogWarning($"SSL Certificate name mismatch detected, but accepting connection.");
				return true;
			}
			
			Logger.LogError($"SSL Certificate validation failed: {sslPolicyErrors}");
			return false;
			#endif
		}
		
		private static void InitializeSSL() {
			if (_sslInitialized) return;
			
			lock (DefaultHeaders) {
				if (_sslInitialized) return;
				
				try {
					// Configurer le gestionnaire de certificats SSL pour Unity
					#if !UNITY_WEBGL
					System.Net.ServicePointManager.ServerCertificateValidationCallback = AcceptAllCertificates;
					System.Net.ServicePointManager.SecurityProtocol = 
						System.Net.SecurityProtocolType.Tls12 | 
						System.Net.SecurityProtocolType.Tls11 | 
						System.Net.SecurityProtocolType.Tls;
					#endif
					
					_sslInitialized = true;
					Logger.Log("SSL certificate validation configured successfully.");
				} catch (Exception e) {
					Logger.LogError($"Failed to configure SSL: {e.Message}");
				}
			}
		}
		
		public Request() {
			InitializeSSL();
			InitializeDefaultHeaders();
			SetHeaders(DefaultHeaders);
		}
		
		private static void InitializeDefaultHeaders() {
			if (_defaultHeadersInitialized) return;
			
			lock (DefaultHeaders) {
				if (_defaultHeadersInitialized) return;
				
				StringBuilder.Clear();
				StringBuilder.Append(Application.productName);
				StringBuilder.Append('/');
				StringBuilder.Append(Application.version);
				StringBuilder.Append(' ');
				StringBuilder.Append(Constants.ProtocolIdentifier);
				StringBuilder.Append('/');
				StringBuilder.Append(Constants.ProtocolVersion);
				StringBuilder.Append(" (en=");
				StringBuilder.Append(EngineExtensions.CurrentEngine.GetEngineName());
				StringBuilder.Append("; pn=");
				StringBuilder.Append(PlatformExtensions.CurrentPlatform.GetPlatformName());
				StringBuilder.Append(')');
				
				DefaultHeaders["user-agent"] = StringBuilder.ToString();
				DefaultHeaders["x-uuid"] = SystemInfo.deviceUniqueIdentifier;
				DefaultHeaders["x-powered-by"] = "Nox";
				
				_defaultHeadersInitialized = true;
			}
		}

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
			// Optimisation du cache - éviter les double vérifications
			if (!force && CacheDuration > 0) {
				var cachedResponse = Main.Instance.Cache.Get(Cache.CalculateId(this));
				if (cachedResponse != null) {
					_responseCache = cachedResponse;
					return;
				}
			}
			_responseCache = null;

			// Mettre à jour les headers dynamiques seulement si nécessaire
			UpdateDynamicHeaders();
			
			// Optimiser l'application des headers - éviter LINQ
			foreach (var kvp in RequestHeaders.Where(kvp => !string.IsNullOrEmpty(kvp.Value))) 
				RequestObject.SetRequestHeader(kvp.Key, kvp.Value);

			try {
				Logger.Log($"Sending request to {GetMethod()} {RequestObject.url}...");
				await RequestObject.SendWebRequest().WithCancellation(token);
			} catch (Exception e) {
				Logger.LogError($"Failed to send request to {GetMethod()} {RequestObject.url}: {e.Message}");
			}

			// Cache seulement si nécessaire
			if (CacheDuration > 0) {
				_responseCache = Main.Instance.Cache.Set(this);
			}
		}

		private void UpdateDynamicHeaders() {
			// Mise à jour des headers qui peuvent changer entre les requêtes
			var currentUser = Main.UserAPI?.GetCurrent()?.ToIdentifier().ToString();
			if (!string.IsNullOrEmpty(currentUser)) 
				RequestHeaders["x-nox-user"] = currentUser;

			// Optimiser la construction de la liste des mods
			if (Main.Instance?.CoreAPI?.ModAPI == null) return;
			
			lock (StringBuilder) {
				StringBuilder.Clear();
				var mods  = Main.Instance.CoreAPI.ModAPI.GetMods();
				var first = true;
					
				foreach (var mod in mods) {
					if (mod?.IsLoaded() != true) continue;
					var metadata = mod.GetMetadata();
					if (metadata == null) continue;
					if (!first) StringBuilder.Append("; ");
					StringBuilder.Append(metadata.GetId());
					StringBuilder.Append('/');
					StringBuilder.Append(metadata.GetVersion());
					first = false;
				}
					
				RequestHeaders["x-nox-mods"] = StringBuilder.ToString();
			}
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

			if (TypeConverters.TryGetValue(typeof(T), out var converter)) 
				return (T)converter(data);

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
			
			// Utiliser l'encodage UTF8 optimisé
			var data = UTF8Encoding.GetBytes(text ?? "");
			RequestObject.uploadHandler = new UploadHandlerRaw(data);
			
			// Ajuster automatiquement le timeout pour les gros uploads
			if (data.Length > 1024 * 1024)  // Plus de 1MB
				SetTimeoutForUpload(data.Length);
		}

		public void SetBody(byte[] data, string contentType = null) {
			if (!string.IsNullOrEmpty(contentType))
				RequestObject.SetRequestHeader("Content-Type", contentType);
			
			var uploadData = data ?? Array.Empty<byte>();
			RequestObject.uploadHandler = new UploadHandlerRaw(uploadData);
			
			// Ajuster automatiquement le timeout pour les gros uploads
			if (uploadData.Length > 1024 * 1024) // Plus de 1MB
				SetTimeoutForUpload(uploadData.Length);
		}

		public void SetMethod(string method) {
			RequestObject.method = method.ToUpperInvariant();
		}

		public string GetMethod()
			=> RequestObject.method;

		internal int CacheDuration = Cache.DefaultCacheDuration;

		public int GetCacheDuration()
			=> CacheDuration;

		public void SetCacheDuration(int cacheTime = -1) 
			=> CacheDuration = cacheTime switch {
				< 0 => Cache.DefaultCacheDuration,
				> 0 => cacheTime,
				_   => 0
			};

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
			if (string.IsNullOrEmpty(url)) return path ?? string.Empty;
			if (string.IsNullOrEmpty(path)) return url;
				
			// Optimisation : éviter les allocations de string avec EndsWith/StartsWith
			var urlEndsWithSlash    = url[^1] == '/';
			var pathStartsWithSlash = path[0] == '/';
			
			// Utiliser StringBuilder pour une seule allocation
			lock (StringBuilder) {
				StringBuilder.Clear();
				StringBuilder.EnsureCapacity(url.Length + path.Length + 1);
				
				if (urlEndsWithSlash) {
					StringBuilder.Append(url, 0, url.Length - 1);
				} else {
					StringBuilder.Append(url);
				}
				
				StringBuilder.Append('/');
				
				if (pathStartsWithSlash && path.Length > 1) {
					StringBuilder.Append(path, 1, path.Length - 1);
				} else if (!pathStartsWithSlash) {
					StringBuilder.Append(path);
				}
				
				return StringBuilder.ToString();
			}
		}

		public void SetDownloadHandler(DownloadHandler handler) {
			if (handler == null) return;
			RequestObject.downloadHandler = handler;
		}

		/// <summary>
		/// Sets the timeout for the request in seconds.
		/// Use 0 for no timeout, which is recommended for file uploads.
		/// </summary>
		/// <param name="timeoutSeconds">Timeout in seconds (0 = no timeout)</param>
		public void SetTimeout(int timeoutSeconds) {
			RequestObject.timeout = timeoutSeconds;
		}

		/// <summary>
		/// Gets the current timeout setting in seconds.
		/// </summary>
		/// <returns>Current timeout in seconds</returns>
		public int GetTimeout() {
			return RequestObject.timeout;
		}

		/// <summary>
		/// Sets an appropriate timeout based on the upload data size.
		/// Automatically calculates timeout based on file size for uploads.
		/// </summary>
		/// <param name="dataSizeBytes">Size of data being uploaded in bytes</param>
		public void SetTimeoutForUpload(long dataSizeBytes) {
			// Pour les uploads, on calcule un timeout basé sur la taille
			// Estimation: 1MB par seconde minimum + 60 secondes de buffer
			const int minTimeoutSeconds = 60;
			const int bytesPerSecond = 1024; // 1MB/s minimum
			
			var calculatedTimeout = Math.Max(minTimeoutSeconds, (int)(dataSizeBytes / bytesPerSecond) + 60);
			
			// Cap à 30 minutes maximum pour éviter les timeouts infinis
			const int maxTimeout = 30 * 60; // 30 minutes
			RequestObject.timeout = Math.Min(calculatedTimeout, maxTimeout);
			
			Logger.Log($"Set upload timeout to {RequestObject.timeout} seconds for {dataSizeBytes} bytes");
		}

		/// <summary>
		/// Disables timeout completely. Use this for large file uploads.
		/// </summary>
		public void DisableTimeout() {
			RequestObject.timeout = 0;
			Logger.Log("Timeout disabled for this request");
		}
	}
}