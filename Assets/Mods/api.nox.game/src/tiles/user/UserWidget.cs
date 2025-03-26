using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.UI;
using Cysharp.Threading.Tasks;
using Logger = Nox.CCK.Utils.Logger;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using Transform = UnityEngine.Transform;

namespace api.nox.game.Tiles
{
    public class UserWidget : IDisposable
    {
        public UserWidget()
        {
           /* _userUpdateSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_update", OnUserUpdate);
            _userConnectSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_connect", OnUserConnect);
            _userDisconnectSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_disconnect", OnUserDisconnect);*/
        }

        public void Dispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_userUpdateSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_userConnectSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_userDisconnectSub);
        }

        private readonly EventSubscription _userUpdateSub;
        private readonly EventSubscription _userConnectSub;
        private readonly EventSubscription _userDisconnectSub;

        private HomeWidget _userMeWidget;
        private HomeWidget _homeWidget;
        private HomeWidget _serverMeWidget;

        private void OnUserUpdate(EventData context) => OnUpdateUserWidget(context.Data[0] as INoxObject, true);
        private void OnUserConnect(EventData context) => OnUpdateUserWidget(context.Data[0] as INoxObject, true);
        private void OnUserDisconnect(EventData context) => OnUpdateUserWidget(null, false);

        private void OnUpdateUserWidget(INoxObject user, bool connected)
        {
            if (connected)
            {
                _userMeWidget ??= new HomeWidget { id = "game.user.me", width = 3, height = 2 };
                _homeWidget ??= new HomeWidget { id = "game.user.me.home", width = 1, height = 1 };
                _userMeWidget.GetContent = (menuId, ft) => OnGetContentUserMe(menuId, user, ft);
                _homeWidget.GetContent = (menuId, ft) => OnGetContentHome(menuId, _homeWidget, user, ft);
                GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", _userMeWidget);
                GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", _homeWidget);
            }
            else
            {
                if (_userMeWidget != null)
                {
                    _userMeWidget.GetContent = null;
                    GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", _userMeWidget);
                    _userMeWidget = null;
                }

                if (_homeWidget != null)
                {
                    _homeWidget.GetContent = null;
                    GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", _homeWidget);
                    _homeWidget = null;
                }
            }
        }

        private void OnUpdateServerWidget(INoxObject server, bool connected)
        {
            if (connected)
            {
                _serverMeWidget ??= new HomeWidget { id = "game.server.me", width = 3, height = 2 };
                _serverMeWidget.GetContent = (menuId, ft) => OnGetContentServerMe(menuId, _serverMeWidget, server, ft);
                GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", _serverMeWidget);
            }
            else if (_serverMeWidget != null)
            {
                _serverMeWidget.GetContent = null;
                GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", _serverMeWidget);
                _serverMeWidget = null;
            }
        }

        /// <summary>
        /// Initialize the user widget
        /// </summary>
        /// <returns></returns>
        internal async UniTask Initialization()
        {
            var userApi = GameClientSystem.NetworkAPI.GetField("User");
            var serverApi = GameClientSystem.NetworkAPI.GetField("Server");
            Logger.Log($"UserAPI: {userApi} [{string.Join(", ", userApi.GetFields())}] [{string.Join(", ", userApi.GetMethods())}]");
            Logger.Log($"ServerAPI: {serverApi} [{string.Join(", ", serverApi.GetFields())}] [{string.Join(", ", serverApi.GetMethods())}]");
            var user = userApi.CallMethod("GetCurrentUser");
            user ??= await userApi.CallAsyncMethod("GetMyUser");
            Logger.Log($"User: {user}");
            var server = serverApi.CallMethod("GetCurrentServer");
            server ??= await serverApi.CallAsyncMethod("GetMyServer");
            Logger.Log($"Server: {server}");
            OnUpdateUserWidget(user, user != null);
            OnUpdateServerWidget(server, server != null);
        }


        /// <summary>
        /// Get the content of the user widget
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="user"></param>
        /// <param name="tf"></param>
        /// <returns></returns>
        private GameObject OnGetContentUserMe(int menuId, INoxObject user, Transform tf)
        {
            var asset = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.prefab");
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/users/widget.prefab");
            var btn = Object.Instantiate(asset, tf);
            var content = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            Reference.GetReference("display", content).GetComponent<TextLanguage>()
                .UpdateText(new[] { user.GetField<string>("display") });
            var ban = Reference.GetReference("banner", content).GetComponent<RawImage>();
            ban.gameObject.SetActive(false);
            var banner = user.GetField<string>("banner");
            if (!string.IsNullOrEmpty(banner))
                try
                {
                    _ = TileManager.UpdateTexture(ban, banner)
                        .ContinueWith(a => ban.gameObject.SetActive(a));
                }
                catch
                {
                    // ignored
                }

            var btree = Reference.GetReference("button", btn).GetComponent<Button>();
            btree.onClick.AddListener(() => OnClickWidgetUser(menuId, user));
            return btn;
        }

        /// <summary>
        /// Get the content of the home widget
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="widget"></param>
        /// <param name="user"></param>
        /// <param name="tf"></param>
        /// <returns></returns>
        private GameObject OnGetContentHome(int menuId, HomeWidget widget, INoxObject user, Transform tf)
        {
            var asset = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.prefab");
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.userme.home.prefab");
            var btn = Object.Instantiate(asset, tf);
            var content = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            var btree = Reference.GetReference("button", btn).GetComponent<Button>();
            btree.onClick.AddListener(() => OnClickWidgetHome(menuId, user).Forget());
            return btn;
        }

        /// <summary>
        /// Get the content of the server widget
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="widget"></param>
        /// <param name="server"></param>
        /// <param name="tf"></param>
        private GameObject OnGetContentServerMe(int menuId, HomeWidget widget, INoxObject server, Transform tf)
        {
            var asset = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.prefab");
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.serverme.prefab");
            var btn = Object.Instantiate(asset, tf);
            var content = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            Reference.GetReference("display", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { server.GetField<string>("title") });
            var ban = Reference.GetReference("icon", content).GetComponent<RawImage>();
            var iconMask = Reference.GetReference("iconmask", content);
            iconMask.SetActive(false);
            var icon = server.GetField<string>("icon");
            if (!string.IsNullOrEmpty(icon))
                _ = TileManager.UpdateTexture(ban, icon).ContinueWith(a => iconMask.SetActive(a));
            var btree = Reference.GetReference("button", btn).GetComponent<Button>();
            btree.onClick.AddListener(() => OnClickWidgetServer(menuId, server));
            return btn;
        }


        private void OnClickWidgetUser(int menuId, INoxObject user)
        {
            if (user == null) return;
            MenuManager.Instance.SendGotoTile(menuId, "game.user", user);
        }

        private async UniTask OnClickWidgetHome(int menuId, INoxObject user)
        {
            if (user == null)
            {
                Logger.Log("User is null");
                return;
            }

            var home = await user.CallAsyncMethod("GetHome");
            if (home == null)
            {
                Logger.Log("Home is null");
                return;
            }

            var asset = await GameClientSystem.NetworkAPI
                .GetField("World").GetField("Asset")
                .CallAsyncMethod("SearchAssets", new Dictionary<string, object>
                {
                    { "server", home.GetField<string>("server") },
                    { "world_id", home.GetField<uint>("id") },
                    { "limit", 1 },
                    { "platforms", new[] { Constants.CurrentPlatform.GetPlatformName() } },
                    { "engines", new[] { Constants.CurrentEngine.GetEngineName() } },
                    { "offset", 0 }
                });

            if (asset == null)
            {
                Logger.Log("Asset is null");
                return;
            }

            var assets = asset.GetField<INoxObject[]>("assets");

            if (assets.Length == 0)
            {
                Logger.Log("Assets is empty");
                return;
            }

            MenuManager.Instance.SendGotoTile(menuId, "game.world", home, assets[0]);
        }

        private void OnClickWidgetServer(int menuId, INoxObject server)
        {
            if (server == null) return;
            MenuManager.Instance.SendGotoTile(menuId, "game.server", server);
        }
    }
}