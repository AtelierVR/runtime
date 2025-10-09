using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.UI;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.instance.client {
	public class InstancePage : IPage {
		internal static string GetStaticKey()
			=> "instance";

		public string GetKey()
			=> GetStaticKey();

		internal int                 MId;
		private  object[]            _context;
		private  GameObject          _content;
		private  InstanceComponent   _component;
		private  IInstanceIdentifier _identifier;
		public   IInstance           Instance;
		public   IWorldAsset         Asset;
		public   IWorld              World;
		private  bool                _isLoading;
		public   ushort              Version = ushort.MaxValue;


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
					return OnPageByIdentifier(menu, context, new InstanceIdentifier(id0, null, ser0));
				case "identifier" when T(context, 1, out string id2):
					return OnPageByIdentifier(menu, context, InstanceIdentifier.FromString(id2));
				case "instance" when T(context, 1, out IInstance i0):
					var w0 = T(context, 2, out IWorld world) ? world : null;
					var a0 = T(context, 3, out IWorldAsset asset) ? asset : null;
					return OnPageByInstance(menu, context, i0, w0, a0);
			}

			return null;
		}

		private static InstancePage OnPageByIdentifier(IMenu menu, object[] context, InstanceIdentifier identifier) {
			var page = new InstancePage {
				MId         = menu.GetId(),
				_context    = context,
				_identifier = identifier,
				Instance    = null,
				World       = null,
			};
			page.Refresh(true).Forget();
			return page;
		}

		private static InstancePage OnPageByInstance(IMenu menu, object[] context, IInstance instance, IWorld world, IWorldAsset asset) {
			var page = new InstancePage {
				MId         = menu.GetId(),
				_context    = context,
				_identifier = instance.ToIdentifier(),
				Instance    = instance,
				World       = world,
				Asset       = asset,
				Version = Main.WorldAPI
					.Make(instance.GetWorldId())
					.GetVersion()
			};
			if (page.World == null)
				page.FetchWorld(true, true).Forget();
			else if (page.Asset == null)
				page.FetchAsset(true).Forget();
			return page;
		}

		private async UniTask FetchAsset(bool update = false) {
			if (_isLoading) return;
			_isLoading = true;
			Asset = (await Main.WorldAPI.SearchAssets(
					_identifier.ToString(),
					Main.WorldAPI.MakeAssetSearchRequest()
						.SetLimit(1)
						.SetVersions(new[] { Version })
						.SetEngines(new[] { EngineExtensions.CurrentEngine.GetEngineName() })
						.SetPlatforms(new[] { PlatformExtensions.CurrentPlatform.GetPlatformName() })
				)).GetAssets()
				.FirstOrDefault();
			_isLoading = false;
			if (update) _component.UpdateContent(Instance, World, Asset);
		}

		private async UniTask Refresh(bool load) {
			if (_isLoading) return;
			await FetchInstance();
			await FetchWorld();
			await FetchAsset();
			if (!load)
				_component.UpdatePlayerList(Instance).Forget();
			_component.UpdateContent(Instance, World, Asset);
		}

		private async UniTask FetchInstance(bool update = false) {
			if (_isLoading) return;
			_isLoading = true;
			Instance   = await Main.Instance.Network.Fetch(_identifier.ToString());
			_isLoading = false;
			if (update) _component.UpdateContent(Instance, World, Asset);
		}

		private async UniTask FetchWorld(bool update = false, bool updateAsset = false) {
			if (_isLoading || Instance == null) return;
			_isLoading = true;
			World      = await Main.WorldAPI.Fetch(Instance.GetWorldId());
			_isLoading = false;
			if (updateAsset) await FetchAsset(false);
			if (update) _component.UpdateContent(Instance, World, Asset);
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(MId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			Logger.LogDebug("Creating content for instance page with identifier", parent);
			(_content, _component) = InstanceComponent.Generate(this, parent);
			Logger.LogDebug("Created content for instance page with identifier", parent);
			_component.UpdateLoading();
			return _content;
		}

		public void OnOpen(IPage lastPage)
			=> _component.UpdatePlayerList(Instance).Forget();

		public void OnDisplay(IPage lastPage) {
			if (Instance != null) _component.UpdateContent(Instance, World, Asset);
			else if (_isLoading) _component.UpdateLoading();
			else _component.UpdateError("Instance not found or loading failed.");
		}

		public void OnRemove() { }
	}
}