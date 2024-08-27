
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using Nox.SimplyLibs;
using NUnit.Framework.Constraints;
using api.nox.game.sessions;

namespace api.nox.game
{
    internal class InstanceTileManager
    {
        internal GameClientSystem clientMod;
        private GameObject tile;
        private EventSubscription eventInstanceUpdate;

        internal InstanceTileManager(GameClientSystem clientMod)
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
            clientMod.coreAPI.EventAPI.Unsubscribe(eventInstanceUpdate);
        }

        internal void SendTile(EventData context)
        {
            var instance = ((context.Data[1] as object[])[0] as ShareObject).Convert<SimplyInstance>();
            var tile = new TileObject()
            {
                id = "api.nox.game.instance",
                onRemove = () => this.tile = null,
                GetContent = (Transform tf) =>
                {
                    var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.instance");
                    pf.SetActive(false);
                    this.tile = Object.Instantiate(pf, tf);
                    UpdateContent(this.tile, instance);
                    return this.tile;
                }
            };
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        private void UpdateContent(GameObject tile, SimplyInstance instance)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.title };
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.title };
            Reference.GetReference("description", tile).GetComponent<TextLanguage>().arguments = new string[] { instance.description };

            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(instance.thumbnail)) UpdateTexure(icon, instance.thumbnail).Forget();

            var gotobtn = Reference.GetReference("goto.button", tile).GetComponent<Button>();
            gotobtn.onClick.RemoveAllListeners();
            gotobtn.interactable = true;
            if (instance.address == null) gotobtn.interactable = false;
            else
            {
                var relay = clientMod.NetworkAPI.Relay.GetRelay(instance.address);
                if (relay != null)
                {
                    Debug.Log("Relay found: " + instance.address);
                }
            }

            if (gotobtn.interactable)
                gotobtn.onClick.AddListener(() => JoinOnlineSession(tile, instance).Forget());
        }

        private async UniTask JoinOnlineSession(GameObject tile, SimplyInstance instance)
        {
            var gotobtn = Reference.GetReference("goto.button", tile).GetComponent<Button>();
            if (!gotobtn.interactable) return;
            gotobtn.interactable = false;

            var user = clientMod.NetworkAPI.GetCurrentUser();
            if (user == null)
            {
                gotobtn.interactable = true;
                return;
            }

            var session = GameSystem.instance.sessionManager.GetSession(instance.server, instance.id);
            if (session != null)
            {
                session.SetCurrent();
                return;
            }
            var token = await clientMod.NetworkAPI.Auth.GetToken(instance.server);
            if (token == null)
            {
                gotobtn.interactable = true;
                return;
            }

            var controller = new OnlineController(instance)
            {
                connectionData = new()
                {
                    relay_address = instance.address,
                    master_address = instance.server,
                    authentificate = new()
                    {
                        token = token.token,
                        server_address = user.server,
                        use_integrity_token = token.isIntegrity,
                        user_id = user.id
                    }
                }
            };
            session = GameSystem.instance.sessionManager.New(controller, instance.server, instance.id);
            var pre = await controller.Prepare();
            if (!pre)
                session.Dispose();

            gotobtn.interactable = true;
            return;
        }
    }
}