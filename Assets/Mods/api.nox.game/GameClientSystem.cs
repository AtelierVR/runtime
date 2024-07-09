using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.Controllers;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace api.nox.game
{
    public class GameClientSystem : ClientModInitializer
    {
        private ClientModCoreAPI coreAPI;

        private GameObject controllerObject;

        public void OnInitialize(ModCoreAPI api)
        {
        }

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            coreAPI = api;
            api.AssetAPI.LoadLocalWorld("default");
            var controller = api.AssetAPI.GetLocalAsset<GameObject>("prefabs/xr-controller");
            m_controller = Object.Instantiate(controller);
            GameObject.DontDestroyOnLoad(m_controller);
            var XRControllerComponent = m_controller.AddComponent<XRController>();
            XRControllerComponent.enabled = false;
            var DesktopControllerComponent = m_controller.AddComponent<DesktopController>();
            DesktopControllerComponent.enabled = false;
            XRControllerComponent.SetupControllerAtStartup(api);
            m_bindLeftMenu.action.performed += ctx => OnMenuClick(true);
            m_bindRightMenu.action.performed += ctx => OnMenuClick(false);

            api.EventAPI.Subscribe("game.widget", OnWidget);
            api.EventAPI.Subscribe("game.tile", OnTile);
            api.EventAPI.Subscribe("game.tile.goto", OnGotoTile);
        }

        public void OnTile(Nox.CCK.Mods.Events.EventData context)
        {
            var tile = (context.Data[0] as ShareObject).Convert<Tile>();
            Debug.Log("Tile ID: " + tile.id);
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
                    var tile = new Tile
                    {
                        id = "game.home",
                        content = coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.home")
                    };
                    coreAPI.EventAPI.Emit("game.tile", tile);
                    break;
            }
        }

        public void OnWidget(Nox.CCK.Mods.Events.EventData context)
        {
            var widget = (context.Data[0] as ShareObject).Convert<Widget>();
            Debug.Log("Widget ID: " + widget.id);
        }

        private void OnMenuClick(bool isLeft)
        {
            var menu = GetOrCreateMenu();
            if (!menu.gameObject.activeSelf)
            {
                var forw = m_headCamera.transform.forward + m_controller.transform.forward / 2;
                menu.transform.position = m_headCamera.transform.position + forw * .5f;
                var rect = Reference.GetReference("game.menu.canvas", menu.gameObject).GetComponent<RectTransform>();
                menu.transform.position = new Vector3(
                    menu.transform.position.x,
                    m_headCamera.transform.position.y - rect.sizeDelta.y * rect.lossyScale.y / 2,
                    menu.transform.position.z
                );
                var lookPos = m_headCamera.transform.position - menu.transform.position;
                lookPos.y = 0;
                var rotation = Quaternion.LookRotation(lookPos);
                menu.transform.rotation = rotation;
            }
            menu.gameObject.SetActive(!menu.gameObject.activeSelf);
        }

        public void GotoTile(string page, params object[] args) => coreAPI.EventAPI.Emit("game.tile.goto", page, args);

        public void OnUpdateClient()
        {
        }


        private GameObject m_controller;
        private InputActionReference m_bindLeftMenu => Nox.CCK.Binding.GetBinding("game.lefthand.menu", m_controller);
        private InputActionReference m_bindRightMenu => Nox.CCK.Binding.GetBinding("game.righthand.menu", m_controller);
        private Camera m_headCamera => Nox.CCK.Reference.GetReference("game.camera", m_controller).GetComponent<Camera>();
        private Menu m_menu;
        private Menu GetOrCreateMenu()
        {
            if (m_menu != null) return m_menu;
            var menuPrefab = coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/xr-menu");
            var menuObject = Object.Instantiate(menuPrefab);
            menuObject.gameObject.SetActive(false);
            GameObject.DontDestroyOnLoad(menuObject);
            m_menu = menuObject.GetComponent<Menu>();
            GotoTile("default");
            return m_menu;
        }

        public void OnDispose()
        {
            Object.Destroy(m_controller);
            if (m_menu != null) Object.Destroy(m_menu.gameObject);
        }

        private Tile _currentTile;
        private void SetDisplayTile(Tile tile)
        {
            var menu = GetOrCreateMenu();
            if (menu == null) return;
            var container = Reference.GetReference("game.menu.container", menu.gameObject);
            if (container == null) return;
            if (_currentTile != null)
            {
                _currentTile.onHide?.DynamicInvoke();
                _currentTile.content.transform.SetParent(null, false);
            }
            if (tile == null) return;
            tile.content.transform.SetParent(container.transform, false);
            _currentTile = tile;
            tile.onDisplay?.DynamicInvoke();
        }
    }

    public class Widget : ShareObject
    {
        public string id;
        public uint width = 1;
        public uint height = 1;
        public GameObject content;
        public GameObject button = null;
        public bool isInteractable = true;
        public uint weight = 1;
        public Action onClick = null;
        public Action<bool> onHover = null;
    }

    public class Tile : ShareObject
    {
        public bool showNavigation = true;
        public bool isReforced = false; // If true, the tile can't be removed, except by an other renforced tile
        public bool addToHistory = true; // If true, the tile will be added to the history, and can be restored
        public string id;
        public GameObject content;
        public Action<string> onOpen = null; // Called when the tile is opened at the first time (before reading the content) (the string is the previous tile id)
        public Action<string> onRestore = null; // Called when the tile is restored (before reading the content) (the string is the previous tile id)
        public Action onRemove = null; // Called when the tile is removed (after hiding the content) (the string is the next tile id)
        public Action<string> onDisplay = null; // Called when the tile is displayed (after reading the content) (the string is the previous tile id)
        public Action<string> onHide = null; // Called when the tile is hidden (after hiding the content) (the string is the next tile id)

        // first time: [onOpen] -> [onDisplay] -> (display shown)
        // on restore: [onRestore] -> [onDisplay] -> (display shown)
        // on remove (no history): (display hidded) -> [onHide] -> [onRemove]
        // on hide (history): (display hidded) -> [onHide]
    }
}