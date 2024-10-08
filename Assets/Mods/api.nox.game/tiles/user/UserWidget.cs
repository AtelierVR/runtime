using System;
using api.nox.game.UI;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.SimplyLibs;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.game.Tiles
{
    public class UserWidget : IDisposable
    {
        public UserWidget()
        {
            sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("network.user", OnUserUpdate);
            Initialization().Forget();
        }
        public void Dispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(sub);
        }

        private EventSubscription sub;
        private HomeWidget userMeWidget;
        private HomeWidget homeWidget;


        /// <summary>
        /// Initialize the user widget
        /// </summary>
        /// <returns></returns>
        private async UniTask Initialization()
        {
            var user = GameClientSystem.Instance.NetworkAPI.GetCurrentUser();
            user ??= await GameClientSystem.Instance.NetworkAPI.User.GetMyUser();
            if (user != null) OnUserConnect(user);
            else OnUserDisconnect();
        }

        /// <summary>
        /// Handle the user update
        /// </summary>
        /// <param name="context"></param>
        private void OnUserUpdate(EventData context)
        {
            if (context.Data[1] as bool? ?? false) return;
            var user = (context.Data[0] as ShareObject).Convert<SimplyUserMe>();
            if (user == null) OnUserDisconnect();
            else OnUserConnect(user);
        }

        /// <summary>
        /// Handle the user disconnecting
        /// </summary>
        private void OnUserDisconnect()
        {
            Debug.Log("User disconnected.");
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

        /// <summary>
        /// Handle the user connecting
        /// </summary>
        /// <param name="user"></param>
        private void OnUserConnect(SimplyUserMe user)
        {
            Debug.Log("User connected: " + user.username);
            userMeWidget ??= new HomeWidget { id = "game.user.me", width = 3, height = 2 };
            homeWidget ??= new HomeWidget { id = "game.user.me.home", width = 1, height = 1 };
            userMeWidget.GetContent = (int menuId, Transform ft) => OnGetContentUserMe(menuId, userMeWidget, user, ft);
            homeWidget.GetContent = (int menuId, Transform ft) => OnGetContentHome(menuId, homeWidget, user, ft);
            GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", userMeWidget);
            GameClientSystem.CoreAPI.EventAPI.Emit("game.widget", homeWidget);
        }

        /// <summary>
        /// Get the content of the user widget
        /// </summary>
        /// <param name="widget"></param>
        /// <param name="user"></param>
        /// <param name="tf"></param>
        /// <returns></returns>
        private GameObject OnGetContentUserMe(int menuId, HomeWidget widget, SimplyUserMe user, Transform tf)
        {
            Debug.Log("UserWidget.GetContentUserMe");
            var baseprefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget.userme");
            var btn = Object.Instantiate(baseprefab, tf);
            var btncontent = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            Reference.GetReference("display", btncontent).GetComponent<TextLanguage>().UpdateText(new string[] { user.display });
            var ban = Reference.GetReference("banner", btncontent).GetComponent<RawImage>();
            var gban = ban.gameObject;
            if (!string.IsNullOrEmpty(user.banner))
                _ = TileManager.UpdateTexure(ban, user.banner).ContinueWith((bool a) => gban.SetActive(a));
            var thumb = Reference.GetReference("icon", btncontent).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(user.thumbnail))
                TileManager.UpdateTexure(thumb, user.thumbnail).Forget();
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
        private GameObject OnGetContentHome(int menuId, HomeWidget widget, SimplyUserMe user, Transform tf)
        {
            Debug.Log("UserWidget.OnGetContentHome");
            var baseprefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget.userme.home");
            var btn = Object.Instantiate(baseprefab, tf);
            var btncontent = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            var btnref = Reference.GetReference("button", btn).GetComponent<Button>();
            btnref.onClick.AddListener(() => OnClickWidgetHome(menuId, widget, user).Forget());
            return btn;
        }


        private void OnClickWidgetUser(int menuId, HomeWidget widget, SimplyUserMe user)
        {
            Debug.Log("UserWidget.OnClickWidgetUser");
            if (user == null) return;
            MenuManager.Instance.SendGotoTile(menuId, "game.user", user);
        }

        private async UniTask OnClickWidgetHome(int menuId, HomeWidget widget, SimplyUserMe user)
        {
            Debug.Log("UserWidget.OnClickWidgetHome");
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
            MenuManager.Instance.SendGotoTile(menuId, "game.world", home);
        }
    }
}