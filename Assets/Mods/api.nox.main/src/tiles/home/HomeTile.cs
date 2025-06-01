/*using System.Collections.Generic;
using api.nox.game.UI;
using Nox.CCK.Mods.Events;
using Logger = Nox.CCK.Utils.Logger;
using UnityEngine;

namespace api.nox.game.Tiles
{
    internal class HomeTileManager : TileManager
    {
        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            Logger.Log("HomeTileManager.SendTile");
            var menuId = (context.Data[0] as int?) ?? 0;
            var tile = new TileObject() { id = "api.nox.game.home", context = context };
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(menuId, tile, gameObject);
            MenuManager.Instance.SendTile(menuId, tile);
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tf"></param>
        /// <returns>Content of the tile</returns>
        internal GameObject OnGetContent(TileObject tile, Transform tf)
        {
            Logger.Log("HomeTileManager.GetTileContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/game.home.prefab");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.home";
            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnDisplay(int menuId, TileObject tile, GameObject content)
        {
            Logger.Log("HomeTileManager.OnDisplay");
            // _widgets.UpdateWidgets(menuId, content);
        }

        private List<HomeWithMenu> GetMenuWithHome()
        {
            var menus = MenuManager.Instance.GetMenus();
            var tiles = new List<HomeWithMenu>();
            foreach (var menu in menus)
            {
                var tile = menu.History.GetCurrent();
                if (tile != null && tile.id == "api.nox.game.home")
                    tiles.Add(new HomeWithMenu() { home = tile, menuId = menu.Id });
            }

            return tiles;
        }

        /*
        private WidgetManager _widgets;

        internal HomeTileManager()
        {
            _widgets = new WidgetManager();
            _widgets.OnWidgetsUpdate.AddListener(widgets =>
            {
                foreach (var tile in GetMenuWithHome())
                    _widgets.UpdateWidgets(tile.menuId, tile.home.content);
            });
        }#1#

        internal void OnDispose()
        {/*
            _widgets.Dispose();
            _widgets.OnWidgetsUpdate = null;
            _widgets = null;#1#
        }
    }

    class HomeWithMenu
    {
        public HistoryTile home;
        public int menuId;
    }
}*/