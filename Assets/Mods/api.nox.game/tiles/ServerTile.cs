
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.CCK.Servers;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;

namespace api.nox.game
{
    internal class ServerTileManager
    {
        internal GameClientSystem clientMod;
        private TileObject tile;
        private EventSubscription eventServerUpdate;
        private HomeWidget serverMeWidget;

        internal ServerTileManager(GameClientSystem clientMod)
        {
            Debug.Log("ServerTileManager");
            this.clientMod = clientMod;
            eventServerUpdate = clientMod.coreAPI.EventAPI.Subscribe("network.server", OnServerUpdate);
            Debug.Log("ServerTileManager initialized.");
            Initialization().Forget();
        }

        private void OnServerUpdate(EventData context)
        {
            if (context.Data[1] as bool? ?? false) return;
            var server = (context.Data[0] as ShareObject).Convert<Server>();
            if (server == null) OnServerDisconnect();
            else OnServerConnect(server);
        }

        private async UniTask<bool> UpdateTexure(RawImage img, string url)
        {
            var tex = await clientMod.coreAPI.NetworkAPI.FetchTexture(url);
            if (tex != null)
            {
                img.texture = tex;
                return true;
            }
            else return false;
        }

        private void OnServerDisconnect()
        {
            Debug.Log("Server disconnected.");
            if (serverMeWidget != null)
            {
                serverMeWidget.GetContent = null;
                clientMod.coreAPI.EventAPI.Emit("game.widget", serverMeWidget);
                serverMeWidget = null;
            }
        }


        private void OnServerConnect(Server server)
        {
            Debug.Log("Server connected: " + server.title);
            if (serverMeWidget == null)
                serverMeWidget = new HomeWidget
                {
                    id = "game.server.me",
                    width = 3,
                    height = 2,
                };
            serverMeWidget.GetContent = (Transform parent) =>
            {
                var baseprefab = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
                var prefab = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget.serverme");
                var btn = Object.Instantiate(baseprefab, parent);
                var btncontent = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
                var dis = Reference.GetReference("display", btncontent).GetComponent<TextLanguage>();
                dis.arguments = new string[] { server.title };
                dis.UpdateText();
                var ban = Reference.GetReference("icon", btncontent).GetComponent<RawImage>();
                var iconmask = Reference.GetReference("iconmask", btncontent);
                iconmask.SetActive(false);
                if (!string.IsNullOrEmpty(server.icon))
                {
                    UniTask uniTask = UpdateTexure(ban, server.icon).ContinueWith(_ => iconmask.SetActive(true));
                }
                var btnref = Reference.GetReference("button", btn).GetComponent<Button>();
                btnref.onClick.AddListener(OnClickWidget);
                return btn;
            };
            clientMod.coreAPI.EventAPI.Emit("game.widget", serverMeWidget);
        }

        private void OnClickWidget()
        {
            var server = clientMod.coreAPI.NetworkAPI.GetCurrentServer();
            if (server == null) return;
            clientMod.GotoTile("game.server", server);
        }

        internal void OnDispose()
        {
            clientMod.coreAPI.EventAPI.Unsubscribe(eventServerUpdate);
        }

        internal void SendTile(EventData context)
        {
            if (this.tile != null)
            {
                clientMod.coreAPI.EventAPI.Emit("game.tile", this.tile);
                return;
            }
            var server = ((context.Data[1] as object[])[0] as ShareObject).Convert<Server>();
            var tile = new TileObject()
            {
                onRemove = () => { 
                    this.tile = null;
                }
            };
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.server");
            tile.content = Object.Instantiate(pf);
            UpdateContent(tile.content, server);
            tile.id = "api.nox.game.server";
            this.tile = tile;
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        private void UpdateContent(GameObject tile, Server server)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>().arguments = new string[] { server.title };
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { server.title };
            Reference.GetReference("address", tile).GetComponent<TextLanguage>().arguments = new string[] { server.address };

            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(server.icon)) UpdateTexure(icon, server.icon).Forget();
        }


        private async UniTask Initialization()
        {
            var server = clientMod.coreAPI.NetworkAPI.GetCurrentServer();
            server ??= await clientMod.coreAPI.NetworkAPI.ServerAPI.GetMyServer();
            if (server != null) OnServerConnect(server);
            else OnServerDisconnect();
        }
    }
}