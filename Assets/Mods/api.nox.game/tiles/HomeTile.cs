using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace api.nox.game
{
    internal class HomeTileManager
    {
        internal GameClientSystem clientMod;
        private EventSubscription widgetsub;
        private TileObject homeTile;

        internal HomeTileManager(GameClientSystem clientMod)
        {
            this.clientMod = clientMod;
            this.widgetsub = clientMod.coreAPI.EventAPI.Subscribe("game.widget", OnWidget);
        }

        internal void OnDispose()
        {
            clientMod.coreAPI.EventAPI.Unsubscribe(widgetsub);
        }

        internal Dictionary<string, HomeWidget> widgets = new();
        private void OnWidget(EventData context)
        {
            var widget = (context.Data[0] as ShareObject).Convert<HomeWidget>();
            if (widgets.ContainsKey(widget.id) && widget.GetContent == null)
            {
                widgets.Remove(widget.id);
                return;
            }
            if (widget.GetContent == null) return;
            if (widgets.ContainsKey(widget.id))
                widgets[widget.id] = widget;
            else widgets.Add(widget.id, widget);
            UpdateWidgets();
        }

        internal void SendTile(EventData context)
        {
            if (homeTile != null)
            {
                clientMod.coreAPI.EventAPI.Emit("game.tile", homeTile);
                return;
            }
            var tile = new TileObject();
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.home");
            tile.content = Object.Instantiate(pf);
            tile.id = "api.nox.game.home";
            tile.onOpen = (string previous) => OnOpen(context, previous);
            tile.onRemove = () => OnRemove(context);
            tile.onRestore = (string previous) => OnRestore(context, previous);
            tile.onDisplay = (string previous) => { };
            tile.onHide = (string next) => { };
            homeTile = tile;
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }
        
        internal void UpdateWidgets()
        {
            if (homeTile == null) return;
            Debug.Log("Updating home tile");
            var rect = Reference.GetReference("game.home.widgets", homeTile.content).GetComponent<MenuGridder>();
            // remove to the parent all the children
            foreach (Transform child in rect.transform)
                Object.Destroy(child.gameObject);
            foreach (var widget in widgets.Values)
            {
                Debug.Log("Adding widget to home tile");
                var go = widget.GetContent(rect.transform);
                var gi = go.GetComponent<MenuGridderItem>();
                gi.size = new Vector2(widget.width, widget.height);
            }

            UniTask.Create(async() => {
                await UniTask.DelayFrame(1);
                rect.UpdateContent();
            }).Forget();
        }

        internal void OnOpen(EventData context, string previous)
        {
            Debug.Log("Opening home tile");
            UpdateWidgets();
        }

        internal void OnRestore(EventData context, string previous)
        {
            Debug.Log("Restoring home tile");
            UpdateWidgets();
        }

        internal void OnRemove(EventData context)
        {
            Debug.Log("Removing home tile");
            homeTile = null;
        }
    }
}