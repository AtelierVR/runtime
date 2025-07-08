/*using System.Linq;
using api.nox.game.controllers;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Logger = Nox.CCK.Utils.Logger;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.SceneManagement;
using Nox.Worlds;

namespace api.nox.game {
	public class GameClientSystem : ClientModInitializer {
		private static GameClientSystem _instance;

		private ClientModCoreAPI _coreAPI;

		[NoxPublic(NoxAccess.Method)]
		public PlayerController GetPlayerController()
			=> PlayerController.Instance;


		internal static ClientModCoreAPI CoreAPI
			=> _instance._coreAPI;

		internal static MainModInitializer NetworkAPI
			=> CoreAPI.ModAPI.GetMod("network").GetMains().FirstOrDefault();

		internal static MainModInitializer SessionAPI
			=> CoreAPI.ModAPI.GetMod("session").GetMains().FirstOrDefault();

		internal static MainModInitializer RelayAPI
			=> CoreAPI.ModAPI.GetMod("relay").GetMains().FirstOrDefault();

		internal static IWorldAPI WorldAPI
			=> CoreAPI.ModAPI.GetMod("world").GetMains().FirstOrDefault() as IWorldAPI;

		public async UniTask OnInitializeClientAsync(ClientModCoreAPI api) {
			_instance = this;
			_coreAPI  = api;
		}

		private Scene _defaultScene;

		private async UniTask PrepareDefaultWorld() {
			var world = await WorldAPI.LoadWorldFromAssets(
				CoreAPI.ModMetadata.GetId(),
				"worlds/default/default.unity"
			);
			world.GetMainScene().SetVisible(true);
			world.SetCurrent();
		}

		public async UniTask OnPostInitializeClientAsync() {
			await PrepareDefaultWorld();
		}

		/*private void OnSessionChanged(EventData context) {
			var oldSession = context.Data[0] as INoxObject; // as api.nox.session.Session;
			var newSession = context.Data[1] as INoxObject; // as api.nox.session.Session;
			if (_defaultWorld == default || !_defaultWorld.isLoaded) return;

			Logger.LogDebug($"Session Changed: \"{oldSession}\" -> \"{newSession}\"");

			if (newSession == null) SetupDefaultWorld();
			else WorldHidden.Get(_defaultWorld).Set(false);
		}#1#

		/*private void SetupDefaultWorld() {
			WorldHidden.Get(_defaultWorld).Set(true);
			var cur = PlayerController.Instance.currentController;
			if (!BaseDescriptor.TryGetDescriptor<BaseDescriptor>(_defaultWorld, out var desc)) {
				Logger.LogWarning("Default world has no descriptor");
				return;
			}

			cur.IsFlying = desc.GetFlyOnSpawn();
			Logger.LogDebug($"Default world is flying: {desc.GetFlyOnSpawn()}");
			if (desc.GetSpawnType() != SpawnType.None)
				cur.Teleport(desc.ChoiceSpawn().transform);
		}#1#

		// private void OnGotoTile(EventData context)
		// {
		//     var menuId = (context.Data[0] as int?) ?? 0;
		//     if (menuId == 0)
		//     {
		//         Logger.LogWarning("GotoTile: MenuId is 0");
		//         return;
		//     }
		//
		//     var page = context.Data[1] as string;
		//     Logger.LogDebug($"GotoTile: {menuId} {page}");
		//     switch (page)
		//     {
		//         case "home":
		//         case "game.home":
		//         case "default":
		//             _homeTile.SendTile(context);
		//             break;
		//         case "game.user":
		//             _userTile.SendTile(context);
		//             break;
		//         case "game.server":
		//             _serverTile.SendTile(context);
		//             break;
		//         case "game.navigation":
		//             _navigationTile.SendTile(context);
		//             break;
		//         case "game.world":
		//             _worldTile.SendTile(context);
		//             break;
		//         case "game.instance.make":
		//             _makeInstance.SendTile(context);
		//             break;
		//         case "game.instance":
		//             _instanceTile.SendTile(context);
		//             break;
		//         case "game.settings":
		//             _settingTile.SendTile(context);
		//             break;
		//         case "game.session":
		//             _sessionTile.SendTile(context);
		//             break;
		//     }
		// }

		// private void OnOldMenuClick(InputAction.CallbackContext context)
		// {
		//     Logger.Log("OldMenu Clicked");

		//     if (!coreAPI.XRAPI.IsEnabled() && eventSystem?.currentSelectedGameObject != null) return;
		//     var menu = GetOrCreateOldMenu();
		//     if (!menu.gameObject.activeSelf)
		//     {
		//         var forw = m_headCamera.transform.forward + m_controller.transform.forward / 2;
		//         menu.transform.position = m_headCamera.transform.position + forw * (coreAPI.XRAPI.IsEnabled() ? .5f : .4f);
		//         var rect = Reference.GetReference("game.menu.canvas", menu.gameObject).GetComponent<RectTransform>();
		//         menu.transform.position = new Vector3(
		//             menu.transform.position.x,
		//             m_headCamera.transform.position.y - rect.sizeDelta.y * rect.lossyScale.y / 2,
		//             menu.transform.position.z
		//         );
		//         var lookPos = m_headCamera.transform.position - menu.transform.position;
		//         lookPos.y = 0;
		//         var rotation = Quaternion.LookRotation(lookPos) * Quaternion.Euler(0, 180, 0);
		//         menu.transform.rotation = rotation;
		//     }
		//     menu.gameObject.SetActive(!menu.gameObject.activeSelf);
		// }

		public void OnDisposeClient() {
			// _sessionTile.OnDispose();
			// _homeTile.OnDispose();
			// _userTile.OnDispose();
			// await _serverTile.OnDisposeAsync();
			// _worldTile.OnDispose();
			// _navigationTile.OnDispose();
			// _settingTile.OnDispose();
			// _makeInstance.OnDispose();
			// _instanceTile.OnDispose();
			// _coreAPI.EventAPI.Unsubscribe(_tileSub);
			// _coreAPI.EventAPI.Unsubscribe(_tileGotoSub);
			// _coreAPI.EventAPI.Unsubscribe(_sessionChangedSub);
			// Worlds.WorldManager.UnloadAllAssets(true);
		}
	}
}*/