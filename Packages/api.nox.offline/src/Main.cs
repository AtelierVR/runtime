using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Offline;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.offline {
	public class Main : IOfflineAPI, MainModInitializer {
		internal static IEntityAPI EntityAPI
			=> _instance._coreAPI.ModAPI.GetMod("entity").GetMains().FirstOrDefault() as IEntityAPI;

		internal static IWorldAPI WorldAPI
			=> _instance._coreAPI.ModAPI.GetMod("world").GetMains().FirstOrDefault() as IWorldAPI;

		internal static ISessionAPI SessionAPI
			=> _instance._coreAPI.ModAPI.GetMod("session").GetMains().FirstOrDefault() as ISessionAPI;

		private        MainModCoreAPI      _coreAPI;
		private static Main                _instance;
		private        EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnInitializeMain(MainModCoreAPI api) {
			_coreAPI  = api;
			_instance = this;
			_events = new[] {
				_coreAPI.EventAPI.Subscribe("session_can_make_adapter", OnCanMakeAdapter),
				_coreAPI.EventAPI.Subscribe("session_make_adapter", OnMakeAdapter)
			};
		}

		private void OnCanMakeAdapter(EventData context) {
			if (!context.TryGet<string>(0, out var type) || type is not "offline") return;
			var options = !context.TryGet<Dictionary<string, object>>(1, out var opts)
				? new Dictionary<string, object>()
				: opts;

			if (!options.ContainsKey("world")) {
				Logger.LogError("Offline adapter requires world and version to be set in options");
				return;
			}

			var worldId = options.TryGetValue("world", out var w0) && w0 is IWorldIdentifier w1 ? w1 : null;
			if (worldId == null) {
				Logger.LogError("Offline adapter requires world to be set in options");
				return;
			}

			context.Callback(true);
		}

		private void OnMakeAdapter(EventData context) {
			if (!context.TryGet<string>(0, out var type) || type is not "offline")
				return;

			var options = !context.TryGet<Dictionary<string, object>>(1, out var opts)
				? new Dictionary<string, object>()
				: opts;

			var worldId = options.TryGetValue("world", out var w0)
				&& w0 is IWorldIdentifier w1
					? w1
					: null;

			var setCurrent = !options.TryGetValue("set_current", out var c0)
				|| c0 is not bool c1
				|| c1;

			var title = options.TryGetValue("title", out var t0) && t0 is string t1
				? t1
				: "Offline Session";

			var thumbnail = options.TryGetValue("thumbnail", out var i0) && i0 is Texture2D i1
				? i1
				: null;

			var adapter = new OfflineAdapter {
				Title     = title,
				Thumbnail = thumbnail
			};
			var session = SessionAPI.New(adapter);
			adapter.SetState(false, "Preparing offline session...", 0f);
			context.Callback(adapter, session);
			PrepareAsync(session, adapter, worldId, setCurrent).Forget();
		}

		private async UniTask PrepareAsync(ISession session, OfflineAdapter adapter, IWorldIdentifier worldId, bool setCurrent) {
			adapter.SetState(false, "Fetching world data...", 0.05f);
			var asset = (await WorldAPI.SearchAssets(
					worldId.ToString(),
					WorldAPI.MakeAssetSearchRequest()
						.SetEngines(new[] { EngineExtensions.CurrentEngine.GetEngineName() })
						.SetPlatforms(new[] { PlatformExtensions.CurrentPlatform.GetPlatformName() })
						.SetVersions(new[] { worldId.GetVersion() })
						.SetLimit(1)
				)).GetAssets()
				.FirstOrDefault();
			if (asset == null) {
				Logger.LogError($"No asset found for world {worldId.ToString()} with version {worldId.GetVersion()}");
				return;
			}

			adapter.SetState(false, $"Preparing world '{worldId.ToString()}'...", 0.1f);

			if (!WorldAPI.HasSceneInCache(asset.GetHash())) {
				adapter.SetState(false, $"Downloading world '{worldId.ToString()}'...", 0.15f);
				var download = WorldAPI.DownloadSceneToCache(
					asset.GetUrl(),
					hash: asset.GetHash(),
					progress: arg0 => adapter.SetState(false, $"Downloading world '{worldId.ToString()}'...", 0.15f + arg0 * 0.45f)
				);
				download.Start();
				await download.Wait();
			}

			adapter.SetState(false, $"Loading world '{worldId}'...", 0.6f);
			var scene = await WorldAPI.LoadFromCache(asset.GetHash());
			if (scene == null) {
				Logger.LogError($"Failed to load scene for world {worldId.ToString()} with version {worldId.GetVersion()}");
				return;
			}

			adapter.SetState(false, $"World '{worldId.ToString()}' loaded successfully", 0.65f);

			scene.SetIdentifier(worldId);
			adapter.SetDimension(scene);

			if (setCurrent) {
				adapter.SetState(false, $"Setting world '{worldId.ToString()}' as current", 0.8f);
				await session.SetCurrent();
			}

			adapter.SetState(true, $"World '{worldId.ToString()}' is ready");
		}

		public void OnDisposeMain() {
			foreach (var e in _events)
				_coreAPI.EventAPI.Unsubscribe(e);
			_coreAPI  = null;
			_instance = null;
		}

		[NoxPublic(NoxAccess.Method)]
		public IOfflineAdapter New()
			=> new OfflineAdapter();
	}
}