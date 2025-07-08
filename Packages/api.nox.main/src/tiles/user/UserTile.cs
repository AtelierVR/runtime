/*using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.UI;
using api.nox.game.UI;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using Transform = UnityEngine.Transform;

namespace api.nox.game.Tiles
{
    internal class UserTileManager : TileManager
    {
        internal class UserTileObject : TileObject
        {
            public UnityAction<INoxObject> OnUserUpdated;

            public INoxObject User
            {
                get => GetData<INoxObject>(0);
                set => SetData(0, value);
            }
        }

        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            var tile = new UserTileObject() { id = "api.nox.game.user", context = context };
            tile.GetContent = tf => OnGetContent(tile, tf);
            tile.onDisplay = (_, gameObject) => OnDisplay(tile, gameObject);
            tile.onRemove = () => OnRemove(tile);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        private void OnUserTileUpdate(UserTileObject tile, GameObject content, INoxObject user)
        {
            var cUser = tile.User;
            if (cUser == null) return;
            if (cUser.GetField<uint>("id") != user.GetField<uint>("id")) return;
            if (cUser.GetField<string>("server") != user.GetField<string>("server")) return;
            tile.User = user;
            UpdateContent(tile, content);
        }

        private void OnRemove(UserTileObject tile)
        {
            if (tile.OnUserUpdated != null)
                _onUserUpdated.RemoveListener(tile.OnUserUpdated);
            tile.OnUserUpdated = null;
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tf"></param>
        /// <returns></returns>
        private GameObject OnGetContent(UserTileObject tile, Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/users/content.prefab");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.user";
            if (tile.OnUserUpdated != null)
                _onUserUpdated.RemoveListener(tile.OnUserUpdated);
            tile.OnUserUpdated = (user) => OnUserTileUpdate(tile, content, user);
            _onUserUpdated.AddListener(tile.OnUserUpdated);
            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        private void OnDisplay(UserTileObject tile, GameObject content)
            => UpdateContent(tile, content);

        internal UserWidget Widget;
        private readonly EventSubscription _userUpdateSub;
        private readonly EventSubscription _userFetchSub;
        private readonly UnityEvent<INoxObject> _onUserUpdated;

        internal UserTileManager()
        {
            Widget = new UserWidget();
            _onUserUpdated = new UnityEvent<INoxObject>();
            _userUpdateSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_update", OnUserUpdate);
            _userFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_fetch", OnUserFetch);
        }

        private void OnUserFetch(EventData data)
        {
            if (data.Data[0] is not INoxObject user) return;
            _onUserUpdated?.Invoke(user);
        }

        private void OnUserUpdate(EventData data)
        {
            if (data.Data[0] is not INoxObject user) return;
            _onUserUpdated?.Invoke(user);
        }


        internal void OnDispose()
        {
            Widget.Dispose();
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_userUpdateSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_userFetchSub);
            _onUserUpdated?.RemoveAllListeners();
            Widget = null;
        }

        private void UpdateContent(UserTileObject tile, GameObject content)
        {
            var user = tile.User;
            if (user == null)
            {
                Logger.LogError("User is null");
                return;
            }

            var display = user.GetField<string>("display");
            var banner = user.GetField<string>("banner");
            var thumbnail = user.GetField<string>("thumbnail");

            Reference.GetReference("title", content).GetComponent<TextLanguage>()
                .UpdateText(new[] { display });

            var withBanner = Reference.GetReference("withbanner", content);
            var noBanner = Reference.GetReference("nobanner", content);
            withBanner.SetActive(!string.IsNullOrEmpty(banner));
            noBanner.SetActive(string.IsNullOrEmpty(banner));

            var current = string.IsNullOrEmpty(banner) ? noBanner : withBanner;

            Reference.GetReference("display", current).GetComponent<TextLanguage>()
                .UpdateText(new[] { display });

            if (!string.IsNullOrEmpty(banner))
            {
                var thumb = Reference.GetReference("banner", current).GetComponent<RawImage>();
                UpdateTexture(thumb, banner).Forget();
            }

            if (!string.IsNullOrEmpty(thumbnail))
            {
                var thumb = Reference.GetReference("icon", current).GetComponent<RawImage>();
                UpdateTexture(thumb, thumbnail).Forget();
            }


            var refreshUser = Reference.GetReference("refresh_user", content).GetComponent<Button>();
            refreshUser.onClick.RemoveAllListeners();
            refreshUser.onClick.AddListener(() => OnClickRefreshUser(tile, content).Forget());
        }


        private async UniTask OnClickRefreshUser(UserTileObject tile, GameObject content)
        {
            var dlb = Reference.GetReference("refresh_user", content).GetComponent<Button>();
            if (!dlb.interactable) return;
            dlb.interactable = false;
            var user = tile.User;
            if (user == null)
            {
                Logger.LogError("User is null");
                dlb.interactable = true;
                return;
            }

            user = await GameClientSystem.NetworkAPI.GetField("User")
                .CallAsyncMethod("GetUser", user.GetField<string>("server"), user.GetField<uint>("id"));

            if (user == null)
            {
                Logger.LogError("User not found");
                dlb.interactable = true;
                return;
            }

            tile.User = user;
            dlb.interactable = true;

            UpdateContent(tile, content);
        }
    }
}*/