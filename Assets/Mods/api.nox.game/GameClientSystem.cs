using System.Linq;
using api.nox.game.Controllers;
using api.nox.game.Tiles;
using api.nox.game.UI;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Worlds;
using UnityEngine;
using Object = UnityEngine.Object;
using api.nox.network;

namespace api.nox.game
{
    public class GameClientSystem : ClientModInitializer
    {
        internal static GameClientSystem Instance;
        internal static ClientModCoreAPI CoreAPI => Instance.coreAPI;

        internal ClientModCoreAPI coreAPI;
        private HomeTileManager homeTile;
        private UserTileManager userTile;
        private ServerTileManager serverTile;
        private WorldTileManager worldTile;
        private MakeInstanceTileManager makeinstance;
        internal NavigationTileManager navigationTile;
        private SettingTileManager settingTile;
        private InstanceTileManager instance;
        private EventSubscription tilesub;
        private EventSubscription tilegotosub;
        private EventSubscription sessionchangedsub;
        
        internal NetworkSystem NetworkAPI => coreAPI.ModAPI.GetMod("network")?.GetMainClasses().OfType<NetworkSystem>().FirstOrDefault();

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            Instance = this;
            coreAPI = api;

            var world = coreAPI.AssetAPI.LoadLocalWorld("default");

            // Initialize the game controller
            var controller = Object.Instantiate(api.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.controller"));
            controller.name = "game.controller";
            GameObject.DontDestroyOnLoad(controller);
            PlayerController.GetCurrentController().IsFlying = true;

            // Teleport the player to the spawn point
            try
            {
                var descriptor = BaseDescriptor.GetDescriptor(world);
                if (descriptor != null)
                    PlayerController.GetCurrentController().Teleport(descriptor.ChoiceSpawn().transform);
            }
            catch (System.Exception e) { Debug.LogWarning(e); }

            // Initialize the tile managers
            homeTile = new HomeTileManager();
            worldTile = new WorldTileManager();
            instance = new InstanceTileManager();
            userTile = new UserTileManager();
            serverTile = new ServerTileManager();
            navigationTile = new NavigationTileManager();
            settingTile = new SettingTileManager();
            makeinstance = new MakeInstanceTileManager();

            // Subscribe to the tile events
            tilesub = api.EventAPI.Subscribe("game.tile", context => MenuManager.Instance.OnTile(context));
            tilegotosub = api.EventAPI.Subscribe("game.tile.goto", context => OnGotoTile(context));
        }

        public void OnPostInitializeClient()
        {
            Debug.Log("GameClientSystem PostInitialize");
            MenuManager.Instance.GetViewPortMenu().IsVisible = false;
            navigationTile.PostInitialize();
            settingTile.PostInitialize();
        }

        public void OnGotoTile(EventData context)
        {
            Debug.Log("GotoTile");
            for (int i = 0; i < context.Data.Length; i++)
                Debug.Log($"Data[{i}]: {context.Data[i]}");

            var menuId = (context.Data[0] as int?) ?? 0;
            if (menuId == 0)
            {
                Debug.Log("GotoTile: MenuId is 0");
                return;
            }

            var page = context.Data[1] as string;
            Debug.Log($"GotoTile: {menuId} {page}");
            switch (page)
            {
                case "home":
                case "game.home":
                case "default":
                    homeTile.SendTile(context);
                    break;
                case "game.user":
                    userTile.SendTile(context);
                    break;
                case "game.server":
                    serverTile.SendTile(context);
                    break;
                case "game.navigation":
                    navigationTile.SendTile(context);
                    break;
                case "game.world":
                    worldTile.SendTile(context);
                    break;
                case "game.instance.make":
                    makeinstance.SendTile(context);
                    break;
                case "game.instance":
                    instance.SendTile(context);
                    break;
                case "game.settings":
                    settingTile.SendTile(context);
                    break;
            }
        }

        // private void OnOldMenuClick(InputAction.CallbackContext context)
        // {
        //     Debug.Log("OldMenu Clicked");

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

        public void OnDispose()
        {
            homeTile.OnDispose();
            userTile.OnDispose();
            serverTile.OnDispose();
            worldTile.OnDispose();
            navigationTile.OnDispose();
            settingTile.OnDispose();
            coreAPI.EventAPI.Unsubscribe(tilesub);
            coreAPI.EventAPI.Unsubscribe(tilegotosub);
            coreAPI.EventAPI.Unsubscribe(sessionchangedsub);
            PlayerController.Instance.Dispose();
            MenuManager.Instance.Dispose();
            WorldManager.UnloadAllWorlds();
        }
    }


}