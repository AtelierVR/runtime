
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
using UnityEngine.Events;
using System;
using Object = UnityEngine.Object;

namespace api.nox.game.Tiles
{
    internal class InstanceTileManager : TileManager
    {
        private EventSubscription InstanceFetchSub;
        [Serializable] public class InstanceFetchedEvent : UnityEvent<SimplyInstance> { }
        public InstanceFetchedEvent OnInstanceFetched;

        internal InstanceTileManager()
        {
            InstanceFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("instance_fetch", OnFetchInstance);
            OnInstanceFetched = new InstanceFetchedEvent();
        }

        internal void OnDispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(InstanceFetchSub);
            OnInstanceFetched?.RemoveAllListeners();
            OnInstanceFetched = null;
        }

        internal class InstanceTileObject : TileObject
        {
            public UnityAction<SimplyInstance> OnInstanceFetched;
            public SimplyInstance Instance
            {
                get => GetData<ShareObject>(0)?.Convert<SimplyInstance>();
                set => SetData(0, value);
            }

            public SimplyWorld World
            {
                get => GetData<ShareObject>(1)?.Convert<SimplyWorld>();
                set => SetData(1, value);
            }

            public SimplyWorldAsset Asset
            {
                get => GetData<ShareObject>(2)?.Convert<SimplyWorldAsset>();
                set => SetData(2, value);
            }
        }

        /// <summary>
        /// Send a tile to the menu manager
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            var tile = new InstanceTileObject() { id = "api.nox.game.instance", context = context };
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
        internal GameObject OnGetContent(InstanceTileObject tile, Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.instance");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.instance";

            if (tile.OnInstanceFetched != null)
                OnInstanceFetched.RemoveListener(tile.OnInstanceFetched);
            tile.OnInstanceFetched = (user) => OnInstanceTileUpdate(tile, content, user);
            OnInstanceFetched.AddListener(tile.OnInstanceFetched);

            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnDisplay(InstanceTileObject tile, GameObject content)
        {
            Debug.Log("InstanceTileManager.OnDisplay");
            UpdateContent(tile, content);
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnOpen(InstanceTileObject tile, GameObject content)
        {
            Debug.Log("InstanceTileManager.OnOpen");
            UpdateLocation(tile, content).Forget();
        }

        /// <summary>
        /// Handle the hiding of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnHide(InstanceTileObject tile, GameObject content)
        {
            Debug.Log("InstanceTileManager.OnHide");
        }

        private void OnFetchInstance(EventData context)
        {
            var instance = (context.Data[0] as ShareObject).Convert<SimplyInstance>();
            OnInstanceFetched?.Invoke(instance);
        }

        private void OnInstanceTileUpdate(InstanceTileObject tile, GameObject content, SimplyInstance instance)
        {
            var cInstance = tile.Instance;
            if (cInstance == null) return;
            if (cInstance.id != instance.id) return;
            if (cInstance.server != instance.server) return;

            var refresh_instance = Reference.GetReference("refresh_instance", content).GetComponent<Button>();
            if (!refresh_instance.interactable) return;

            tile.Instance = instance;

            UpdateContent(tile, content);
        }

        internal void UpdateContent(InstanceTileObject tile, GameObject content)
        {
            var instance = tile.Instance;
            if (instance == null)
            {
                Debug.LogError("Instance is null");
                return;
            }
            Reference.GetReference("display", content).GetComponent<TextLanguage>().UpdateText(new string[] { instance.title });
            Reference.GetReference("title", content).GetComponent<TextLanguage>().UpdateText(new string[] { instance.title });
            Reference.GetReference("description", content).GetComponent<TextLanguage>().UpdateText(new string[] { instance.description });
            Reference.GetReference("ai.address", content).GetComponent<TextLanguage>().UpdateText(new string[] { instance.server });
            Reference.GetReference("ai.id", content).GetComponent<TextLanguage>().UpdateText(new string[] { instance.id.ToString() });
            Reference.GetReference("ai.relay", content).GetComponent<TextLanguage>().UpdateText(new string[] { instance.address ?? "Not openned" });

            var thumbnail = Reference.GetReference("thumbnail", content).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(instance.thumbnail))
                UpdateTexure(thumbnail, instance.thumbnail).Forget();

            var refresh_instance = Reference.GetReference("refresh_instance", content).GetComponent<Button>();
            refresh_instance.onClick.RemoveAllListeners();
            refresh_instance.onClick.AddListener(() => OnClickRefreshInstance(tile, refresh_instance, content).Forget());

            UpdateRelay(tile, content);
            UpdatePlayers(tile, content);

        }

        private async UniTask UpdateLocation(InstanceTileObject tile, GameObject content)
        {
            var instance = tile.Instance;
            var location = await LocationIP.LocationIP.FetchLocation(instance.address.Split(':')[0]);
            if (location == null || !location.success) return;
            var flag = Reference.GetReference("flag", content).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(location.GetFlagImg())) UpdateTexure(flag, location.GetFlagImg()).Forget();
        }

        private async UniTask OnClickRefreshInstance(InstanceTileObject tile, Button dlb, GameObject content)
        {
            if (!dlb.interactable) return;
            dlb.interactable = false;
            var instance = tile.Instance;
            if (instance == null)
            {
                Debug.LogError("Instance is null");
                return;
            }

            instance = await GameClientSystem.Instance.NetworkAPI.Instance.GetInstance(instance.server, instance.id);
            if (instance == null)
            {
                Debug.LogError("Instance is null");
                return;
            }

            tile.Instance = instance;

            dlb.interactable = true;
            UpdateContent(tile, content);
        }

        private void UpdateRelay(InstanceTileObject tile, GameObject content)
        {
            var instance = tile.Instance;
            var asset = tile.Asset;
            var gotobtn = Reference.GetReference("goto.button", content).GetComponent<Button>();
            gotobtn.onClick.RemoveAllListeners();
            gotobtn.interactable = false;

            if (!string.IsNullOrEmpty(instance.address) && asset != null)
            {
                var relay = GameClientSystem.Instance.NetworkAPI.Relay.GetRelay(instance.address);
                if (relay != null)
                {
                    var currentSession = GameSystem.Instance.SessionManager.CurrentSession;
                    if (currentSession != null && currentSession.controller is OnlineController controller)
                    {
                        var ins = controller.GetInstance();
                        if (ins == null || ins.id != instance.id || ins.server != instance.server)
                        {
                            Debug.Log("Current session is not the same as the instance");
                            gotobtn.interactable = true;
                        }
                    }
                    else
                    {
                        Debug.Log($"No current session {currentSession} {currentSession?.controller}");
                        gotobtn.interactable = true;
                    }
                }
                else
                {
                    Debug.Log("Relay is null");
                    gotobtn.interactable = true;
                }
            }

            if (gotobtn.interactable)
                gotobtn.onClick.AddListener(() => JoinOnlineSession(tile, content).Forget());
        }

        private void UpdatePlayers(InstanceTileObject tile, GameObject content)
        {
            var instance = tile.Instance;
            var player_node = Reference.GetReference("players", content);
            Reference.GetReference("players.title", content).GetComponent<TextLanguage>().UpdateText(new string[] { instance.players.Length.ToString() });
            var player_prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.instance.player");
            player_prefab.SetActive(false);
            foreach (Transform child in player_node.transform)
                Object.Destroy(child.gameObject);
            foreach (var player in instance.players)
            {
                var player_tile = Object.Instantiate(player_prefab, player_node.transform);
                Reference.GetReference("title", content).GetComponent<TextLanguage>().UpdateText(new string[] { player.display });
                player_tile.SetActive(true);
                // Reference.GetReference("display", player_tile).GetComponent<TextLanguage>().arguments = new string[] { player.display };
                // Reference.GetReference("user", player_tile).GetComponent<TextLanguage>().arguments = new string[] { player.user };
            }
        }


        private async UniTask JoinOnlineSession(InstanceTileObject tile, GameObject content)
        {
            var instance = tile.Instance;
            var world = tile.World;
            var asset = tile.Asset;
            if (instance == null || world == null || asset == null)
            {
                Debug.LogError("Instance, World or Asset is null");
                return;
            }

            var gotobtn = Reference.GetReference("goto.button", content).GetComponent<Button>();
            if (!gotobtn.interactable) return;
            gotobtn.interactable = false;

            var user = GameClientSystem.Instance.NetworkAPI.GetCurrentUser();
            if (user == null)
            {
                gotobtn.interactable = true;
                return;
            }

            var session = GameSystem.Instance.SessionManager.GetSession(instance.server, instance.id);
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

            session = GameSystem.Instance.SessionManager.New(controller, instance.server, instance.id);
            session.world = world;
            session.worldAsset = asset;

            if (await controller.Prepare())
            {
                Debug.Log("Session set current");
                session.SetCurrent();
            }
            else
            {
                Debug.Log("Session disposed");
                session.Dispose();
            }

            gotobtn.interactable = true;

            UpdateRelay(tile, content);
        }
    }
}