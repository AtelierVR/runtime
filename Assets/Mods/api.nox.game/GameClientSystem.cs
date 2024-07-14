using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.Controllers;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.game
{
    public class GameClientSystem : ClientModInitializer
    {
        internal ClientModCoreAPI coreAPI;

        private GameObject controllerObject;
        private HomeTileManager homeTile;
        private UserTileManager userTile;
        private ServerTileManager serverTile;
        private WorldTileManager worldTile;
        private NavigationTileManager navigationTile;
        private EventSystem eventSystem;
        private EventSubscription tilesub;
        private EventSubscription tilegotosub;

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            coreAPI = api;
            api.AssetAPI.LoadLocalWorld("default");
            var controller = api.AssetAPI.GetLocalAsset<GameObject>("prefabs/xr-controller");
            m_controller = Object.Instantiate(controller);
            m_controller.name = "game.controller";
            GameObject.DontDestroyOnLoad(m_controller);
            var XRControllerComponent = m_controller.AddComponent<XRController>();
            XRControllerComponent.enabled = false;
            var DesktopControllerComponent = m_controller.AddComponent<DesktopController>();
            DesktopControllerComponent.enabled = false;
            XRControllerComponent.SetupControllerAtStartup(api);
            m_bindLeftMenu.action.performed += ctx => OnMenuClick(true);
            m_bindRightMenu.action.performed += ctx => OnMenuClick(false);
            homeTile = new HomeTileManager(this);
            userTile = new UserTileManager(this);
            serverTile = new ServerTileManager(this);
            navigationTile = new NavigationTileManager(this);
            worldTile = new WorldTileManager(this);
            tilesub = api.EventAPI.Subscribe("game.tile", OnTile);
            tilegotosub = api.EventAPI.Subscribe("game.tile.goto", OnGotoTile);
            eventSystem = Reference.GetReference("game.eventsystem", m_controller)?.GetComponent<EventSystem>();
            eventSystem?.gameObject.SetActive(!coreAPI.XRAPI.IsEnabled());
        }

        public void OnTile(Nox.CCK.Mods.Events.EventData context)
        {
            var tile = (context.Data[0] as ShareObject).Convert<TileObject>();
            if (_currentTile != null && _currentTile.isReforced && !tile.isReforced) return;
            SetDisplayTile(tile);
        }


        public void OnGotoTile(Nox.CCK.Mods.Events.EventData context)
        {
            var page = context.Data[0] as string;
            var args = context.Data.Skip(1).ToArray();
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
            }
        }


        private void OnMenuClick(bool isLeft)
        {
            if (!coreAPI.XRAPI.IsEnabled() && eventSystem?.currentSelectedGameObject != null) return;
            var menu = GetOrCreateMenu();
            if (!menu.gameObject.activeSelf)
            {
                var forw = m_headCamera.transform.forward + m_controller.transform.forward / 2;
                menu.transform.position = m_headCamera.transform.position + forw * (coreAPI.XRAPI.IsEnabled() ? .5f : .4f);
                var rect = Reference.GetReference("game.menu.canvas", menu.gameObject).GetComponent<RectTransform>();
                menu.transform.position = new Vector3(
                    menu.transform.position.x,
                    m_headCamera.transform.position.y - rect.sizeDelta.y * rect.lossyScale.y / 2,
                    menu.transform.position.z
                );
                var lookPos = m_headCamera.transform.position - menu.transform.position;
                lookPos.y = 0;
                var rotation = Quaternion.LookRotation(lookPos) * Quaternion.Euler(0, 180, 0);
                menu.transform.rotation = rotation;
            }
            menu.gameObject.SetActive(!menu.gameObject.activeSelf);
        }

        public void GotoTile(string page, params object[] args) => coreAPI.EventAPI.Emit("game.tile.goto", page, args);

        private GameObject m_controller;
        private InputActionReference m_bindLeftMenu => Binding.GetBinding("game.lefthand.menu", m_controller);
        private InputActionReference m_bindRightMenu => Binding.GetBinding("game.righthand.menu", m_controller);
        private Camera m_headCamera => Reference.GetReference("game.camera", m_controller).GetComponent<Camera>();
        private Menu m_menu;
        private Menu GetOrCreateMenu()
        {
            if (m_menu != null) return m_menu;
            var menuPrefab = coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/xr-menu");
            var menuObject = Object.Instantiate(menuPrefab);
            menuObject.name = "game.menu";
            menuObject.gameObject.SetActive(false);
            GameObject.DontDestroyOnLoad(menuObject);
            m_menu = menuObject.GetComponent<Menu>();
            m_menu._gameClientSystem = this;
            GotoTile(m_menu.defaultTile);
            return m_menu;
        }



        public void OnDispose()
        {

            Object.Destroy(m_controller);
            if (m_menu != null) Object.Destroy(m_menu.gameObject);
            homeTile.OnDispose();
            userTile.OnDispose();
            serverTile.OnDispose();
            worldTile.OnDispose();
            navigationTile.OnDispose();
            coreAPI.EventAPI.Unsubscribe(tilesub);
            coreAPI.EventAPI.Unsubscribe(tilegotosub);
        }

        private TileObject _currentTile;
        private void SetDisplayTile(TileObject tile)
        {
            if (tile == null) return;
            Debug.Log($"Displaying tile {tile.id}");
            var menu = GetOrCreateMenu();
            if (menu == null) return;
            var container = Reference.GetReference("game.menu.container", menu.gameObject);
            if (container == null) return;
            var oldtile = _currentTile?.id;
            if (_currentTile != null)
            {
                _currentTile.onHide?.DynamicInvoke(tile.id);
                _currentTile.onRemove?.DynamicInvoke();
                Object.Destroy(_currentTile.content);
            }
            tile.onOpen?.DynamicInvoke(oldtile);
            tile.content.transform.SetParent(container.transform, false);
            tile.content.SetActive(true);
            tile.content.name = tile.id;
            _currentTile = tile;
            tile.onDisplay?.DynamicInvoke(oldtile);
            ForceUpdateLayout.UpdateManually(container);
        }
    }


}