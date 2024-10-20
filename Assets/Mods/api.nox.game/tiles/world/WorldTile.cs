
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.UI;
using UnityEngine.Events;
using System;
using Object = UnityEngine.Object;
using api.nox.network.Users;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using api.nox.network;
using api.nox.network.Instances;

namespace api.nox.game.Tiles
{
    internal class WorldTileManager : TileManager
    {
        private EventSubscription WorldFetchSub;
        private EventSubscription WorldAssetFetchSub;
        private EventSubscription UserUpdateSub;
        [Serializable] public class UserUpdatedEvent : UnityEvent<UserMe> { }
        [Serializable] public class WorldFetchedEvent : UnityEvent<World> { }
        [Serializable] public class WorldAssetFetchedEvent : UnityEvent<WorldAsset> { }
        public UserUpdatedEvent OnUserUpdated;
        public WorldFetchedEvent OnWorldFetched;
        public WorldAssetFetchedEvent OnWorldAssetFetched;

        internal WorldTileManager()
        {
            WorldFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("world_fetch", OnFetchWorld);
            WorldAssetFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("world_asset_fetch", OnFetchWorldAsset);
            UserUpdateSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_update", OnUserUpdate);
            OnUserUpdated = new UserUpdatedEvent();
            OnWorldFetched = new WorldFetchedEvent();
            OnWorldAssetFetched = new WorldAssetFetchedEvent();
        }

        internal void OnDispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(WorldFetchSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(WorldAssetFetchSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(UserUpdateSub);
            OnUserUpdated?.RemoveAllListeners();
            OnWorldFetched?.RemoveAllListeners();
            OnWorldAssetFetched?.RemoveAllListeners();
            OnUserUpdated = null;
            OnWorldFetched = null;
            OnWorldAssetFetched = null;
        }

        internal class WorldTileObject : TileObject
        {
            public bool IsFetchingInstances = false;
            public UnityAction<World> OnWorldFetched;
            public UnityAction<WorldAsset> OnWorldAssetFetched;
            public UnityAction<UserMe> OnUserUpdated;

            public World World
            {
                get => GetData<World>(0);
                set => SetData(0, value);
            }
            public WorldAsset Asset
            {
                get => GetData<WorldAsset>(1);
                set => SetData(1, value);
            }
        }

        private void OnFetchWorld(EventData context)
        {
            var world = context.Data[0] as World;
            if (world == null) return;
            OnWorldFetched?.Invoke(world);
        }

        private void OnFetchWorldAsset(EventData context)
        {
            var asset = context.Data[0] as WorldAsset;
            if (asset == null) return;
            OnWorldAssetFetched?.Invoke(asset);
        }

        private void OnUserUpdate(EventData context)
        {
            var user = context.Data[0] as UserMe;
            if (user == null) return;
            OnUserUpdated?.Invoke(user);
        }

        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            Debug.Log("WorldTileManager.SendTile");
            var tile = new WorldTileObject() { id = "api.nox.game.world", context = context };
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = (str) => OnOpen(tile, tile.content);
            tile.onHide = (str) => OnHide(tile, tile.content);
            tile.onRemove = () => OnRemove(tile);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tf"></param>
        /// <returns>Content of the tile</returns>
        internal GameObject OnGetContent(WorldTileObject tile, Transform tf)
        {
            Debug.Log("WorldTileManager.GetTileContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.world");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.world";

            if (tile.OnUserUpdated != null)
                OnUserUpdated.RemoveListener(tile.OnUserUpdated);
            tile.OnUserUpdated = (user) => OnUserTileUpdate(tile, content, user);
            OnUserUpdated.AddListener(tile.OnUserUpdated);

            if (tile.OnWorldFetched != null)
                OnWorldFetched.RemoveListener(tile.OnWorldFetched);
            tile.OnWorldFetched = (world) => OnWorldTileUpdate(tile, content, world);
            OnWorldFetched.AddListener(tile.OnWorldFetched);

            if (tile.OnWorldAssetFetched != null)
                OnWorldAssetFetched.RemoveListener(tile.OnWorldAssetFetched);
            tile.OnWorldAssetFetched = (asset) => OnWorldAssetTileUpdate(tile, content, asset);
            OnWorldAssetFetched.AddListener(tile.OnWorldAssetFetched);

            return content;
        }

        internal void OnRemove(WorldTileObject tile)
        {
            Debug.Log("WorldTileManager.OnRemove");
            if (tile.OnUserUpdated != null)
                OnUserUpdated.RemoveListener(tile.OnUserUpdated);
            if (tile.OnWorldFetched != null)
                OnWorldFetched.RemoveListener(tile.OnWorldFetched);
            if (tile.OnWorldAssetFetched != null)
                OnWorldAssetFetched.RemoveListener(tile.OnWorldAssetFetched);
            tile.OnUserUpdated = null;
            tile.OnWorldFetched = null;
            tile.OnWorldAssetFetched = null;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnDisplay(WorldTileObject tile, GameObject content)
        {
            Debug.Log("WorldTileManager.OnDisplay");
            UpdateContent(tile, content);
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnOpen(WorldTileObject tile, GameObject content)
        {
            Debug.Log("WorldTileManager.OnOpen");
            OnClickRefreshInstances(tile, content).Forget();
        }

        /// <summary>
        /// Handle the hiding of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnHide(WorldTileObject tile, GameObject content)
        {
            Debug.Log("WorldTileManager.OnHide");
        }

        internal void OnWorldTileUpdate(WorldTileObject tile, GameObject content, World world)
        {
            var cWorld = tile.World;
            if (cWorld == null) return;
            if (cWorld.id != world.id) return;
            if (cWorld.server != world.server) return;

            var refresh_world = Reference.GetReference("refresh_world", content).GetComponent<Button>();
            if (!refresh_world.interactable) return;

            tile.World = world;

            UpdateContent(tile, content);
        }

        internal void OnWorldAssetTileUpdate(WorldTileObject tile, GameObject content, WorldAsset asset)
        {
            var cAsset = tile.Asset;
            if (cAsset != null)
            {
                if (cAsset.world_id != asset.world_id) return;
                if (cAsset.server != asset.server) return;
                if (cAsset.version != asset.version) return;
            }

            var refresh_world = Reference.GetReference("refresh_world", content).GetComponent<Button>();
            if (!refresh_world.interactable) return;

            tile.Asset = asset;

            UpdateContent(tile, content);
        }

        internal void OnUserTileUpdate(WorldTileObject tile, GameObject content, UserMe user)
        {
            var cUser = GameClientSystem.Instance.NetworkAPI.User.CurrentUser;
            if (cUser == null) return;
            if (cUser.id != user.id) return;
            if (cUser.server != user.server) return;

            var refresh_world = Reference.GetReference("refresh_world", content).GetComponent<Button>();
            if (!refresh_world.interactable) return;

            var dlb = Reference.GetReference("home.button", content).GetComponent<Button>();
            if (!dlb.interactable) return;

            CheckHome(tile, content, user);
        }

        internal void UpdateContent(WorldTileObject tile, GameObject content)
        {
            Debug.Log("WorldTileManager.UpdateContent");
            var world = tile.World;
            var asset = tile.Asset;
            if (world == null)
            {
                Debug.LogError("World is null");
                return;
            }

            Reference.GetReference("display", content).GetComponent<TextLanguage>().UpdateText(new string[] { world.title });
            Reference.GetReference("title", content).GetComponent<TextLanguage>().UpdateText(new string[] { world.title });
            Reference.GetReference("description", content).GetComponent<TextLanguage>().UpdateText(new string[] { world.description });
            var icon = Reference.GetReference("icon", content).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(world.thumbnail))
                UpdateTexure(icon, world.thumbnail).Forget();

            CheckHome(tile, content, GameClientSystem.Instance.NetworkAPI.User.CurrentUser);
            CheckVersion(tile, content);

            var refresh_world = Reference.GetReference("refresh_world", content).GetComponent<Button>();
            refresh_world.onClick.RemoveAllListeners();
            refresh_world.onClick.AddListener(() => OnClickRefreshWorld(tile, refresh_world, content).Forget());

            var refresh_instances = Reference.GetReference("refresh_instances", content).GetComponent<Button>();
            refresh_instances.onClick.RemoveAllListeners();
            refresh_instances.onClick.AddListener(() => OnClickRefreshInstances(tile, content).Forget());
        }

        private async UniTask OnClickRefreshWorld(WorldTileObject tile, Button dlb, GameObject content)
        {
            if (!dlb.interactable) return;
            dlb.interactable = false;
            var world = tile.World;
            var asset = tile.Asset;
            if (world == null)
            {
                Debug.LogError("World is null");
                dlb.interactable = true;
                return;
            }

            world = await GameClientSystem.Instance.NetworkAPI.World.GetWorld(world.server, world.id);

            if (world == null)
            {
                Debug.LogError("World not found");
                dlb.interactable = true;
                return;
            }

            Debug.Log($"World fetched: {world}");
            var search = await GameClientSystem.Instance.NetworkAPI.World.Asset.SearchAssets(new()
            {
                server = world.server,
                world_id = world.id,
                limit = 1,
                offset = 0,
                platforms = new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) },
                engines = new string[] { "unity" },
                versions = asset == null || asset.version == ushort.MaxValue
                    ? null : new ushort[] { asset.version }
            });

            tile.World = world;
            tile.Asset = search?.assets[0];
            dlb.interactable = true;

            UpdateContent(tile, content);
        }

        internal void CheckHome(WorldTileObject tile, GameObject content, UserMe user)
        {
            Debug.Log("WorldTileManager.CheckHome");
            var dlb = Reference.GetReference("home.button", content).GetComponent<Button>();
            dlb.onClick.RemoveAllListeners();
            var wp = string.IsNullOrEmpty(user?.home) ? null : WorldIdentifier.FromString(user.home);
            var hasHome = wp?.id == tile.World.id && wp.server == tile.World.server;
            SetHomeButton(hasHome, content);
            dlb.onClick.AddListener(() => OnClickHome(tile, content, user, dlb, hasHome).Forget());
        }

        internal void CheckVersion(WorldTileObject tile, GameObject content)
        {
            var asset = tile.Asset;

            var dlb = Reference.GetReference("download.button", content).GetComponent<Button>();
            var gotob = Reference.GetReference("goto.button", content).GetComponent<Button>();
            var instb = Reference.GetReference("instance.button", content).GetComponent<Button>();
            dlb.interactable = false;
            gotob.interactable = false;
            instb.interactable = false;
            dlb.onClick.RemoveAllListeners();
            gotob.onClick.RemoveAllListeners();
            instb.onClick.RemoveAllListeners();

            if (asset != null)
            {
                if (!WorldManager.IsWorldLoaded(asset.hash))
                {
                    dlb.interactable = true;
                    var type = WorldManager.HasWorldInCache(asset.hash) ? DownloadButtonType.Downloaded : DownloadButtonType.Download;
                    dlb.onClick.AddListener(() => OnClickDownload(tile, content, type).Forget());
                    SetDownloadButton(content, type);
                    gotob.interactable = true;
                    // gotob.onClick.AddListener(() => OnClickGoto(tile, content).Forget());
                }

                instb.interactable = true;
                instb.onClick.AddListener(() => OnClickMakeInstance(tile, content).Forget());
            }
            else SetDownloadButton(content, DownloadButtonType.Unavailable);
        }

        private async UniTask OnClickMakeInstance(WorldTileObject tile, GameObject content)
        {
            var instb = Reference.GetReference("instance.button", content).GetComponent<Button>();
            if (!instb.interactable) return;
            instb.interactable = false;

            var server = GameClientSystem.Instance.NetworkAPI.Server.CurrentServer;
            server ??= await GameClientSystem.Instance.NetworkAPI.Server.GetMyServer();
            server = server != null && server.features.Contains("instance") ? server : null;
            if (server == null)
            {
                instb.interactable = true;
                return;
            }

            MenuManager.Instance.SendGotoTile(tile.MenuId, "game.instance.make", server, tile.World);

            instb.interactable = true;
        }

        private async UniTask OnClickHome(WorldTileObject tile, GameObject content, UserMe user, Button dlb, bool hasHome)
        {
            Debug.Log("WorldTileManager.OnClickHome");
            if (!dlb.interactable) return;
            if (hasHome)
            {
                dlb.interactable = false;
                user = await GameClientSystem.Instance.NetworkAPI.User.UpdateMyUser(new() { home = "NULL" });
                dlb.interactable = true;
                CheckHome(tile, content, user);
            }
            else
            {
                dlb.interactable = false;
                Debug.Log($"Setting home to {tile.World.ToIdentifier().ToFullString(tile.World.server)}");
                user = await GameClientSystem.Instance.NetworkAPI.User.UpdateMyUser(new()
                {
                    home = tile.World.ToIdentifier().ToFullString(tile.World.server)
                });
                dlb.interactable = true;
                CheckHome(tile, content, user);
            }
        }


        private async UniTask OnClickRefreshInstances(WorldTileObject tile, GameObject content)
        {
            Debug.Log("WorldTileManager.OnClickRefreshInstances");
            var refresh_instances = Reference.GetReference("refresh_instances", content).GetComponent<Button>();
            if (!refresh_instances.interactable) return;
            refresh_instances.interactable = false;

            var config = Config.Load();
            var servers_t = config.Get("servers");
            if (servers_t == null) return;
            var server_d = servers_t.ToObject<Dictionary<string, NavigationWorkerInfo>>();
            var servers = server_d.Values.ToArray();
            var workers = servers.Where(x => (x.navigation || x.address == config.Get("server", "")) && x.features.Contains("instance")).ToArray();

            List<UniTask> tasks = new();

            var container = Reference.GetReference("instances", content).GetComponent<RectTransform>();
            for (int i = 0; i < container.childCount; i++)
                Object.Destroy(container.GetChild(i).gameObject);

            for (var i = 0; i < workers.Length; i++)
            {
                var worker = workers[i];
                if (worker == null) continue;
                var work = FetchWorkInstances(tile, content, worker.address);
                tasks.Add(work);
            }
            await UniTask.WhenAll(tasks);

            refresh_instances.interactable = true;
        }

        private async UniTask FetchWorkInstances(WorldTileObject tile, GameObject content, string address)
        {
            var res = await GameClientSystem.Instance.NetworkAPI.Instance.SearchInstances(new()
            {
                world = tile.World.ToIdentifier().ToFullString(tile.World.server),
                server = address,
                limit = 100
            });
            List<Instance> instances = new();
            while (res != null && res.HasNext())
            {
                instances.AddRange(res.instances);
                res = await res.Next();
            }
            if (res != null)
                instances.AddRange(res.instances);
            var container = Reference.GetReference("instances", content);
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/instance.entry");
            foreach (var instance in instances)
            {
                var entry = Object.Instantiate(pf, container.transform);
                Reference.GetReference("instance_title", entry).GetComponent<TextLanguage>().UpdateText(new string[] { instance.name, instance.title });
                Reference.GetReference("instance_description", entry).GetComponent<TextLanguage>().UpdateText(new string[] { instance.description });
                Reference.GetReference("instance_button", entry).GetComponent<Button>()
                    .onClick.AddListener(() => MenuManager.Instance.SendGotoTile(tile.MenuId, "game.instance", instance, tile.World, tile.Asset));
            }
            ForceUpdateLayout.UpdateManually(container);
        }

        // private async UniTask OnClickGoto(GameObject tile, World world, WorldAsset asset)
        // {
        //     var dlb = Reference.GetReference("download.button", tile).GetComponent<Button>();
        //     var gotob = Reference.GetReference("goto.button", tile).GetComponent<Button>();
        //     if (!gotob.interactable) return;
        //     gotob.interactable = false;
        //     if (!WorldManager.HasWorldInCache(asset.hash))
        //         await OnClickDownload(tile, content, DownloadButtonType.Download);

        //     var session = GameSystem.Instance.SessionManager.GetSession(world.server, world.id);
        //     if (session != null)
        //     {
        //         session.SetCurrent();
        //         return;
        //     }

        //     var controller = new OfflineController();
        //     session = GameSystem.Instance.SessionManager.New(controller, world.server, world.id);
        //     session.world = world;
        //     session.worldAsset = asset;
        //     if (await controller.Prepare())
        //         session.SetCurrent();
        //     else session.Dispose();

        //     // UpdateContent(tile, world);
        // }


        private async UniTask OnClickDownload(WorldTileObject tile, GameObject content, DownloadButtonType type)
        {
            var asset = tile.Asset;
            var dlb = Reference.GetReference("download.button", content).GetComponent<Button>();
            if (!dlb.interactable) return;
            if (type == DownloadButtonType.Download)
            {
                dlb.interactable = false;
                SetDownloadButton(content, DownloadButtonType.Downloading, 0);
                var res = await WorldManager.DownloadWorld(asset.hash, asset.url, (progress, size) => SetDownloadButton(content, DownloadButtonType.Downloading, progress));
                if (res.success) SetDownloadButton(content, DownloadButtonType.Downloading, 1);
                dlb.interactable = true;
                CheckVersion(tile, content);
            }
            else if (type == DownloadButtonType.Downloaded)
            {
                dlb.interactable = false;
                WorldManager.DeleteWorldFromCache(asset.hash);
                SetDownloadButton(content, DownloadButtonType.Download);
                dlb.interactable = true;
                CheckVersion(tile, content);
            }
        }


        internal void SetDownloadButton(GameObject content, DownloadButtonType type, float progress = 0)
        {
            var downloadbutton = Reference.GetReference("download.button", content);
            var start = Reference.GetReference("start", downloadbutton);
            var downloaded = Reference.GetReference("downloaded", downloadbutton);
            var downloading = Reference.GetReference("downloading", downloadbutton);
            var unavailable = Reference.GetReference("unavailable", downloadbutton);
            start.SetActive(type == DownloadButtonType.Download);
            downloaded.SetActive(type == DownloadButtonType.Downloaded);
            downloading.SetActive(type == DownloadButtonType.Downloading);
            unavailable.SetActive(type == DownloadButtonType.Unavailable);
            if (type == DownloadButtonType.Downloading)
            {
                var progressbar = Reference.GetReference("progress", downloading).GetComponent<RectTransform>();
                var width = progressbar.transform.parent.GetComponent<RectTransform>().rect.width;
                progressbar.sizeDelta = new Vector2(width * progress, 0);
                var percent = Reference.GetReference("percent", downloading).GetComponent<TextLanguage>();
                percent.arguments = new string[] { ((float)progress * 100).ToString("0") };
                percent.UpdateText();
            }
        }

        internal void SetHomeButton(bool hasHome, GameObject content)
        {
            var homebutton = Reference.GetReference("home.button", content);
            var set = Reference.GetReference("no", homebutton);
            var reset = Reference.GetReference("yes", homebutton);
            set.SetActive(!hasHome);
            reset.SetActive(hasHome);
        }

        internal enum DownloadButtonType
        {
            Download = 0,
            Downloading = 1,
            Downloaded = 2,
            Unavailable = 3
        }

    }
}