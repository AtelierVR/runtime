using System;
using api.nox.game.UI;
using api.nox.network.Servers;
using api.nox.network.Users;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.game.Tiles
{
    public class UserWidget : IDisposable
    {
        public UserWidget()
        {
            UserUpdateSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_update", OnUserUpdate);
            UserConnectSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_connect", OnUserConnect);
            UserDisconnectSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("user_disconnect", OnUserDisconnect);
            Initialization().Forget();
        }
        public void Dispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(UserUpdateSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(UserConnectSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(UserDisconnectSub);
        }

        private EventSubscription UserUpdateSub;
        private EventSubscription UserConnectSub;
        private EventSubscription UserDisconnectSub;

        private HomeWidget userMeWidget;
        private HomeWidget homeWidget;
        private HomeWidget serverMeWidget;

        private void OnUserUpdate(EventData context) => OnUpdateUserWidget(context.Data[0] as UserMe, true);
        private void OnUserConnect(EventData context) => OnUpdateUserWidget(context.Data[0] as UserMe, true);
        private void OnUserDisconnect(EventData context) => OnUpdateUserWidget(null, false);

        private void OnUpdateUserWidget(UserMe user, bool connected)
        {
            if (connected)
            {
                userMeWidget ??= new HomeWidget { id = "game.user.me", width = 3, height = 2 };
                homeWidget ??= new HomeWidget { id = "game.user.me.home", width = 1, height = 1 };
                userMeWidget.GetContent = (int menuId, Transform ft) => OnGetContentUserMe(menuId, userMeWidget, user, ft);
                homeWidget.GetContent = (int menuId, Transform ft) => OnGetContentHome(menuId, homeWidget, user, ft);
                GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", userMeWidget);
                GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", homeWidget);
            }
            else
            {
                if (userMeWidget != null)
                {
                    userMeWidget.GetContent = null;
                    GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", userMeWidget);
                    userMeWidget = null;
                }
                if (homeWidget != null)
                {
                    homeWidget.GetContent = null;
                    GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", homeWidget);
                    homeWidget = null;
                }
            }
        }

        private void OnUpdateServerWidget(Server server, bool connected)
        {
            if (connected)
            {
                serverMeWidget ??= new HomeWidget { id = "game.server.me", width = 3, height = 2 };
                serverMeWidget.GetContent = (int menuId, Transform ft) => OnGetContentServerMe(menuId, serverMeWidget, server, ft);
                GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", serverMeWidget);
            }
            else
            {
                if (serverMeWidget != null)
                {
                    serverMeWidget.GetContent = null;
                    GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", serverMeWidget);
                    serverMeWidget = null;
                }
            }
        }

        /// <summary>
        /// Initialize the user widget
        /// </summary>
        /// <returns></returns>
        private async UniTask Initialization()
        {
            var user = GameClientSystem.Instance.NetworkAPI.User.CurrentUser;
            user ??= await GameClientSystem.Instance.NetworkAPI.User.GetMyUser();
            var server = GameClientSystem.Instance.NetworkAPI.Server.CurrentServer;
            server ??= await GameClientSystem.Instance.NetworkAPI.Server.GetMyServer();
            OnUpdateUserWidget(user, user != null);
            OnUpdateServerWidget(server, server != null);
        }


        /// <summary>
        /// Get the content of the user widget
        /// </summary>
        /// <param name="widget"></param>
        /// <param name="user"></param>
        /// <param name="tf"></param>
        /// <returns></returns>
        private GameObject OnGetContentUserMe(int menuId, HomeWidget widget, UserMe user, Transform tf)
        {
            var baseprefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget.userme");
            var btn = Object.Instantiate(baseprefab, tf);
            var btncontent = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            Reference.GetReference("display", btncontent).GetComponent<TextLanguage>().UpdateText(new string[] { user.display });
            var ban = Reference.GetReference("banner", btncontent).GetComponent<RawImage>();
            var gban = ban.gameObject;
            gban.SetActive(false);
            if (!string.IsNullOrEmpty(user.banner))
                try { _ = TileManager.UpdateTexure(ban, user.banner).ContinueWith((bool a) => gban.SetActive(a)); }
                catch (Exception e) { Debug.LogError(e); }
            var thumb = Reference.GetReference("icon", btncontent).GetComponent<RawImage>();
            var iconmask = Reference.GetReference("iconmask", btncontent);
            iconmask.SetActive(false);
            if (!string.IsNullOrEmpty(user.thumbnail))
                try { _ = TileManager.UpdateTexure(thumb, user.thumbnail).ContinueWith((bool a) => iconmask.SetActive(a)); }
                catch (Exception e) { Debug.LogError(e); }
            var btnref = Reference.GetReference("button", btn).GetComponent<Button>();
            btnref.onClick.AddListener(() => OnClickWidgetUser(menuId, widget, user));
            return btn;
        }

        /// <summary>
        /// Get the content of the home widget
        /// </summary>
        /// <param name="widget"></param>
        /// <param name="user"></param>
        /// <param name="tf"></param>
        /// <returns></returns>
        private GameObject OnGetContentHome(int menuId, HomeWidget widget, UserMe user, Transform tf)
        {
            var baseprefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget.userme.home");
            var btn = Object.Instantiate(baseprefab, tf);
            var btncontent = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            var btnref = Reference.GetReference("button", btn).GetComponent<Button>();
            btnref.onClick.AddListener(() => OnClickWidgetHome(menuId, widget, user).Forget());

            return btn;
        }

        /// <summary>
        /// Get the content of the server widget
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="widget"></param>
        /// <param name="server"></param>
        private GameObject OnGetContentServerMe(int menuId, HomeWidget widget, Server server, Transform tf)
        {
            var baseprefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget.serverme");
            var btn = Object.Instantiate(baseprefab, tf);
            var btncontent = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            Reference.GetReference("display", btncontent).GetComponent<TextLanguage>().UpdateText(new string[] { server.title });
            var ban = Reference.GetReference("icon", btncontent).GetComponent<RawImage>();
            var iconmask = Reference.GetReference("iconmask", btncontent);
            iconmask.SetActive(false);
            if (!string.IsNullOrEmpty(server.icon))
                _ = TileManager.UpdateTexure(ban, server.icon).ContinueWith((bool a) => iconmask.SetActive(a));
            var btnref = Reference.GetReference("button", btn).GetComponent<Button>();
            btnref.onClick.AddListener(() => OnClickWidgetServer(menuId, widget, server));
            return btn;
        }


        private void OnClickWidgetUser(int menuId, HomeWidget widget, UserMe user)
        {
            if (user == null) return;
            MenuManager.Instance.SendGotoTile(menuId, "game.user", user);
        }

        private async UniTask OnClickWidgetHome(int menuId, HomeWidget widget, UserMe user)
        {
            if (user == null)
            {
                Debug.Log("User is null");
                return;
            }
            var home = await user.GetHome();
            if (home == null)
            {
                Debug.Log("Home is null");
                return;
            }

            var asset = await GameClientSystem.Instance.NetworkAPI.World.Asset.SearchAssets(new()
            {
                server = home.server,
                world_id = home.id,
                limit = 1,
                platforms = new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) },
                engines = new string[] { "unity" },
                offset = 0
            });

            if (asset == null || asset.assets.Length == 0)
            {
                Debug.Log("Asset is null");
                return;
            }

            MenuManager.Instance.SendGotoTile(menuId, "game.world", home, asset.assets[0]);
        }

        private void OnClickWidgetServer(int menuId, HomeWidget widget, Server server)
        {
            if (server == null) return;
            MenuManager.Instance.SendGotoTile(menuId, "game.server", server);
        }
    }
}