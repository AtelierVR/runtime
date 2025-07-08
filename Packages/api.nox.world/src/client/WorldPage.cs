using System;
using System.Linq;
using System.Threading;
using api.nox.world.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.UI;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world.client {
	public class WorldPage : IPage {
		internal static string GetStaticKey()
			=> "world";

		public string GetKey()
			=> GetStaticKey();

		private int              _mId;
		private object[]         _context;
		private GameObject       _content;
		private WorldComponent   _component;
		private IWorldIdentifier _identifier;
		public  IWorld           World;
		public  IWorldAsset      Asset;
		public  ushort           Version = ushort.MaxValue;
		private bool             _isLoading;


		public void OnRefresh()
			=> Refresh(false).Forget();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		internal static IPage OnGotoAction(IMenu menu, object[] context) {
			if (!T(context, 0, out string type)) return null;
			switch (type) {
				case "id_server" when T(context, 1, out uint id0) && T(context, 2, out string ser0):
					return OnPageByIdentifier(menu, context, new WorldIdentifier(id0, null, ser0));
				case "identifier" when T(context, 1, out string id2):
					return OnPageByIdentifier(menu, context, WorldIdentifier.FromString(id2));
				case "world" when T(context, 1, out IWorld w0):
					var a0 = T(context, 2, out IWorldAsset asset) ? asset : null;
					return OnPageByWorld(menu, context, w0, a0);
			}

			return null;
		}

		private static WorldPage OnPageByIdentifier(IMenu menu, object[] context, WorldIdentifier identifier) {
			var page = new WorldPage {
				_mId        = menu.GetId(),
				_context    = context,
				_identifier = identifier,
				World       = null,
				Asset       = null,
				Version     = identifier.GetVersion()
			};
			page.Refresh(true).Forget();
			return page;
		}

		private static WorldPage OnPageByWorld(IMenu menu, object[] context, IWorld world, IWorldAsset asset)
			=> new() {
				_mId        = menu.GetId(),
				_context    = context,
				_identifier = world.ToIdentifier(),
				World       = world,
				Asset       = asset,
				Version     = asset?.GetVersion() ?? ushort.MaxValue
			};

		private async UniTask Refresh(bool load) {
			if (_isLoading) return;
			_isLoading = true;
			World      = await Main.Instance.Network.Fetch(_identifier.ToString());
			Asset = (await Main.Instance.Network.SearchAssets(
					_identifier.ToString(),
					new AssetSearchRequest {
						Limit     = 1,
						Versions  = new[] { Version },
						Engines   = new[] { EngineExtensions.CurrentEngine.GetEngineName() },
						Platforms = new[] { PlatformExtensions.CurrentPlatform.GetPlatformName() }
					}
				)).GetAssets()
				.FirstOrDefault();
			if (!load) _component.UpdateInstances(World).Forget();
			_isLoading = false;
		}

		public CancellationTokenSource DownloadToken;

		public bool IsDownloading()
			=> DownloadToken is { IsCancellationRequested: false };

		public bool IsDownloaded()
			=> false;

		public void RemoveDownload() {
			return;
		}

		public void CancelDownload() {
			if (DownloadToken == null) return;
			DownloadToken.Cancel();
			DownloadToken.Dispose();
			DownloadToken = null;
			_component?.UpdateDownloading(false, 1);
		}

		public async UniTask DownloadAssetAsync() {
			if (DownloadToken != null || Asset == null) return;
			DownloadToken = new CancellationTokenSource();

			_component.UpdateDownloading(true, 0);
			await Main.Instance.Network.DownloadAssetFile(
					_identifier.ToString(),
					Asset.GetVersion(),
					null,
					f => _component.UpdateDownloading(true, f)
				)
				.AttachExternalCancellation(DownloadToken.Token);

			if (DownloadToken.IsCancellationRequested) {
				DownloadToken.Dispose();
				DownloadToken = null;
				return;
			}

			_component.UpdateDownloading(true, 1);
			await UniTask.Yield();
			_component.UpdateDownloading(false, 1);
			DownloadToken.Dispose();
			DownloadToken = null;
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			Logger.LogDebug("Creating content for world page with identifier", parent);
			(_content, _component) = WorldComponent.Generate(this, parent);
			Logger.LogDebug("Created content for world page with identifier", parent);
			_component.UpdateLoading();
			return _content;
		}

		public void OnOpen(IPage lastPage)
			=> _component.UpdateInstances(World).Forget();

		public void OnDisplay(IPage lastPage) {
			if (World != null) _component.UpdateContent(World);
			else if (_isLoading) _component.UpdateLoading();
			else _component.UpdateError("World not found or loading failed.");
		}

		public void OnRemove() {
			if (DownloadToken != null) {
				DownloadToken.Cancel();
				DownloadToken.Dispose();
				DownloadToken = null;
			}
		}
	}
}