using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.UI;
using Nox.CCK;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Object = UnityEngine.Object;

namespace api.nox.game.Tiles {
    
    internal class SettingTileManager : TileManager
    {
        internal Dictionary<string, SettingHandler> settingHandlers = new();
        private EventSubscription _sub;

        internal SettingTileManager()
        {
            _sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("game.setting", OnSettingHandler);
        }

        
        private void OnSettingHandler(EventData context)
        {
            if (context.Data[0] is not SettingHandler handler) return;
            if (settingHandlers.ContainsKey(handler.id) && handler.GetPages == null)
            {
                settingHandlers.Remove(handler.id);
                // // if (tile != null) UpdateContent(tile);
                // if (selectedHandler == handler.id)
                //     OnSelectHandler(null, null, null);
                return;
            }
            if (handler.GetPages == null) return;
            if (settingHandlers.ContainsKey(handler.id))
                settingHandlers[handler.id] = handler;
            else settingHandlers.Add(handler.id, handler);
        }

        internal void OnDispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_sub);
            settingHandlers = null;
        }

        
        internal void SendTile(EventData context)
        {
            var tile = new TileObject() { id = "api.nox.game.settings", context = context };
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = (str) => OnOpen(tile, tile.content);
            tile.onHide = (str) => OnHide(tile, tile.content);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        
        internal GameObject OnGetContent(TileObject tile, Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.settings");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = tile.id;
            return content;
        }

        
        internal void OnDisplay(TileObject tile, GameObject content)
        {
            Debug.Log("NavigationTileManager.OnDisplay");
            if (settingHandlers.Count > 0)
                OnSelectHandler(tile, content, settingHandlers.First().Value.id);
            UpdateContent(tile, content);
        }

        private void UpdateContent(TileObject tile, GameObject content)
        {
            
        }

        private void OnSelectHandler(TileObject tile, GameObject content, string id)
        {

        }

        internal void OnOpen(TileObject tile, GameObject content)
        {
            Debug.Log("SettingTileManager.OnOpen");
        }

        internal void OnHide(TileObject tile, GameObject content)
        {
            Debug.Log("SettingTileManager.OnHide");
        }



    }
}