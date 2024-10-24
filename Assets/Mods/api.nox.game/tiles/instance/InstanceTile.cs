
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using api.nox.game.sessions;
using api.nox.game.UI;
using UnityEngine.Events;
using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using api.nox.network.Instances;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using api.nox.network;
using api.nox.network.Users;

namespace api.nox.game.Tiles
{
    internal class InstanceTileManager : TileManager
    {
        private EventSubscription InstanceFetchSub;
        [Serializable] public class InstanceFetchedEvent : UnityEvent<Instance> { }
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
            public UnityAction<Instance> OnInstanceFetched;
            public Instance Instance
            {
                get => GetData<Instance>(0);
                set => SetData(0, value);
            }

            public World World
            {
                get => GetData<World>(1);
                set => SetData(1, value);
            }

            public WorldAsset Asset
            {
                get => GetData<WorldAsset>(2);
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
            tile.onRemove = () => OnRemove(tile);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        internal void OnRemove(InstanceTileObject tile)
        {
            if (tile.OnInstanceFetched != null)
                OnInstanceFetched.RemoveListener(tile.OnInstanceFetched);
            tile.OnInstanceFetched = null;
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
            FetchLocation(tile, content).Forget();
            OnClickRefreshPlayers(tile, content).Forget();
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
            var instance = context.Data[0] as Instance;
            OnInstanceFetched?.Invoke(instance);
        }

        private void OnInstanceTileUpdate(InstanceTileObject tile, GameObject content, Instance instance)
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
            refresh_instance.onClick.AddListener(() => OnClickRefreshInstance(tile, content).Forget());

            var refresh_players = Reference.GetReference("refresh_players", content).GetComponent<Button>();
            refresh_players.onClick.RemoveAllListeners();
            refresh_players.onClick.AddListener(() => OnClickRefreshPlayers(tile, content, true).Forget());

            UpdateRelay(tile, content);
        }

        private async UniTask FetchLocation(InstanceTileObject tile, GameObject content)
        {
            var instance = tile.Instance;
            var location = await LocationIP.LocationIP.FetchLocation(instance.address.Split(':')[0]);
            var flag = Reference.GetReference("flag", content);
            var flag_img = Reference.GetReference("flagimg", flag).GetComponent<RawImage>();
            flag.SetActive(false);
            if (location != null && location.success && !string.IsNullOrEmpty(location.GetFlagImg()))
                try { _ = UpdateTexure(flag_img, location.GetFlagImg()).ContinueWith((bool a) => flag.SetActive(a)); }
                catch (Exception e) { Debug.LogError(e); }
        }

        private async UniTask OnClickRefreshInstance(InstanceTileObject tile, GameObject content)
        {
            var refresh_instance = Reference.GetReference("refresh_instance", content).GetComponent<Button>();
            if (!refresh_instance.interactable) return;
            refresh_instance.interactable = false;
            var instance = tile.Instance;
            if (instance == null)
            {
                Debug.LogError("Instance is null");
                refresh_instance.interactable = true;
                return;
            }

            instance = await GameClientSystem.Instance.NetworkAPI.Instance.GetInstance(instance.server, instance.id);
            if (instance == null)
            {
                Debug.LogError("Instance is null");
                return;
            }

            tile.Instance = instance;
            FetchLocation(tile, content).Forget();

            refresh_instance.interactable = true;
            UpdateContent(tile, content);

            ForceUpdateLayout.UpdateManually(content);
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
                    if (currentSession != null && currentSession.Controller is OnlineController controller)
                    {
                        if (controller.InstanceId != instance.id || controller.Server != instance.server)
                            gotobtn.interactable = true;
                    }
                    else gotobtn.interactable = true;
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

            var user = GameClientSystem.Instance.NetworkAPI.User.CurrentUser;
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


        private async UniTask OnClickRefreshPlayers(InstanceTileObject tile, GameObject content, bool isButton = false)
        {
            var refresh_players = Reference.GetReference("refresh_players", content).GetComponent<Button>();
            if (!refresh_players.interactable) return;
            refresh_players.interactable = false;

            if (isButton)
            {
                var refresh_instance = Reference.GetReference("refresh_instance", content).GetComponent<Button>();
                if (!refresh_instance.interactable)
                {
                    refresh_players.interactable = true;
                    return;
                }

                await OnClickRefreshInstance(tile, content);
            }

            List<UniTask> tasks = new();

            var container = Reference.GetReference("players", content).GetComponent<RectTransform>();
            foreach (Transform child in container.transform)
                Object.Destroy(child.gameObject);

            var players = tile.Instance.players;
            Reference.GetReference("players.title", content).GetComponent<TextLanguage>().UpdateText(new string[] { players.Length.ToString() });

            Dictionary<string, List<string>> requests = new();
            foreach (var player in players)
            {
                var identifier = UserIdentifier.FromString(player.user);
                var server = identifier.IsLocal() ? tile.Instance.server : identifier.server;
                if (!requests.ContainsKey(server))
                    requests[server] = new List<string>();
                requests[server].Add(player.user);
            }

            foreach (var server in requests.Keys)
                tasks.Add(FetchWorkPlayers(tile, content, server, requests[server].ToArray()));
            await UniTask.WhenAll(tasks);

            refresh_players.interactable = true;
        }


        private async UniTask FetchWorkPlayers(InstanceTileObject tile, GameObject content, string address, string[] players)
        {
            var res = await GameClientSystem.Instance.NetworkAPI.User.SearchUsers(new()
            {
                user_ids = players,
                server = address,
                limit = 100
            });

            List<User> users = new();
            while (res != null && res.HasNext())
            {
                users.AddRange(res.users);
                res = await res.Next();
            }

            if (res != null)
                users.AddRange(res.users);

            var container = Reference.GetReference("players", content);
            var user_prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.instance.player");
            user_prefab.SetActive(false);

            foreach (var user in users)
            {
                var user_tile = Object.Instantiate(user_prefab, container.transform);
                Reference.GetReference("title", user_tile).GetComponent<TextLanguage>().UpdateText(new string[] { user.display });
                user_tile.SetActive(true);
                var thumbnail = Reference.GetReference("thumbnail", user_tile).GetComponent<RawImage>();
                if (!string.IsNullOrEmpty(user.thumbnail))
                    UpdateTexure(thumbnail, user.thumbnail).Forget();
                var button = Reference.GetReference("button", user_tile).GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => MenuManager.Instance.SendGotoTile(tile.MenuId, "game.user", user));
            }

            ForceUpdateLayout.UpdateManually(container);
        }

    }
}