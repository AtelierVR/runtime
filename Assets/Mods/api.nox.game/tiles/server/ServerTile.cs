
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.CCK.Servers;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using Nox.SimplyLibs;
using api.nox.game.UI;
using api.nox.game.Tiles;
using System;
using Object = UnityEngine.Object;
using Newtonsoft.Json.Linq;

namespace api.nox.game.Tiles
{
    internal class ServerTileManager : TileManager
    {
        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            Debug.Log("ServerTileManager.SendTile");
            var tile = new TileObject() { id = "api.nox.game.server", context = context };
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = (str) => OnOpen(tile, tile.content);
            tile.onHide = (str) => OnHide(tile, tile.content);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tf"></param>
        /// <returns>Content of the tile</returns>
        internal GameObject OnGetContent(TileObject tile, Transform tf)
        {
            Debug.Log("ServerTileManager.GetTileContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.server");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.server";
            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnDisplay(TileObject tile, GameObject content)
        {
            Debug.Log("ServerTileManager.OnDisplay");
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnOpen(TileObject tile, GameObject content)
        {
            Debug.Log("ServerTileManager.OnOpen");
            var server = tile.GetData<ShareObject>(0)?.Convert<SimplyServer>();
            UpdateContent(content, server);
        }

        /// <summary>
        /// Handle the hiding of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnHide(TileObject tile, GameObject content)
        {
            Debug.Log("ServerTileManager.OnHide");
        }

        private void UpdateContent(GameObject tile, SimplyServer server)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>().UpdateText(new string[] { server.title });
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().UpdateText(new string[] { server.title });
            Reference.GetReference("address", tile).GetComponent<TextLanguage>().UpdateText(new string[] { server.address });
            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(server.icon)) UpdateTexure(icon, server.icon).Forget();
        }

        internal SimplyWebSocket ws;

        internal ServerTileManager()
        {
            Debug.Log("ServerTileManager initializing.");
            Initialization().Forget();
        }

        private async void OnServerDisconnect()
        {
            Debug.Log("Server disconnected.");
            if (ws != null)
            {
                await ws.Close();
                ws = null;
            }
        }

        private async UniTask OnServerConnect(SimplyServer server)
        {
            Debug.Log("Server connected: " + server.title);
            ws = await server.GetOrConnect();
            if (ws != null)
            {
                Debug.Log("Server connected: " + server.title);
                ws.OnMessage += (msg) => OnWSMessage(msg);
                ws.OnClose += () => Debug.Log("Server closed.");
            }
        }

        private void OnWSMessage(string msg)
        {
            Debug.Log("Server message: " + msg);
            JObject obj = JObject.Parse(msg);
            var type = obj.TryGetValue("type", out var typeobj) ? typeobj.Value<string>() : null;
            Debug.Log("Server message type: " + type);
            switch (type)
            {
                case "user_update":
                    OnUserUpdate(obj.TryGetValue("data", out var data) ? data.ToObject<JObject>() : null);
                    break;
                case "user_connect":
                    Debug.Log("User connect.");
                    break;
                case "user_disconnect":
                    Debug.Log("User disconnect.");
                    break;
            }
        }

        private async UniTask Initialization()
        {
            var server = GameClientSystem.Instance.NetworkAPI.GetCurrentServer();
            server ??= await GameClientSystem.Instance.NetworkAPI.Server.GetMyServer();
            if (server != null) await OnServerConnect(server);
            else OnServerDisconnect();
        }

        public void OnDispose()
        {
            ws?.Close().Forget();
            ws = null;
        }

        private void OnUserUpdate(JObject data)
        {
            var user = JsonUtility.FromJson<SimplyUserMe>(data.ToString());
            GameSystem.Instance.CoreAPI.EventAPI.Emit(new NetEventContext("user_update", user));
        }

        public class NetEventContext : EventContext
        {
            private readonly object[] _data;
            private readonly string _eventName;
            public NetEventContext(string eventName, params object[] data)
            {
                _eventName = eventName;
                _data = data;
            }
            public object[] Data => _data;
            public string Destination => null;
            public string EventName => _eventName;
            public EventEntryFlags Channel => EventEntryFlags.Client | EventEntryFlags.Main | EventEntryFlags.Editor;
        }
    }
}
