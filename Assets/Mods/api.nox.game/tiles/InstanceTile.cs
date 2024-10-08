
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using Nox.SimplyLibs;
using api.nox.game.sessions;
using api.nox.game.LocationIP;
using api.nox.game.UI;

namespace api.nox.game.Tiles
{
    internal class InstanceTileManager : TileManager
    {
        /// <summary>
        /// Send a tile to the menu manager
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            var tile = new TileObject() { id = "api.nox.game.instance", context = context };
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = (str) => OnOpen(tile, tile.content);
            tile.onHide = (str) => OnHide(tile, tile.content);
            MenuManager.Instance.SendTile((context.Data[0] as int?) ?? 0, tile);
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tf"></param>
        /// <returns>Content of the tile</returns>
        internal GameObject OnGetContent(TileObject tile, Transform tf)
        {
            Debug.Log("InstanceTileManager.GetTileContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.instance");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.instance";
            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnDisplay(TileObject tile, GameObject content)
        {
            Debug.Log("InstanceTileManager.OnDisplay");
            var instance = (tile.context.Data[1] as ShareObject).Convert<SimplyInstance>();
            var world = (tile.context.Data[2] as ShareObject).Convert<SimplyWorld>();
            var asset = (tile.context.Data[3] as ShareObject).Convert<SimplyWorldAsset>();
            UpdateContent(content, instance, world, asset);
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnOpen(TileObject tile, GameObject content)
        {
            Debug.Log("InstanceTileManager.OnOpen");
        }

        /// <summary>
        /// Handle the hiding of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnHide(TileObject tile, GameObject content)
        {
            Debug.Log("InstanceTileManager.OnHide");
        }






        private void UpdateContent(GameObject tile, SimplyInstance instance, SimplyWorld world, SimplyWorldAsset asset, IPData location = null)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.title };
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.title };
            Reference.GetReference("description", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.description };
            Reference.GetReference("ai.address", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.server };
            Reference.GetReference("ai.id", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.id.ToString() };
            Reference.GetReference("ai.relay", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.address ?? "Not openned" };

            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(instance.thumbnail)) UpdateTexure(icon, instance.thumbnail).Forget();

            var gotobtn = Reference.GetReference("goto.button", tile).GetComponent<Button>();
            gotobtn.onClick.RemoveAllListeners();
            gotobtn.interactable = false;
            if (!string.IsNullOrEmpty(instance.address))
            {
                UpdateLocation(tile, instance).Forget();
                gotobtn.interactable = true;
                var relay = GameClientSystem.Instance.NetworkAPI.Relay.GetRelay(instance.address);
                if (relay != null)
                {
                    var currentSession = GameSystem.instance.sessionManager.CurrentSession;
                    if (currentSession != null && currentSession.controller is OnlineController controller)
                    {
                        var ins = controller.GetInstance();
                        if (ins == null || ins.id != instance.id || ins.server != instance.server)
                            gotobtn.interactable = true;
                    }
                    else gotobtn.interactable = true;
                }

                UpdatePlayers(tile, instance);
            }
            else Debug.Log("Instance not openned: " + instance.title);

            Debug.Log("InstanceTileManager: " + instance.title + " " + instance.address + " " + instance.server + " " + instance.players.Length);

            if (gotobtn.interactable)
                gotobtn.onClick.AddListener(() => JoinOnlineSession(tile, instance, world, asset).Forget());
        }

        private void UpdatePlayers(GameObject tile, SimplyInstance instance)
        {
            var player_node = Reference.GetReference("players", tile);
            Reference.GetReference("players.title", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.players.Length.ToString() };
            var player_prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.instance.player");
            player_prefab.SetActive(false);
            foreach (Transform child in player_node.transform)
                Object.Destroy(child.gameObject);
            foreach (var player in instance.players)
            {
                var player_tile = Object.Instantiate(player_prefab, player_node.transform);
                Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { player.display };
                player_tile.SetActive(true);
                // Reference.GetReference("display", player_tile).GetComponent<TextLanguage>().arguments = new string[] { player.display };
                // Reference.GetReference("user", player_tile).GetComponent<TextLanguage>().arguments = new string[] { player.user };
            }
        }

        private async UniTask UpdateLocation(GameObject tile, SimplyInstance instance)
        {
            var location = await LocationIP.LocationIP.FetchLocation(instance.address.Split(':')[0]);
            if (location == null || !location.success) return;
            var flag = Reference.GetReference("flag", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(location.GetFlagImg())) UpdateTexure(flag, location.GetFlagImg()).Forget();
        }

        private async UniTask JoinOnlineSession(GameObject tile, SimplyInstance instance, SimplyWorld world, SimplyWorldAsset asset)
        {
            Debug.Log("JoinOnlineSession: " + instance.title);
            var gotobtn = Reference.GetReference("goto.button", tile).GetComponent<Button>();
            if (!gotobtn.interactable) return;
            gotobtn.interactable = false;


            var user = GameClientSystem.Instance.NetworkAPI.GetCurrentUser();
            if (user == null)
            {
                gotobtn.interactable = true;
                return;
            }

            var session = GameSystem.instance.sessionManager.GetSession(instance.server, instance.id);
            if (session != null)
            {
                session.SetCurrent();
                return;
            }
            var token = await GameClientSystem.Instance.NetworkAPI.Auth.GetToken(instance.server);
            if (token == null)
            {
                gotobtn.interactable = true;
                return;
            }

            var controller = new OnlineController(instance)
            {
                connectionData = new()
                {
                    relay_address = instance.address,
                    master_address = instance.server,
                    authentificate = new()
                    {
                        token = token.token,
                        server_address = user.server,
                        use_integrity_token = token.isIntegrity,
                        user_id = user.id
                    }
                }
            };
            session = GameSystem.instance.sessionManager.New(controller, instance.server, instance.id);
            session.world = world;
            session.worldAsset = asset;
            if (await controller.Prepare())
                session.SetCurrent();
            else session.Dispose();

            gotobtn.interactable = true;
            return;
        }
    }
}