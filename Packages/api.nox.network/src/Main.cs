using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.Users;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network {
	public class Main : MainModInitializer, INetworkAPI {
		internal        IModCoreAPI  CoreAPI;
		internal static Main         Instance;
		private         LanguagePack _language;
		internal        CacheManager Cache;

		internal static IUserAPI UserAPI
			=> Main.Instance.CoreAPI.ModAPI.GetMod("user")
				?.GetEntry<IUserAPI>();

		public void OnInitialize(IModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			Cache    = new CacheManager();
		}

		public async UniTask OnPostInitializeMainAsync() {
			Logger.Log("Testing network API...");
			var req = new Request();
			if (!await req.SetMasterUrl(UserAPI.GetCurrent().GetServerAddress(), "/api/test"))
				Logger.LogWarning("Network API is not working. Please check your server address.");
			else {
				req.SetUrl("https://postman-echo.com/get");
				await req.Send(true);
				Logger.LogDebug("Test: " + req.GetResponse<string>());
			}
		}

		public void OnDispose() {
			if (Cache != null) {
				Cache.Dispose();
				Cache = null;
			}

			CoreAPI  = null;
			Instance = null;
		}

		private readonly List<(string, UnityWebRequest)> _activeRequests = new();

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<Texture2D> FetchTexture(string url, UnityWebRequest req = null, Action<float, ulong> progress = null, CancellationToken token = default) {
			if (string.IsNullOrEmpty(url)) {
				Logger.LogWarning("FetchTexture: URL is null or empty.");
				return null;
			}

			Logger.Log($"Fetching [TEXTURE] {url}...");
			var                    request = _activeRequests.FirstOrDefault(r => r.Item1 == url);
			DownloadHandlerTexture dt;

			bool isMain;

			if (request != default) {
				isMain = false;
				req    = request.Item2;
				Logger.LogDebug($"Reusing existing request for {url}...");
				dt = req.downloadHandler as DownloadHandlerTexture;
				await UniTask.WaitUntil(
					() => {
						progress?.Invoke(req.downloadProgress, req.downloadedBytes);
						return req.isDone || token.IsCancellationRequested;
					}, cancellationToken: token
				);
			} else {
				isMain              =   true;
				req                 ??= new UnityWebRequest(url, "GET");
				req.url             =   url;
				dt                  =   new DownloadHandlerTexture();
				req.downloadHandler =   dt;
				_activeRequests.Add((url, req));
				try {
					var asc = req.SendWebRequest();
					await UniTask.WaitUntil(
						() => {
							CoreAPI.EventAPI.Emit(
								new NetEventContext(
									"network.download",
									url,
									req.downloadProgress,
									req.downloadedBytes
								)
							);
							progress?.Invoke(req.downloadProgress, req.downloadedBytes);
							return asc.isDone || token.IsCancellationRequested;
						}, cancellationToken: token
					);
				} catch {
					// ignored
				}
			}

			if (token.IsCancellationRequested && isMain) req.Abort();
			_activeRequests.RemoveAll(r => r.Item1 == url);
			return req.responseCode != 200 ? null : dt!.texture;
		}

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<string> DownloadFile(string url, string hash, UnityWebRequest req = null, Action<float, ulong> progress = null, CancellationToken token = default) {
			if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(hash)) {
				Logger.LogWarning("DownloadFile: URL or hash is null or empty.");
				return null;
			}

			Logger.Log($"Fetching [FILE] {url}...");
			req                 ??= new UnityWebRequest(url, "GET");
			req.url             =   url;
			req.downloadHandler =   new DownloadHandlerBuffer();
			try {
				var asc = req.SendWebRequest();
				await UniTask.WaitUntil(
					() => {
						CoreAPI.EventAPI.Emit(
							new NetEventContext(
								"network.download",
								url,
								req.downloadProgress,
								req.downloadedBytes
							)
						);
						progress?.Invoke(req.downloadProgress, req.downloadedBytes);
						return asc.isDone || token.IsCancellationRequested;
					}, cancellationToken: token
				);

				if (token.IsCancellationRequested) {
					req.Abort();
					return null;
				}
			} catch {
				return null;
			}

			if (req.responseCode != 200) return null;
			var file = Path.Combine(Application.temporaryCachePath, hash);
			if (!Directory.Exists(Path.GetDirectoryName(file)))
				Directory.CreateDirectory(Path.GetDirectoryName(file) ?? string.Empty);
			await File.WriteAllBytesAsync(file, req.downloadHandler.data, token);
			if (Hashing.HashFile(file) == hash) return file;
			File.Delete(file);
			return null;
		}

		public string MergeUrl(Uri url, string path)
			=> Request.MergeUrl(url, path);

		public string MergeUrl(string url, string path)
			=> Request.MergeUrl(url, path);

		public UniTask<string> GetGateway(string address)
			=> Discover.GetGateway(address);

		public IRequest MakeRequest()
			=> new Request();
	}
}