
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using System.Collections.Generic;
using Nox.SimplyLibs;
using System.Linq;
using api.nox.game.sessions;
using api.nox.game.UI;

namespace api.nox.game.Tiles
{
    internal class WorldTileManager : TileManager
    {
        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            Debug.Log("WorldTileManager.SendTile");
            var tile = new TileObject() { id = "api.nox.game.world", context = context };
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
            Debug.Log("WorldTileManager.GetTileContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.world");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.world";
            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnDisplay(TileObject tile, GameObject content)
        {
            Debug.Log("WorldTileManager.OnDisplay");
            var world = (tile.context.Data[1] as ShareObject)?.Convert<SimplyWorld>();
            var asset = (tile.context.Data[2] as ShareObject)?.Convert<SimplyWorldAsset>();
            UpdateContent(content, world, asset);
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnOpen(TileObject tile, GameObject content)
        {
            Debug.Log("WorldTileManager.OnOpen");
        }

        /// <summary>
        /// Handle the hiding of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnHide(TileObject tile, GameObject content)
        {
            Debug.Log("WorldTileManager.OnHide");
        }


        private void UpdateContent(GameObject tile, SimplyWorld world, SimplyWorldAsset asset)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>().arguments = new string[] { world.title };
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { world.title };
            Reference.GetReference("description", tile).GetComponent<TextLanguage>().arguments = new string[] { world.description };
            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(world.thumbnail)) UpdateTexure(icon, world.thumbnail).Forget();
            // CheckVersion(tile, world).Forget();
            // CheckHome(tile, world);
        }




























        private async UniTask FetchInstancesWorker(GameObject tile, SimplyWorld world)
        {
            var config = Config.Load();
            var servers = config.Get("navigation.servers", new WorkerInfo[0]);
            var workers = servers.Where(x => x.features.Contains("instance")).ToArray();
            List<UniTask> tasks = new();

            var container = Reference.GetReference("instances", tile).GetComponent<RectTransform>();
            for (int i = 0; i < container.childCount; i++)
                Object.Destroy(container.GetChild(i).gameObject);

            for (var i = 0; i < workers.Length; i++)
            {
                var worker = workers[i];
                if (worker == null) continue;
                var work = FetchWorkInstances(tile, world, worker.address);
                tasks.Add(work);
            }
            await UniTask.WhenAll(tasks);
        }

        private bool goto_instance = false;
        private async UniTask FetchWorkInstances(GameObject tile, SimplyWorld world, string address)
        {
            var res = await GameClientSystem.Instance.NetworkAPI.Instance.SearchInstances(new()
            {
                world = world.ToMinimalString(),
                server = address,
                limit = 100
            });
            List<SimplyInstance> instances = new();
            while (res != null && res.HasNext())
            {
                instances.AddRange(res.instances);
                res = await res.Next();
            }
            if (res != null)
                instances.AddRange(res.instances);
            var container = Reference.GetReference("instances", tile);
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/instance.entry");
            foreach (var instance in instances)
            {
                var entry = Object.Instantiate(pf, container.transform);
                Reference.GetReference("title", entry).GetComponent<TextLanguage>().arguments = new string[] { instance.name, instance.title };
                Reference.GetReference("description", entry).GetComponent<TextLanguage>().arguments = new string[] { instance.description };
                Reference.GetReference("button", entry).GetComponent<Button>().onClick
                    .AddListener(() => OnClickInstance(instance).Forget());
            }
            goto_instance = false;
        }

        private async UniTask OnClickInstance(SimplyInstance instance)
        {
            if (goto_instance) return;
            goto_instance = true;

            var worldref = new WorldPatern(instance.world, instance.server);
            var world = await GameClientSystem.Instance.NetworkAPI.World.GetWorld(worldref.server, worldref.id);
            if (world == null)
            {
                Debug.LogError("World not found");
                goto_instance = false;
                return;
            }

            var search = await world.SearchAssets(0, 1,
                worldref.Version == ushort.MaxValue ? null : new uint[] { worldref.Version },
                new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) },
                new string[] { "unity" }
            );

            if (search == null || search.assets.Length == 0)
            {
                Debug.LogError("World not compatible with player");
                goto_instance = false;
                return;
            }
            var asset = search.assets[0];
            // clientMod.GotoTile("game.instance", instance, world, asset);
            goto_instance = false;
        }

        private async UniTask CheckVersion(GameObject tile, SimplyWorld world, uint[] versions = null)
        {
            var dlb = Reference.GetReference("download.button", tile).GetComponent<Button>();
            var gotob = Reference.GetReference("goto.button", tile).GetComponent<Button>();
            var instb = Reference.GetReference("instance.button", tile).GetComponent<Button>();
            dlb.interactable = false;
            gotob.interactable = false;
            instb.interactable = false;
            dlb.onClick.RemoveAllListeners();
            gotob.onClick.RemoveAllListeners();
            instb.onClick.RemoveAllListeners();
            var search = await world.SearchAssets(0, 1, versions, new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) }, new string[] { "unity" });
            if (search == null || search.assets.Length == 0)
            {
                Debug.LogError("World not compatible with player");
                return;
            }

            var asset = search.assets[0];
            if (!WorldManager.IsWorldLoaded(asset.hash))
            {
                dlb.interactable = true;
                var type = WorldManager.HasWorldInCache(asset.hash) ? DownloadButtonType.Downloaded : DownloadButtonType.Download;
                dlb.onClick.AddListener(() => OnClickDownload(dlb, tile, world, asset, type).Forget());
                SetDownloadButton(type);
                gotob.interactable = true;
                gotob.onClick.AddListener(() => OnClickGoto(tile, world, asset).Forget());
            }

            instb.interactable = true;
            instb.onClick.AddListener(() =>
            {
                var server = GameClientSystem.Instance.NetworkAPI.GetCurrentServer();
                server = server != null && server.features.Contains("instance") ? server : null;
                // clientMod.GotoTile("game.instance.make", world, versions == null || versions.Length == 0 ? null : asset, server);
            });
        }

        private async UniTask OnClickGoto(GameObject tile, SimplyWorld world, SimplyWorldAsset asset)
        {
            var dlb = Reference.GetReference("download.button", tile).GetComponent<Button>();
            var gotob = Reference.GetReference("goto.button", tile).GetComponent<Button>();
            if (!gotob.interactable) return;
            gotob.interactable = false;
            if (!WorldManager.HasWorldInCache(asset.hash))
                await OnClickDownload(dlb, tile, world, asset, DownloadButtonType.Download);

            var session = GameSystem.instance.sessionManager.GetSession(world.server, world.id);
            if (session != null)
            {
                session.SetCurrent();
                return;
            }

            var controller = new OfflineController();
            session = GameSystem.instance.sessionManager.New(controller, world.server, world.id);
            session.world = world;
            session.worldAsset = asset;
            if (await controller.Prepare())
                session.SetCurrent();
            else session.Dispose();

            // UpdateContent(tile, world);
        }

        private void CheckHome(GameObject tile, SimplyWorld world)
        {
            var dlb = Reference.GetReference("home.button", tile).GetComponent<Button>();
            dlb.interactable = false;
            dlb.onClick.RemoveAllListeners();
            var user = GameClientSystem.Instance.NetworkAPI.GetCurrentUser();
            WorldPatern wp = string.IsNullOrEmpty(user?.home) ? null : new WorldPatern(user.home, user.server);
            var hasHome = wp?.id == world.id && wp.server == world.server;
            SetHomeButton(hasHome);
            dlb.interactable = true;
            dlb.onClick.AddListener(() => OnClickHome(dlb, tile, world, hasHome).Forget());
        }

        private async UniTask OnClickDownload(Button dlb, GameObject tile, SimplyWorld world, SimplyWorldAsset asset, DownloadButtonType type)
        {
            if (!dlb.interactable) return;
            if (type == DownloadButtonType.Download)
            {
                dlb.interactable = false;
                SetDownloadButton(DownloadButtonType.Downloading, 0);
                var res = await WorldManager.DownloadWorld(asset.hash, asset.url, (progress, size) => SetDownloadButton(DownloadButtonType.Downloading, progress));
                if (res.success) SetDownloadButton(DownloadButtonType.Downloading, 1);
                dlb.interactable = true;
                await CheckVersion(tile, world, new uint[] { asset.version });
            }
            else if (type == DownloadButtonType.Downloaded)
            {
                dlb.interactable = false;
                WorldManager.DeleteWorldFromCache(asset.hash);
                SetDownloadButton(DownloadButtonType.Download);
                dlb.interactable = true;
                await CheckVersion(tile, world, new uint[] { asset.version });
            }
        }

        private async UniTask OnClickHome(Button dlb, GameObject tile, SimplyWorld world, bool hasHome)
        {
            if (!dlb.interactable) return;
            if (hasHome)
            {
                dlb.interactable = false;
                await GameClientSystem.Instance.NetworkAPI.User.UpdateUser(new SimplyUserUpdate { home = "NULL" });
                dlb.interactable = true;
                CheckHome(tile, world);
            }
            else
            {
                dlb.interactable = false;
                var wp = new WorldPatern() { id = world.id, server = world.server };
                await GameClientSystem.Instance.NetworkAPI.User.UpdateUser(new SimplyUserUpdate { home = wp.ToString() });
                dlb.interactable = true;
                CheckHome(tile, world);
            }
        }

        internal void SetDownloadButton(DownloadButtonType type, float progress = 0)
        {
            // var downloadbutton = Reference.GetReference("download.button", tile);
            // var start = Reference.GetReference("start", downloadbutton);
            // var downloaded = Reference.GetReference("downloaded", downloadbutton);
            // var downloading = Reference.GetReference("downloading", downloadbutton);
            // start.SetActive(type == DownloadButtonType.Download);
            // downloaded.SetActive(type == DownloadButtonType.Downloaded);
            // downloading.SetActive(type == DownloadButtonType.Downloading);
            // if (type == DownloadButtonType.Downloading)
            // {
            //     var progressbar = Reference.GetReference("progress", downloading).GetComponent<RectTransform>();
            //     var width = progressbar.transform.parent.GetComponent<RectTransform>().rect.width;
            //     progressbar.sizeDelta = new Vector2(width * progress, 0);
            //     var percent = Reference.GetReference("percent", downloading).GetComponent<TextLanguage>();
            //     percent.arguments = new string[] { ((float)progress * 100).ToString("0") };
            //     percent.UpdateText();
            // }
        }

        internal void SetHomeButton(bool hasHome)
        {
            // var homebutton = Reference.GetReference("home.button", tile);
            // var set = Reference.GetReference("no", homebutton);
            // var reset = Reference.GetReference("yes", homebutton);
            // set.SetActive(!hasHome);
            // reset.SetActive(hasHome);
        }

        internal enum DownloadButtonType
        {
            Download = 0,
            Downloading = 1,
            Downloaded = 2
        }
        private EventSubscription eventWorldUpdate;
        private HomeWidget worldMeWidget;

        internal void OnDispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(eventWorldUpdate);
        }
    }
}