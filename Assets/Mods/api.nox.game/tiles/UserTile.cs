
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.CCK.Users;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;

namespace api.nox.game
{
    internal class UserTileManager
    {
        internal GameClientSystem clientMod;
        private TileObject tile;
        private EventSubscription eventUserUpdate;
        private HomeWidget userMeWidget;

        internal UserTileManager(GameClientSystem clientMod)
        {
            Debug.Log("UserTileManager");
            this.clientMod = clientMod;
            eventUserUpdate = clientMod.coreAPI.EventAPI.Subscribe("network.user", OnUserUpdate);
            Debug.Log("UserTileManager initialized.");
            Initialization().Forget();
        }

        private void OnUserUpdate(EventData context)
        {
            if (context.Data[1] as bool? ?? false) return;
            var user = (context.Data[0] as ShareObject).Convert<UserMe>();
            if (user == null) OnUserDisconnect();
            else OnUserConnect(user);
        }

        private void OnUserConnect(UserMe user)
        {
            Debug.Log("User connected: " + user.username);
            if (userMeWidget == null)
                userMeWidget = new HomeWidget
                {
                    id = "game.user.me",
                    width = 3,
                    height = 2,
                };
            userMeWidget.GetContent = (Transform parent) =>
            {
                var baseprefab = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
                var prefab = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget.userme");
                var btn = Object.Instantiate(baseprefab, parent);
                var btncontent = Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
                var dis = Reference.GetReference("display", btncontent).GetComponent<TextLanguage>();
                dis.arguments = new string[] { user.display };
                dis.UpdateText();
                var ban = Reference.GetReference("banner", btncontent).GetComponent<RawImage>();
                ban.gameObject.SetActive(false);
                if (!string.IsNullOrEmpty(user.banner))
                {
                    UniTask uniTask = UpdateTexure(ban, user.banner).ContinueWith(_ => ban.gameObject.SetActive(true));
                }
                var thumb = Reference.GetReference("icon", btncontent).GetComponent<RawImage>();
                if (!string.IsNullOrEmpty(user.thumbnail)) UpdateTexure(thumb, user.thumbnail).Forget();
                var btnref = Reference.GetReference("button", btn).GetComponent<Button>();
                btnref.onClick.AddListener(OnClickWidget);
                return btn;
            };
            clientMod.coreAPI.EventAPI.Emit("game.widget", userMeWidget);
        }

        private void OnClickWidget()
        {
            var user = clientMod.coreAPI.NetworkAPI.GetCurrentUser();
            if (user == null) return;
            clientMod.GotoTile("game.user", user);
        }

        private async UniTask<bool> UpdateTexure(RawImage img, string url)
        {
            var tex = await clientMod.coreAPI.NetworkAPI.FetchTexture(url);
            if (tex != null)
            {
                img.texture = tex;
                return true;
            }
            else return false;
        }

        private void OnUserDisconnect()
        {
            Debug.Log("User disconnected.");
            if (userMeWidget != null)
            {
                userMeWidget.GetContent = null;
                clientMod.coreAPI.EventAPI.Emit("game.widget", userMeWidget);
                userMeWidget = null;
            }
        }

        internal void OnDispose()
        {
            clientMod.coreAPI.EventAPI.Unsubscribe(eventUserUpdate);
        }

        internal void SendTile(EventData context)
        {
            if (this.tile != null)
            {
                clientMod.coreAPI.EventAPI.Emit("game.tile", this.tile);
                return;
            }
            var user = ((context.Data[1] as object[])[0] as ShareObject).Convert<User>();
            var tile = new TileObject()
            {
                onRemove = () =>
                {
                    this.tile = null;
                }
            };
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.user");
            pf.SetActive(false);
            tile.content = Object.Instantiate(pf);
            UpdateContent(tile.content, user);
            tile.id = "api.nox.game.user";
            this.tile = tile;
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        private void UpdateContent(GameObject tile, User user)
        {
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { user.display };

            var withbanner = Reference.GetReference("withbanner", tile);
            var nobanner = Reference.GetReference("nobanner", tile);
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

        private async UniTask Initialization()
        {
            var user = clientMod.coreAPI.NetworkAPI.GetCurrentUser();
            user ??= await clientMod.coreAPI.NetworkAPI.UserAPI.GetMyUser();
            if (user != null) OnUserConnect(user);
            else OnUserDisconnect();
        }

    }
}