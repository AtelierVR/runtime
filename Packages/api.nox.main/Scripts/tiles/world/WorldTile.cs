/*using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.UI;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;
using api.nox.network;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using Transform = UnityEngine.Transform;

namespace api.nox.game.Tiles
{
    internal class WorldTileManager : TileManager
    {
        private readonly EventSubscription _worldFetchSub;
        private readonly EventSubscription _worldAssetFetchSub;
        private readonly EventSubscription _userUpdateSub;
        private UnityEvent<INoxObject> _onUserUpdated;
        private UnityEvent<INoxObject> _onWorldFetched;
        private UnityEvent<INoxObject> _onWorldAssetFetched;

        internal WorldTileManager()
        {
            _worldFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("world_fetch", OnFetchWorld);
            _worldAssetFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("world_asset_fetch", OnFetchWorldAsset);
            _userUpdateSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_update", OnUserUpdate);
            _onUserUpdated = new UnityEvent<INoxObject>();
            _onWorldFetched = new UnityEvent<INoxObject>();
            _onWorldAssetFetched = new UnityEvent<INoxObject>();
        }

        internal void OnDispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_worldFetchSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_worldAssetFetchSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_userUpdateSub);
            _onUserUpdated?.RemoveAllListeners();
            _onWorldFetched?.RemoveAllListeners();
            _onWorldAssetFetched?.RemoveAllListeners();
            _onUserUpdated = null;
            _onWorldFetched = null;
            _onWorldAssetFetched = null;
        }

        private class WorldTileObject : TileObject
        {
            public UnityAction<INoxObject> OnWorldFetched;
            public UnityAction<INoxObject> OnWorldAssetFetched;
            public UnityAction<INoxObject> OnUserUpdated;

            public INoxObject World
            {
                get => GetData<INoxObject>(0);
                set => SetData(0, value);
            }

            public INoxObject Asset
            {
                get => GetData<INoxObject>(1);
                set => SetData(1, value);
            }
        }

        private void OnFetchWorld(EventData context)
        {
            if (context.Data[0] is not INoxObject world) return;
            _onWorldFetched?.Invoke(world);
        }

        private void OnFetchWorldAsset(EventData context)
        {
            if (context.Data[0] is not INoxObject asset) return;
            _onWorldAssetFetched?.Invoke(asset);
        }

        private void OnUserUpdate(EventData context)
        {
            if (context.Data[0] is not INoxObject user) return;
            _onUserUpdated?.Invoke(user);
        }

        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            Logger.Log("WorldTileManager.SendTile");
            var tile = new WorldTileObject() { id = "api.nox.game.world", context = context };
            tile.GetContent = tf => OnGetContent(tile, tf);
            tile.onDisplay = (_, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = _ => OnOpen(tile, tile.content);
            tile.onRemove = () => OnRemove(tile);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tf"></param>
        /// <returns>Content of the tile</returns>
        private GameObject OnGetContent(WorldTileObject tile, Transform tf)
        {
            Logger.Log("WorldTileManager.GetTileContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/game.world.prefab");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.world";

            if (tile.OnUserUpdated != null)
                _onUserUpdated.RemoveListener(tile.OnUserUpdated);
            tile.OnUserUpdated = (user) => OnUserTileUpdate(tile, content, user);
            _onUserUpdated.AddListener(tile.OnUserUpdated);

            if (tile.OnWorldFetched != null)
                _onWorldFetched.RemoveListener(tile.OnWorldFetched);
            tile.OnWorldFetched = (world) => OnWorldTileUpdate(tile, content, world);
            _onWorldFetched.AddListener(tile.OnWorldFetched);

            if (tile.OnWorldAssetFetched != null)
                _onWorldAssetFetched.RemoveListener(tile.OnWorldAssetFetched);
            tile.OnWorldAssetFetched = (asset) => OnWorldAssetTileUpdate(tile, content, asset);
            _onWorldAssetFetched.AddListener(tile.OnWorldAssetFetched);

            return content;
        }

        private void OnRemove(WorldTileObject tile)
        {
            Logger.Log("WorldTileManager.OnRemove");
            if (tile.OnUserUpdated != null)
                _onUserUpdated.RemoveListener(tile.OnUserUpdated);
            if (tile.OnWorldFetched != null)
                _onWorldFetched.RemoveListener(tile.OnWorldFetched);
            if (tile.OnWorldAssetFetched != null)
                _onWorldAssetFetched.RemoveListener(tile.OnWorldAssetFetched);
            tile.OnUserUpdated = null;
            tile.OnWorldFetched = null;
            tile.OnWorldAssetFetched = null;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        private void OnDisplay(WorldTileObject tile, GameObject content)
        {
            Logger.Log("WorldTileManager.OnDisplay");
            UpdateContent(tile, content);
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        private void OnOpen(WorldTileObject tile, GameObject content)
        {
            Logger.Log("WorldTileManager.OnOpen");
            OnClickRefreshInstances(tile, content).Forget();
        }

        private void OnWorldTileUpdate(WorldTileObject tile, GameObject content, INoxObject world)
        {
            var cWorld = tile.World;
            if (cWorld == null) return;
            if (cWorld.GetField<uint>("id") != world.GetField<uint>("id")) return;
            if (cWorld.GetField<string>("server") != world.GetField<string>("server")) return;

            var refreshWorld = Reference.GetReference("refresh_world", content).GetComponent<Button>();
            if (!refreshWorld.interactable) return;

            tile.World = world;

            UpdateContent(tile, content);
        }

        private void OnWorldAssetTileUpdate(WorldTileObject tile, GameObject content, INoxObject asset)
        {
            var cAsset = tile.Asset;
            if (cAsset != null)
            {
                if (cAsset.GetField<string>("world_id") != asset.GetField<string>("world_id")) return;
                if (cAsset.GetField<string>("server") != asset.GetField<string>("server")) return;
                if (cAsset.GetField<string>("version") != asset.GetField<string>("version")) return;
            }

            var refreshWorld = Reference.GetReference("refresh_world", content).GetComponent<Button>();
            if (!refreshWorld.interactable) return;

            tile.Asset = asset;

            UpdateContent(tile, content);
        }

        private void OnUserTileUpdate(WorldTileObject tile, GameObject content, INoxObject user)
        {
            var cUser = GameClientSystem.NetworkAPI.GetField("User").CallMethod("GetCurrentUser");
            if (cUser == null) return;
            if (cUser.GetField<string>("id") != user.GetField<string>("id")) return;
            if (cUser.GetField<string>("server") != user.GetField<string>("server")) return;

            var refreshWorld = Reference.GetReference("refresh_world", content).GetComponent<Button>();
            if (!refreshWorld.interactable) return;

            var dlb = Reference.GetReference("home.button", content).GetComponent<Button>();
            if (!dlb.interactable) return;

            CheckHome(tile, content, user);
        }

        private void UpdateContent(WorldTileObject tile, GameObject content)
        {
            Logger.Log("WorldTileManager.UpdateContent");
            var world = tile.World;
            if (world == null)
            {
                Logger.LogError("World is null");
                return;
            }

            var title = world.GetField<string>("title");
            var thumbnail = world.GetField<string>("thumbnail");
            Reference.GetReference("display", content).GetComponent<TextLanguage>()
                .UpdateText(new[] { title });
            Reference.GetReference("title", content).GetComponent<TextLanguage>()
                .UpdateText(new[] { title });
            Reference.GetReference("description", content).GetComponent<TextLanguage>()
                .UpdateText(new[] { world.GetField<string>("description") });
            var icon = Reference.GetReference("icon", content).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(thumbnail))
                UpdateTexture(icon, thumbnail).Forget();

            CheckHome(tile, content, GameClientSystem.NetworkAPI.GetField("User").CallMethod("GetCurrentUser"));
            CheckVersion(tile, content);

            var refreshWorld = Reference.GetReference("refresh_world", content).GetComponent<Button>();
            refreshWorld.onClick.RemoveAllListeners();
            refreshWorld.onClick.AddListener(() => OnClickRefreshWorld(tile, refreshWorld, content).Forget());

            var refreshInstances = Reference.GetReference("refresh_instances", content).GetComponent<Button>();
            refreshInstances.onClick.RemoveAllListeners();
            refreshInstances.onClick.AddListener(() => OnClickRefreshInstances(tile, content).Forget());
        }

        private async UniTask OnClickRefreshWorld(WorldTileObject tile, Button dlb, GameObject content)
        {
            if (!dlb.interactable) return;
            dlb.interactable = false;
            var world = tile.World;
            var asset = tile.Asset;
            if (world == null)
            {
                Logger.LogError("World is null");
                dlb.interactable = true;
                return;
            }

            var worldApi = GameClientSystem.NetworkAPI.GetField("World");

            world = await worldApi.CallAsyncMethod("GetWorld", world.GetField<string>("server"),
                world.GetField<uint>("id"));

            if (world == null)
            {
                Logger.LogError("World not found");
                dlb.interactable = true;
                return;
            }

            Logger.Log($"World fetched: {world}");
            var search = await worldApi.GetField("Asset")
                .CallAsyncMethod("SearchAssets", new Dictionary<string, object>
                {
                    { "server", world.GetField<string>("server") },
                    { "world_id", world.GetField<uint>("id") },
                    { "limit", 1 },
                    { "offset", 0 },
                    { "platforms", new[] { Constants.CurrentPlatform.GetPlatformName() } },
                    { "engines", new[] { Constants.CurrentEngine.GetEngineName() } },
                    {
                        "versions", asset == null || asset.GetField<ushort>("version") == ushort.MaxValue
                            ? null
                            : new[] { asset.GetField<ushort>("version") }
                    }
                });

            tile.World = world;
            tile.Asset = search?.GetField<INoxObject[]>("assets")?.FirstOrDefault();
            dlb.interactable = true;

            UpdateContent(tile, content);
        }

        private void CheckHome(WorldTileObject tile, GameObject content, INoxObject user)
        {
            Logger.Log("WorldTileManager.CheckHome");
            var dlb = Reference.GetReference("home.button", content).GetComponent<Button>();
            dlb.onClick.RemoveAllListeners();
            var home = user.GetField<string>("home");
            var wp = string.IsNullOrEmpty(home) ? null : WorldIdentifier.FromString(home);
            var hasHome = wp?.ID == tile.World.GetField<uint>("id")
                          && wp.Server == tile.World.GetField<string>("server");
            SetHomeButton(hasHome, content);
            dlb.onClick.AddListener(() => OnClickHome(tile, content, user, dlb, hasHome).Forget());
        }

        private void CheckVersion(WorldTileObject tile, GameObject content)
        {
            var asset = tile.Asset;

            var dlb = Reference.GetReference("download.button", content).GetComponent<Button>();
            var gotoButton = Reference.GetReference("goto.button", content).GetComponent<Button>();
            var instanceButton = Reference.GetReference("instance.button", content).GetComponent<Button>();
            dlb.interactable = false;
            gotoButton.interactable = false;
            instanceButton.interactable = false;
            dlb.onClick.RemoveAllListeners();
            gotoButton.onClick.RemoveAllListeners();
            instanceButton.onClick.RemoveAllListeners();

            if (asset != null)
            {
                var hash = asset.GetField<string>("hash");
                if (!worlds.WorldManager.IsAssetLoaded(hash))
                {
                    dlb.interactable = true;
                    var type = worlds.WorldCache.HasWorldInCache(hash)
                        ? DownloadButtonType.Downloaded
                        : DownloadButtonType.Download;
                    dlb.onClick.AddListener(() => OnClickDownload(tile, content, type).Forget());
                    SetDownloadButton(content, type);
                    gotoButton.interactable = true;
                }

                instanceButton.interactable = true;
                instanceButton.onClick.AddListener(() => OnClickMakeInstance(tile, content).Forget());
            }
            else SetDownloadButton(content, DownloadButtonType.Unavailable);
        }

        private async UniTask OnClickMakeInstance(WorldTileObject tile, GameObject content)
        {
            var instb = Reference.GetReference("instance.button", content).GetComponent<Button>();
            if (!instb.interactable) return;
            instb.interactable = false;

            var serverApi = GameClientSystem.NetworkAPI.GetField("Server");
            var server = serverApi.CallMethod("GetCurrentServer");
            server ??= await serverApi.CallAsyncMethod("GetMyServer");
            server = server != null && server.GetField<string[]>("features").Contains("instance") ? server : null;
            if (server == null)
            {
                instb.interactable = true;
                return;
            }

            MenuManager.Instance.SendGotoTile(tile.MenuId, "game.instance.make", server, tile.World);
            instb.interactable = true;
        }

        private async UniTask OnClickHome(
            WorldTileObject tile,
            GameObject content,
            INoxObject user,
            Button dlb, bool hasHome
        )
        {
            Logger.Log("WorldTileManager.OnClickHome");
            if (!dlb.interactable) return;
            var userApi = GameClientSystem.NetworkAPI.GetField("User");
            dlb.interactable = false;
            if (hasHome)
            {
                user = await userApi.CallAsyncMethod("UpdateMyUser",
                    new Dictionary<string, object> { { "home", "NULL" } });
                dlb.interactable = true;
                CheckHome(tile, content, user);
            }
            else
            {
                var identifier = tile.World.CallMethod("ToIdentifier")
                    .CallMethod<string>("ToFullString", tile.World.GetField<string>("server"));
                Logger.Log($"Setting home to {identifier}");
                user = await userApi.CallAsyncMethod("UpdateMyUser",
                    new Dictionary<string, object> { { "home", identifier } });
            }

            dlb.interactable = true;
            CheckHome(tile, content, user);
        }

        private async UniTask OnClickRefreshInstances(WorldTileObject tile, GameObject content)
        {
            Logger.Log("WorldTileManager.OnClickRefreshInstances");
            var refreshInstances = Reference.GetReference("refresh_instances", content).GetComponent<Button>();
            if (!refreshInstances.interactable) return;
            refreshInstances.interactable = false;

            var config = Config.Load();
            var serversT = config.Get("servers");
            if (serversT == null) return;
            var serverD = serversT.ToObject<Dictionary<string, NavigationWorkerInfo>>();
            var servers = serverD.Values.ToArray();
            var workers = servers.Where(x =>
                (x.navigation || x.address == config.Get("server", "")) && x.features.Contains("instance")).ToArray();

            List<UniTask> tasks = new();

            var container = Reference.GetReference("instances", content).GetComponent<RectTransform>();
            for (var i = 0; i < container.childCount; i++)
                Object.Destroy(container.GetChild(i).gameObject);

            foreach (var worker in workers)
            {
                if (worker == null) continue;
                var work = FetchWorkInstances(tile, content, worker.address);
                tasks.Add(work);
            }

            await UniTask.WhenAll(tasks);

            refreshInstances.interactable = true;
        }

        private async UniTask FetchWorkInstances(WorldTileObject tile, GameObject content, string address)
        {
            var res = await GameClientSystem.NetworkAPI.GetField("Instance").CallAsyncMethod("SearchInstances",
                new Dictionary<string, object>
                {
                    {
                        "world",
                        tile.World.CallMethod("ToIdentifier")
                            .CallMethod<string>("ToFullString", tile.World.GetField<string>("server"))
                    },
                    { "server", address },
                    { "limit", 100 }
                });
            List<INoxObject> instances = new();
            while (res != null && res.CallMethod<bool>("HasNext"))
            {
                instances.AddRange(res.GetField<INoxObject[]>("instances"));
                res = await res.CallAsyncMethod("Next");
            }

            if (res != null)
                instances.AddRange(res.GetField<INoxObject[]>("instances"));
            var container = Reference.GetReference("instances", content);
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/instance.entry.prefab");
            foreach (var instance in instances)
            {
                var entry = Object.Instantiate(pf, container.transform);
                Reference.GetReference("instance_title", entry).GetComponent<TextLanguage>()
                    .UpdateText(new[] { instance.GetField<string>("name"), instance.GetField<string>("title") });
                Reference.GetReference("instance_description", entry).GetComponent<TextLanguage>()
                    .UpdateText(new[] { instance.GetField<string>("description") });
                Reference.GetReference("instance_button", entry).GetComponent<Button>()
                    .onClick.AddListener(() => MenuManager.Instance.SendGotoTile(
                        tile.MenuId, "game.instance",
                        instance, tile.World,
                        tile.Asset)
                    );
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
            switch (type)
            {
                case DownloadButtonType.Download:
                {
                    dlb.interactable = false;
                    SetDownloadButton(content, DownloadButtonType.Downloading);
                    var res = await worlds.WorldCache.DownloadWorld(
                        asset.GetField<string>("hash"), asset.GetField<string>("url"),
                        (progress, _) => SetDownloadButton(content, DownloadButtonType.Downloading, progress));
                    if (res.success) SetDownloadButton(content, DownloadButtonType.Downloading, 1);
                    dlb.interactable = true;
                    CheckVersion(tile, content);
                    break;
                }
                case DownloadButtonType.Downloaded:
                    dlb.interactable = false;
                    worlds.WorldCache.DeleteWorldFromCache(asset.GetField<string>("hash"));
                    SetDownloadButton(content, DownloadButtonType.Download);
                    dlb.interactable = true;
                    CheckVersion(tile, content);
                    break;
            }
        }


        private void SetDownloadButton(GameObject content, DownloadButtonType type, float progress = 0)
        {
            var downloader = Reference.GetReference("download.button", content);
            var start = Reference.GetReference("start", downloader);
            var downloaded = Reference.GetReference("downloaded", downloader);
            var downloading = Reference.GetReference("downloading", downloader);
            var unavailable = Reference.GetReference("unavailable", downloader);
            start.SetActive(type == DownloadButtonType.Download);
            downloaded.SetActive(type == DownloadButtonType.Downloaded);
            downloading.SetActive(type == DownloadButtonType.Downloading);
            unavailable.SetActive(type == DownloadButtonType.Unavailable);
            if (type != DownloadButtonType.Downloading) return;
            var progressbar = Reference.GetReference("progress", downloading).GetComponent<RectTransform>();
            var width = progressbar.transform.parent.GetComponent<RectTransform>().rect.width;
            progressbar.sizeDelta = new Vector2(width * progress, 0);
            var percent = Reference.GetReference("percent", downloading).GetComponent<TextLanguage>();
            percent.arguments = new[] { (progress * 100).ToString("0") };
            percent.UpdateText();
        }

        private void SetHomeButton(bool hasHome, GameObject content)
        {
            var home = Reference.GetReference("home.button", content);
            var set = Reference.GetReference("no", home);
            var reset = Reference.GetReference("yes", home);
            set.SetActive(!hasHome);
            reset.SetActive(hasHome);
        }

        private enum DownloadButtonType
        {
            Download = 0,
            Downloading = 1,
            Downloaded = 2,
            Unavailable = 3
        }
    }
}*/