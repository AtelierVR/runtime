
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.CCK.Worlds;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using System.Collections.Generic;
using Nox.SimplyLibs;
using System.Linq;

namespace api.nox.game
{
    internal class WorldTileManager
    {
        internal GameClientSystem clientMod;
        private GameObject tile;
        private EventSubscription eventWorldUpdate;
        private HomeWidget worldMeWidget;

        internal WorldTileManager(GameClientSystem clientMod)
        {
            this.clientMod = clientMod;
        }
        private async UniTask<bool> UpdateTexure(RawImage img, string url)
        {
            var tex = await clientMod.NetworkAPI.FetchTexture(url);
            if (tex != null)
            {
                img.texture = tex;
                return true;
            }
            else return false;
        }

        internal void OnDispose()
        {
            clientMod.coreAPI.EventAPI.Unsubscribe(eventWorldUpdate);
        }

        internal void SendTile(EventData context)
        {
            var world = ((context.Data[1] as object[])[0] as ShareObject).Convert<SimplyWorld>();
            var tile = new TileObject()
            {
                id = "api.nox.game.world",
                onRemove = () => this.tile = null,
                GetContent = (Transform tf) =>
                {
                    var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.world");
                    pf.SetActive(false);
                    this.tile = Object.Instantiate(pf, tf);
                    UpdateContent(this.tile, world);
                    return this.tile;
                }
            };
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        private void UpdateContent(GameObject tile, SimplyWorld world)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>().arguments = new string[] { world.title };
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { world.title };
            Reference.GetReference("description", tile).GetComponent<TextLanguage>().arguments = new string[] { world.description };
            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(world.thumbnail)) UpdateTexure(icon, world.thumbnail).Forget();
            CheckVersion(tile, world).Forget();
            CheckHome(tile, world);
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
                var server = clientMod.NetworkAPI.GetCurrentServer();
                server = server != null && server.features.Contains("instance") ? server : null;
                clientMod.GotoTile("game.instance.make", world, versions == null || versions.Length == 0 ? null : asset, server);
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
            var scene = await WorldManager.LoadWorld(asset.hash, 0);
            if (scene == default)
                Debug.LogError("Failed to load world");
            UpdateContent(tile, world);
        }

        private void CheckHome(GameObject tile, SimplyWorld world)
        {
            var dlb = Reference.GetReference("home.button", tile).GetComponent<Button>();
            dlb.interactable = false;
            dlb.onClick.RemoveAllListeners();
            var user = clientMod.NetworkAPI.GetCurrentUser();
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
                await clientMod.NetworkAPI.User.UpdateUser(new SimplyUserUpdate { home = "NULL" });
                dlb.interactable = true;
                CheckHome(tile, world);
            }
            else
            {
                dlb.interactable = false;
                var wp = new WorldPatern() { id = world.id, server = world.server };
                await clientMod.NetworkAPI.User.UpdateUser(new SimplyUserUpdate { home = wp.ToString() });
                dlb.interactable = true;
                CheckHome(tile, world);
            }
        }

        internal void SetDownloadButton(DownloadButtonType type, float progress = 0)
        {
            Debug.Log("SetDownloadButton" + type + " " + progress);
            if (tile == null) return;
            var downloadbutton = Reference.GetReference("download.button", tile);
            var start = Reference.GetReference("start", downloadbutton);
            var downloaded = Reference.GetReference("downloaded", downloadbutton);
            var downloading = Reference.GetReference("downloading", downloadbutton);
            start.SetActive(type == DownloadButtonType.Download);
            downloaded.SetActive(type == DownloadButtonType.Downloaded);
            downloading.SetActive(type == DownloadButtonType.Downloading);
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

        internal void SetHomeButton(bool hasHome)
        {
            if (tile == null) return;
            var homebutton = Reference.GetReference("home.button", tile);
            var set = Reference.GetReference("no", homebutton);
            var reset = Reference.GetReference("yes", homebutton);
            set.SetActive(!hasHome);
            reset.SetActive(hasHome);
        }

        internal enum DownloadButtonType
        {
            Download = 0,
            Downloading = 1,
            Downloaded = 2
        }
    }
}