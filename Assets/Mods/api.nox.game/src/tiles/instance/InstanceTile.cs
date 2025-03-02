using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.UI;
using api.nox.game.UI;
using UnityEngine.Events;
using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using api.nox.network.Instances;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using Logger = Nox.CCK.Utils.Logger;
using api.nox.network;
using api.nox.network.Auths;
using api.nox.network.Users;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using SearchRequest = api.nox.network.Users.SearchRequest;
using Transform = UnityEngine.Transform;

namespace api.nox.game.Tiles
{
    internal class InstanceTileManager : TileManager
    {
        private EventSubscription InstanceFetchSub;

        [Serializable]
        public class InstanceFetchedEvent : UnityEvent<Instance>
        {
        }

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
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/game.instance.prefab");
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
            Logger.Log("InstanceTileManager.OnDisplay");
            UpdateContent(tile, content);
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnOpen(InstanceTileObject tile, GameObject content)
        {
            Logger.Log("InstanceTileManager.OnOpen");
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
            Logger.Log("InstanceTileManager.OnHide");
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
                Logger.LogError("Instance is null");
                return;
            }

            Reference.GetReference("display", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { instance.title });
            Reference.GetReference("title", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { instance.title });
            Reference.GetReference("description", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { instance.description });
            Reference.GetReference("ai.address", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { instance.server });
            Reference.GetReference("ai.id", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { instance.id.ToString() });
            Reference.GetReference("ai.relay", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { instance.address ?? "Not openned" });
            Reference.GetReference("ai.capacity", content).GetComponent<TextLanguage>()
                .UpdateText(
                    instance.capacity == ushort.MaxValue
                        ? "dashboard.instance.advanced_informations.capacity.unlimited"
                        : "dashboard.instance.advanced_informations.capacity",
                    new[] { instance.capacity == ushort.MaxValue ? "Unlimited" : instance.capacity.ToString() }
                );


            var thumbnail = Reference.GetReference("thumbnail", content).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(instance.thumbnail))
                UpdateTexture(thumbnail, instance.thumbnail).Forget();

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
                try
                {
                    _ = UpdateTexture(flag_img, location.GetFlagImg()).ContinueWith((bool a) => flag.SetActive(a));
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
        }

        private async UniTask OnClickRefreshInstance(InstanceTileObject tile, GameObject content)
        {
            var refreshInstance = Reference.GetReference("refresh_instance", content).GetComponent<Button>();
            if (!refreshInstance.interactable) return;
            refreshInstance.interactable = false;
            INoxObject instance = tile.Instance;
            if (instance == null)
            {
                Logger.LogError("Instance is null");
                refreshInstance.interactable = true;
                return;
            }

            instance = await GameClientSystem.NetworkAPI
                .GetField<INoxObject>("Instance")
                .CallMethod<UniTask<INoxObject>>("GetInstance",
                    instance.GetField<string>("server"),
                    instance.GetField<uint>("id"));

            if (instance == null)
            {
                Logger.LogError("Instance is null");
                return;
            }

            tile.Instance = instance as Instance;
            FetchLocation(tile, content).Forget();

            refreshInstance.interactable = true;
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
                var relay = GameClientSystem.RelayAPI?.CallMethod<INoxObject>("GetByAddress", instance.address);
                if (relay != null)
                {
                    var currentSession = GameClientSystem.SessionAPI?.CallMethod<INoxObject>("GetCurrentSession");
                    if (currentSession == null) gotobtn.interactable = true;
                    else if (currentSession.HasMethod("GetController"))
                    {
                        var controller = currentSession.CallMethod<INoxObject>("GetController");
                        if (controller.CallMethod<string>("GetTypeName") == "online")
                        {
                            var instanceId = controller.GetField<uint>("InstanceId");
                            var masterAddress = controller.GetField<string>("MasterAddress");
                            if (instanceId != instance.id || masterAddress != instance.server)
                                gotobtn.interactable = true;
                        }
                        else gotobtn.interactable = true;
                    }
                    else gotobtn.interactable = true;
                }
                else
                {
                    Logger.Log("Relay is null");
                    gotobtn.interactable = true;
                }
            }

            if (gotobtn.interactable)
                gotobtn.onClick.AddListener(() => JoinOnlineSession(tile, content).Forget());
        }

        private async UniTask JoinOnlineSession(InstanceTileObject tile, GameObject content)
        {
            if (GameClientSystem.SessionAPI == null)
                throw new Exception("SessionAPI is null");

            Logger.Log($"Joining online session {tile.Instance.server}:{tile.Instance.id}");
            var instance = tile.Instance;
            var world = tile.World;
            var asset = tile.Asset;
            if (instance == null || world == null || asset == null)
            {
                Logger.LogError("Instance, World or Asset is null");
                return;
            }

            var gotobtn = Reference.GetReference("goto.button", content).GetComponent<Button>();
            if (!gotobtn.interactable) return;
            gotobtn.interactable = false;

            var user = GameClientSystem.NetworkAPI
                .GetField<INoxObject>("User")
                .CallMethod<User>("GetCurrentUser");
            if (user == null)
            {
                gotobtn.interactable = true;
                Logger.LogWarning("You are not logged in");
                return;
            }

            var sessions =
                GameClientSystem.SessionAPI?.CallMethod<INoxObject[]>("GetSessionsWithControllerName", "online");
            INoxObject session = null;
            foreach (var s in sessions ?? Array.Empty<INoxObject>())
            {
                if (s == null) continue;
                if (!s.HasMethod("GetController")) continue;
                var ctl = s.CallMethod<INoxObject>("GetController");
                if (ctl == null) continue;
                var instanceId = ctl.GetField<uint>("InstanceId");
                var masterAddress = ctl.GetField<string>("MasterAddress");

                if (instanceId != instance.id || masterAddress != instance.server) continue;

                session = s;
                break;
            }

            if (session != null)
            {
                await GameClientSystem.SessionAPI!.CallMethod<UniTask>("SetCurrentSession",
                    session.GetField<ushort>("Uid"));
                Logger.Log("Session set current");
                return;
            }

            var token = await GameClientSystem.NetworkAPI
                .GetField<INoxObject>("Auth")
                .CallMethod<UniTask<INoxObject>>("GetToken", instance.server);

            if (token == null)
            {
                gotobtn.interactable = true;
                Logger.LogError("Token is null");
                return;
            }

            var controller = new Dictionary<string, object>()
            {
                { "InstanceId", instance.id },
                { "MasterAddress", instance.server },
                {
                    "ConnectionData", new Dictionary<string, object>()
                    {
                        { "relay_address", instance.address },
                        { "master_address", instance.server },
                        {
                            "authenticate", new Dictionary<string, object>()
                            {
                                { "token", token.GetField<string>("Token") },
                                { "server_address", user.server },
                                { "use_integrity_token", token.GetField<bool>("IsIntegrity") },
                                { "user_id", user.id }
                            }
                        }
                    }
                }
            };

            session = GameClientSystem.SessionAPI!.CallMethod<INoxObject>("CreateSession", "online", controller);
            session.SetField("WorldId", world.id);
            session.SetField("WorldAssetId", asset.id);
            session.SetField("WorldAddress", instance.server);

            if (await session.CallMethod<INoxObject>("GetController").CallMethod<UniTask<bool>>("Prepare", false))
            {
                Logger.Log("Session set current");
                await GameClientSystem.SessionAPI!.CallMethod<UniTask>("SetCurrentSessionUid",
                    session.GetField<ushort>("Uid"));
            }
            else
            {
                Logger.Log("Session disposed");
                await session.CallMethod<UniTask>("Dispose");
                await GameClientSystem.SessionAPI!.CallMethod<UniTask>("Remove", session.GetField<ushort>("Uid"));
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
            Reference.GetReference("players.title", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { players.Length.ToString() });

            Dictionary<string, List<string>> requests = new();
            foreach (var player in players)
            {
                var identifier = UserIdentifier.FromString(player.user);
                var server = identifier.IsLocal() ? tile.Instance.server : identifier.Server;
                if (!requests.ContainsKey(server))
                    requests[server] = new List<string>();
                requests[server].Add(player.user);
            }

            foreach (var server in requests.Keys)
                tasks.Add(FetchWorkPlayers(tile, content, server, requests[server].ToArray()));
            await UniTask.WhenAll(tasks);

            refresh_players.interactable = true;
        }


        private async UniTask FetchWorkPlayers(InstanceTileObject tile, GameObject content, string address,
            string[] players)
        {
            var res = await GameClientSystem.NetworkAPI
                .GetField<INoxObject>("User")
                .CallMethod<UniTask<INoxObject>>("SearchUsers", new Dictionary<string, object>
                {
                    { "user_ids", players },
                    { "server", address },
                    { "limit", 100 }
                });


            List<User> users = new();
            while (res != null && res.CallMethod<bool>("HasNext"))
            {
                users.AddRange(res.CallMethod<User[]>("GetUsers"));
                res = await res.CallMethod<UniTask<INoxObject>>("Next");
            }

            if (res != null)
                users.AddRange(res.CallMethod<User[]>("GetUsers"));

            var container = Reference.GetReference("players", content);
            var userPrefab = GameClientSystem.CoreAPI
                .AssetAPI.GetAsset<GameObject>("prefabs/game.instance.player.prefab");
            userPrefab.SetActive(false);

            foreach (var user in users)
            {
                var userTile = Object.Instantiate(userPrefab, container.transform);
                Reference.GetReference("title", userTile).GetComponent<TextLanguage>()
                    .UpdateText(new string[] { user.display });
                userTile.SetActive(true);
                var thumbnail = Reference.GetReference("thumbnail", userTile).GetComponent<RawImage>();
                if (!string.IsNullOrEmpty(user.thumbnail))
                    UpdateTexture(thumbnail, user.thumbnail).Forget();
                var button = Reference.GetReference("button", userTile).GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => MenuManager.Instance.SendGotoTile(tile.MenuId, "game.user", user));
            }

            ForceUpdateLayout.UpdateManually(container);
        }
    }
}