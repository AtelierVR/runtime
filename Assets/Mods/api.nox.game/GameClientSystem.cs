using System.Linq;
using api.nox.game.controllers;
using api.nox.game.tiles;
using api.nox.game.Tiles;
using api.nox.game.UI;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using Object = UnityEngine.Object;
using api.nox.network;
using api.nox.world;
using Logger = Nox.CCK.Utils.Logger;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;

namespace api.nox.game
{
    public class GameClientSystem : ClientModInitializer
    {
        private static GameClientSystem _instance;

        private ClientModCoreAPI _coreAPI;

        /*private HomeTileManager _homeTile;
        private UserTileManager _userTile;
        private ServerTileManager _serverTile;
        private WorldTileManager _worldTile;
        private MakeInstanceTileManager _makeInstance;
        private NavigationTileManager _navigationTile;
        private SettingTileManager _settingTile;
        private InstanceTileManager _instanceTile;
        private SessionTileManager _sessionTile;
        private EventSubscription _tileSub;
        private EventSubscription _tileGotoSub;
        private EventSubscription _sessionChangedSub;*/
        private Scene _defaultWorld;

        internal static ClientModCoreAPI CoreAPI
            => _instance._coreAPI;

        internal static MainModInitializer NetworkAPI
            => CoreAPI.ModAPI.GetMod("network").GetMains().FirstOrDefault();

        internal static MainModInitializer SessionAPI
            => CoreAPI.ModAPI.GetMod("session").GetMains().FirstOrDefault();

        internal static MainModInitializer RelayAPI
            => CoreAPI.ModAPI.GetMod("relay").GetMains().FirstOrDefault();


        public async UniTask OnInitializeClientAsync(ClientModCoreAPI api)
        {
            Logger.LogDebug("GameControllerClient.OnInitializeClientAsync");
            _instance = this;
            _coreAPI = api;

            // Initialize the tile managers
            /*_homeTile = new HomeTileManager();
            _worldTile = new WorldTileManager();
            _instanceTile = new InstanceTileManager();
            _userTile = new UserTileManager();
            // await _userTile.Widget.Initialization();
            _serverTile = new ServerTileManager();
            // await _serverTile.Initialization();
            _navigationTile = new NavigationTileManager();
            _settingTile = new SettingTileManager();
            _makeInstance = new MakeInstanceTileManager();
            _sessionTile = new SessionTileManager();

            // Subscribe to the tile events
            _tileSub = api.EventAPI.Subscribe("game.tile", context => MenuManager.Instance.OnTile(context));
            _tileGotoSub = api.EventAPI.Subscribe("game.tile.goto", OnGotoTile);
            _sessionChangedSub = api.EventAPI.Subscribe("session.changed", OnSessionChanged);
            */

            PlayerController.Create();
            Logger.LogDebug("GameControllerClient initialized");
            await PrepareDefaultWorld();
        }

        private async UniTask PrepareDefaultWorld()
        {
            Logger.LogDebug("Loading default world");
            _defaultWorld = await _coreAPI.AssetAPI.LoadWorld("worlds/default/default.unity");
            Logger.LogDebug(
                $"aaa Default world loaded as {_defaultWorld.name} {_defaultWorld.isLoaded} {_defaultWorld.IsValid()}");
            SetupDefaultWorld();
        }

        public void OnPostInitializeClient()
        {
            Logger.LogDebug("GameControllerClient.OnPostInitializeClient");
            MenuManager.Instance.GetViewPortMenu().IsVisible = false;
            // _navigationTile.PostInitialize();
            // _settingTile.PostInitialize();
            // _sessionTile.PostInitialize();
        }

        private void OnSessionChanged(EventData context)
        {
            var oldSession = context.Data[0] as INoxObject; // as api.nox.session.Session;
            var newSession = context.Data[1] as INoxObject; // as api.nox.session.Session;
            if (_defaultWorld == default || !_defaultWorld.isLoaded) return;

            Logger.LogDebug($"Session Changed: \"{oldSession}\" -> \"{newSession}\"");

            if (newSession == null) SetupDefaultWorld();
            else WorldHidden.Get(_defaultWorld).Set(false);
        }

        private void SetupDefaultWorld()
        {
            WorldHidden.Get(_defaultWorld).Set(true);
            var cur = PlayerController.instance.currentController;
            if (!BaseDescriptor.TryGetDescriptor<BaseDescriptor>(_defaultWorld, out var desc))
            {
                Logger.LogWarning("Default world has no descriptor");
                return;
            }

            cur.IsFlying = desc.GetFlyOnSpawn();
            Logger.LogDebug($"Default world is flying: {desc.GetFlyOnSpawn()}");
            if (desc.GetSpawnType() != SpawnType.None)
                cur.Teleport(desc.ChoiceSpawn().transform);
        }

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

        public void OnDisposeClient()
        {
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
            if (PlayerController.instance)
                PlayerController.instance.Dispose();
            if (MenuManager.Instance != null)
                MenuManager.Instance.Dispose();
            Worlds.WorldManager.UnloadAllAssets(true);
        }
    }
}