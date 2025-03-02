using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.controllers;
using api.nox.game.ui;
using Logger = Nox.CCK.Utils.Logger;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Object = UnityEngine.Object;

namespace api.nox.game.UI
{
    public class MenuManager : IDisposable
    {
        private static MenuManager _instance;
        public static MenuManager Instance => _instance ??= new MenuManager();

        private HashSet<Menu> _menus;

        private MenuManager() => _menus = new HashSet<Menu>();

        public void Dispose()
        {
            foreach (var menu in _menus.ToArray())
            {
                Unregister(menu);
                menu.Dispose();
            }

            _menus.Clear();
            _menus = null;
            _instance = null;
        }

        public void Register(Menu menu) => _menus.Add(menu);
        public void Unregister(Menu menu) => _menus.Remove(menu);
        public void Unregister(int menuId) => _menus.RemoveWhere(m => m.Id == menuId);
        public Menu GetMenu(int menuId) => _menus.FirstOrDefault(m => m.Id == menuId);

        internal void SendGotoTile(int menuId, string page, params object[] args)
            => GameClientSystem.CoreAPI.EventAPI.Emit("game.tile.goto", menuId, page, args);

        internal void SendTile(int menuId, TileObject tile)
            => GameClientSystem.CoreAPI.EventAPI.Emit("game.tile", menuId, tile);
        
        public ViewPortMenu GetViewPortMenu()
        {
            foreach (var m in _menus)
                if (m is ViewPortMenu viewPortMenu)
                    return viewPortMenu;
            var asset = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/ui/viewport.prefab");
            Logger.Log($"Creating ViewPortMenu from {asset}");
            var menu = Object.Instantiate(
                asset,
                PlayerController.instance.transform
            ).GetComponent<ViewPortMenu>();
            menu.IsVisible = false;
            Register(menu);
            Logger.Log($"Creating ViewPortMenu as {menu.Id}");
            SendGotoTile(menu.Id, menu.initTile);
            return menu;
        }

        public PhysicalMenu GetPhysicalMenu()
        {
            foreach (var m in _menus)
                if (m is PhysicalMenu physicalMenu)
                    return physicalMenu;
            var go = Object.Instantiate(
                GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/ui/physical.prefab"));
            var menu = go.GetComponent<PhysicalMenu>();
            go.name = $"[{menu.GetType().Name}]";
            Object.DontDestroyOnLoad(go);
            menu.IsVisible = false;
            Register(menu);
            Logger.Log($"Creating PhysicalMenu as {menu.Id}");
            SendGotoTile(menu.Id, menu.initTile);
            return menu;
        }

        internal void OnTile(EventData context)
        {
            var menuId = (context.Data[0] as int?) ?? 0;
            if (menuId == 0) return;
            var menu = _menus.FirstOrDefault(m => m.Id == menuId);
            if (!menu) return;
            var tile = (context.Data[1] as ShareObject)?.Convert<TileObject>();
            menu.History.Add(((ShareObject)tile)?.Convert<HistoryTile>());
        }

        internal Menu[] GetMenus() => _menus.ToArray();
    }
}