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

        internal HomeTileManager(GameClientSystem clientMod)
        {
            this.clientMod = clientMod;
            widgetsub = clientMod.coreAPI.EventAPI.Subscribe("game.widget", OnWidget);
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

        private GameObject tile;
        internal void SendTile(EventData context)
        {
            var tile = new TileObject()
            {
                id = "api.nox.game.home",
                onRemove = () => OnRemove(context),
                GetContent = (Transform tf) =>
            {
                var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.home");
                pf.SetActive(false);
                this.tile = Object.Instantiate(pf, tf);
                this.tile.name = "game.home";
                UpdateWidgets();
                return this.tile;
            }
            };
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        internal void UpdateWidgets()
        {
            if (tile == null) return;
            var rect = Reference.GetReference("game.home.widgets", this.tile).GetComponent<MenuGridder>();
            foreach (Transform child in rect.transform)
                Object.Destroy(child.gameObject);
            foreach (var widget in widgets.Values)
            {
                var go = widget.GetContent(rect.transform);
                var gi = go.GetComponent<MenuGridderItem>();
                gi.size = new Vector2(widget.width, widget.height);
            }

            UniTask.Create(async () =>
            {
                await UniTask.DelayFrame(1);
                rect.UpdateContent();
            }).Forget();
        }

        internal void OnRemove(EventData context)
        {
            tile = null;
        }
    }
}