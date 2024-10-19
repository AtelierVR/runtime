
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using api.nox.game.UI;
using UnityEngine.Events;
using System;
using Object = UnityEngine.Object;
using api.nox.network.Users;

namespace api.nox.game.Tiles
{
    internal class UserTileManager : TileManager
    {
        internal class UserTileObject : TileObject
        {
            public UnityAction<User> OnUserUpdated;
            public User User
            {
                get => GetData<User>(0);
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
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = (str) => OnOpen(tile, tile.content);
            tile.onHide = (str) => OnHide(tile, tile.content);
            tile.onRemove = () => OnRemove(tile);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        internal void OnUserTileUpdate(UserTileObject tile, GameObject content, User user)
        {
            var cUser = tile.User;
            if (cUser == null) return;
            if (cUser.id != user.id) return;
            if (cUser.server != user.server) return;
            tile.User = user;
            UpdateContent(tile, content);
        }

        internal void OnRemove(UserTileObject tile)
        {
            if (tile.OnUserUpdated != null)
                OnUserUpdated.RemoveListener(tile.OnUserUpdated);
            tile.OnUserUpdated = null;
        }

        /// <summary>
        /// Get the content of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tf"></param>
        /// <returns></returns>
        internal GameObject OnGetContent(UserTileObject tile, Transform tf)
        {
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.user");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);
            content.name = "game.user";

            if (tile.OnUserUpdated != null)
                OnUserUpdated.RemoveListener(tile.OnUserUpdated);
            tile.OnUserUpdated = (user) => OnUserTileUpdate(tile, content, user);
            OnUserUpdated.AddListener(tile.OnUserUpdated);

            return content;
        }

        /// <summary>
        /// Handle the display of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnDisplay(UserTileObject tile, GameObject content)
        {
            UpdateContent(tile, content);
        }

        /// <summary>
        /// Handle the opening of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnOpen(UserTileObject tile, GameObject content)
        {
        }

        /// <summary>
        /// Handle the hiding of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="content"></param>
        internal void OnHide(UserTileObject tile, GameObject content)
        {
        }

        private UserWidget _widget;
        private EventSubscription UserUpdateSub;
        private EventSubscription UserFetchSub;

        // make unity event to invoke when user is updated
        [Serializable] public class UserUpdatedEvent : UnityEvent<User> { }
        public UserUpdatedEvent OnUserUpdated;

        internal UserTileManager()
        {
            _widget = new UserWidget();
            OnUserUpdated = new UserUpdatedEvent();
            UserUpdateSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_update", OnUserUpdate);
            UserFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_fetch", OnUserFetch);
        }

        private void OnUserFetch(EventData data)
        {
            var user = data.Data[0] as User;
            if (user == null) return;
            OnUserUpdated?.Invoke(user);
        }

        private void OnUserUpdate(EventData data)
        {
            var user = data.Data[0] as User;
            if (user == null) return;
            OnUserUpdated?.Invoke(user);
        }


        internal void OnDispose()
        {
            _widget.Dispose();
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(UserUpdateSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(UserFetchSub);
            OnUserUpdated?.RemoveAllListeners();
            _widget = null;
        }

        private void UpdateContent(UserTileObject tile, GameObject content)
        {
            var user = tile.User;
            if (user == null)
            {
                Debug.LogError("User is null");
                return;
            }

            Reference.GetReference("title", content).GetComponent<TextLanguage>().UpdateText(new string[] { user.display });

            var withbanner = Reference.GetReference("withbanner", content);
            var nobanner = Reference.GetReference("nobanner", content);
            withbanner.SetActive(!string.IsNullOrEmpty(user.banner));
            nobanner.SetActive(string.IsNullOrEmpty(user.banner));

            var current = string.IsNullOrEmpty(user.banner) ? nobanner : withbanner;

            Reference.GetReference("display", current).GetComponent<TextLanguage>().UpdateText(new string[] { user.display });

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

            
            var refresh_user = Reference.GetReference("refresh_user", content).GetComponent<Button>();
            refresh_user.onClick.RemoveAllListeners();
            refresh_user.onClick.AddListener(() => OnClickRefreshUser(tile, content).Forget());
        }


        private async UniTask OnClickRefreshUser(UserTileObject tile, GameObject content)
        {
            var dlb = Reference.GetReference("refresh_user", content).GetComponent<Button>();
            if (!dlb.interactable) return;
            dlb.interactable = false;
            var user = tile.User;
            if (user == null)
            {
                Debug.LogError("User is null");
                dlb.interactable = true;
                return;
            }

            user = await GameClientSystem.Instance.NetworkAPI.User.GetUser(user.server, user.id);

            if (user == null)
            {
                Debug.LogError("User not found");
                dlb.interactable = true;
                return;
            }

            tile.User = user;
            dlb.interactable = true;

            UpdateContent(tile, content);
        }
    }
}