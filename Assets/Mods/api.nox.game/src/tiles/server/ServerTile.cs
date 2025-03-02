using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.UI;
using api.nox.game.UI;
using Object = UnityEngine.Object;
using Newtonsoft.Json.Linq;
using api.nox.network.Servers;
using api.nox.network.Users;
using Logger = Nox.CCK.Utils.Logger;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using api.nox.network;
using UnityEngine.Events;
using Transform = UnityEngine.Transform;

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
            Logger.Log("ServerTileManager.SendTile");
            var tile = new TileObject { id = "api.nox.game.server", context = context };
            tile.GetContent = OnGetContent;
            tile.onOpen = _ => OnOpen(tile, tile.content);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tf"></param>
        /// <returns>Content of the tile</returns>
        private GameObject OnGetContent(Transform tf)
        {
            Logger.Log("ServerTileManager.GetTileContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/game.server.prefab");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.server";
            return content;
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        private void OnOpen(TileObject tile, GameObject content)
        {
            Logger.Log("ServerTileManager.OnOpen");
            var server = tile.GetData<Server>(0);
            UpdateContent(content, server);
        }

        private void UpdateContent(GameObject tile, Server server)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>()
                .UpdateText(new[] { server.title });
            Reference.GetReference("title", tile).GetComponent<TextLanguage>()
                .UpdateText(new[] { server.title });
            Reference.GetReference("address", tile).GetComponent<TextLanguage>()
                .UpdateText(new[] { server.address });
            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(server.icon)) UpdateTexture(icon, server.icon).Forget();
        }

        private INoxObject _ws; // WebSocket

        private async UniTask OnServerDisconnect()
        {
            Logger.Log("Server disconnected.");
            if (_ws == null) return;
            await _ws.InvokeAsyncMethod("Close");
            _ws = null;
        }

        private async UniTask OnServerConnect(INoxObject server)
        {
            Logger.Log("Server connected: " + server.GetField<string>("title"));
            _ws = await server.CallAsyncMethod("GetOrConnect");
            if (_ws != null)
            {
                Logger.Log("Server connected: " + server.GetField<string>("title"));
                _ws.GetField<UnityEvent<string>>("OnMessage").AddListener(OnWSMessage);
                Logger.Log($"{_ws.GetType()}: [{string.Join(", ", _ws.GetFields())}]");
                _ws.GetField<UnityEvent>("OnClose").AddListener(OnWSClose);
            }
        }

        private void OnWSClose()
        {
            if (_ws == null) return;
            Logger.Log("Server disconnected.");
            _ws.GetField<UnityEvent<string>>("OnMessage").RemoveListener(OnWSMessage);
            _ws.GetField<UnityEvent>("OnClose").RemoveListener(OnWSClose);
            _ws = null;
        }

        private void OnWSMessage(string msg)
        {
            Logger.Log("Server message: " + msg);
            var obj = JObject.Parse(msg);
            var type = obj.TryGetValue("type", out var typed) ? typed.Value<string>() : null;
            Logger.Log("Server message type: " + type);
            switch (type)
            {
                case "user_update":
                    OnUserUpdate(obj.TryGetValue("data", out var data) ? data.ToObject<JObject>() : null);
                    break;
                case "user_connect":
                    Logger.Log("User connect.");
                    break;
                case "user_disconnect":
                    Logger.Log("User disconnect.");
                    break;
            }
        }

        internal async UniTask Initialization()
        {
            var serverApi = GameClientSystem.NetworkAPI.GetField("Server");
            var server = serverApi.CallMethod("GetCurrentServer");
            server ??= await serverApi.CallAsyncMethod("GetMyServer");
            if (server != null) await OnServerConnect(server);
            else OnServerDisconnect().Forget();
        }

        public async UniTask OnDisposeAsync()
        {
            if (_ws != null)
                await _ws.InvokeAsyncMethod("Close");
            _ws = null;
        }

        private void OnUserUpdate(JObject data)
        {
            var user = JsonUtility.FromJson<UserMe>(data.ToString());
            GameSystem.Instance.CoreAPI.EventAPI.Emit(new utils.EventContext(null, "user_update", EventEntryFlags.Internal, user));
            NetCache.Set(user);
        }
    }
}