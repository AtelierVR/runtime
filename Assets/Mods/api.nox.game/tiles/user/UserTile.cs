
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using Nox.SimplyLibs;
using api.nox.game.UI;
using api.nox.game.Tiles;

namespace api.nox.game
{
    internal class UserTileManager : TileManager
    {
        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            Debug.Log("UserTileManager.SendTile");
            var tile = new TileObject() { id = "api.nox.game.user", context = context };
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
        /// <returns></returns>
        internal GameObject OnGetContent(TileObject tile, Transform tf)
        {
            Debug.Log("UserTileManager.GetTileContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.user");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.user";
            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnDisplay(TileObject tile, GameObject content)
        {
            Debug.Log("UserTileManager.OnDisplay");
            var user = tile.GetData<ShareObject>(0)?.Convert<SimplyUser>();
            UpdateContent(content, user);
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnOpen(TileObject tile, GameObject content)
        {
            Debug.Log("UserTileManager.OnOpen");
        }

        /// <summary>
        /// Handle the hiding of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnHide(TileObject tile, GameObject content)
        {
            Debug.Log("UserTileManager.OnHide");
        }

        private UserWidget _widget;
        internal UserTileManager()
        {
            _widget = new UserWidget();
        }

        internal void OnDispose()
        {
            _widget.Dispose();
            _widget = null;
        }

        private void UpdateContent(GameObject content, SimplyUser user)
        {
            Debug.Log($"UserTileManager.UpdateContent({content}, {user})");
            Reference.GetReference("title", content).GetComponent<TextLanguage>().arguments = new string[] { user.display };

            var withbanner = Reference.GetReference("withbanner", content);
            var nobanner = Reference.GetReference("nobanner", content);
            withbanner.SetActive(!string.IsNullOrEmpty(user.banner));
            nobanner.SetActive(string.IsNullOrEmpty(user.banner));

            var current = string.IsNullOrEmpty(user.banner) ? nobanner : withbanner;

            Reference.GetReference("display", current).GetComponent<TextLanguage>().arguments = new string[] { user.display };

            if (!string.IsNullOrEmpty(user.banner))
            {
                var thumb = Reference.GetReference("banner", current).GetComponent<RawImage>();
                UpdateTexure(thumb, user.banner).Forget();
            }

            if (!string.IsNullOrEmpty(user.thumbnail))
            {
                var thumb = Reference.GetReference("icon", current).GetComponent<RawImage>();
                UpdateTexure(thumb, user.thumbnail).Forget();
            }
        }

    }
}