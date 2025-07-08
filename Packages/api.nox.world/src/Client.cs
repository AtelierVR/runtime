using System.Linq;
using api.nox.world.client;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.Instances;
using Nox.UI;

namespace api.nox.world {
	public class Client : ClientModInitializer {
		internal static IUiAPI UiAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("ui")
				.GetClients()
				.FirstOrDefault() as IUiAPI;

		internal static IInstanceAPI InstanceAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("instance")
				.GetMains()
				.FirstOrDefault() as IInstanceAPI;
		
		public static T GetAsset<T>(string path, string ns = null) where T : UnityEngine.Object
			=> string.IsNullOrEmpty(ns)
				? Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(path)
				: Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(ns, path);

		private EventSubscription _event;

		public void OnInitializeClient(ClientModCoreAPI api) {
			_event = Main.Instance.CoreAPI.EventAPI.Subscribe("menu_goto", OnGoto);
		}

		private void OnGoto(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out string key)) return;
			var menu = UiAPI?.Get<IMenu>(mid);
			if (menu == null) return;
			IPage page = null;
			if (WorldPage.GetStaticKey() == key)
				page = WorldPage.OnGotoAction(menu, context.Data[2..]);
			if (page == null) return;
			Main.Instance.CoreAPI.EventAPI.Emit("menu_display", menu.GetId(), page);
		}

		public void OnDisposeClient() {
			Main.Instance.CoreAPI.EventAPI.Unsubscribe(_event);
			_event = null;
		}
	}
}