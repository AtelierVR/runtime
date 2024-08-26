
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;
using Nox.SimplyLibs;
using NUnit.Framework.Constraints;

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
                gotobtn.onClick.AddListener(() => JoinInstance(tile, instance).Forget());
        }

        private async UniTask JoinInstance(GameObject tile, SimplyInstance instance)
        {
            var gotobtn = Reference.GetReference("goto.button", tile).GetComponent<Button>();
            if (!gotobtn.interactable) return;
            gotobtn.interactable = false;

            var user = clientMod.NetworkAPI.GetCurrentUser();
            if (user == null)
            {
                Debug.LogError("User not found");
                gotobtn.interactable = true;
                return;
            }

            var auth = await clientMod.NetworkAPI.Auth.GetToken(instance.server);
            if (auth == null)
            {
                Debug.LogError("Failed to get auth token");
                gotobtn.interactable = true;
                return;
            }

            var result = await clientMod.NetworkAPI.Relay.MakeConnection(new()
            {
                protocol = SimplyRelayProtocol.UDP,
                relay_address = instance.address,
                master_address = instance.server,
                authentificate = new()
                {
                    token = auth.token,
                    use_integrity_token = auth.isIntegrity,
                    user_id = user.id,
                    server_address = user.server
                }
            });

            if (result == null || !result.IsSuccess)
            {
                Debug.LogError("Failed to connect to relay");
                gotobtn.interactable = true;
                return;
            }

            var status = await result.Relay.RequestStatus();
            if (status == null)
            {
                Debug.LogError("Failed to get relay status");
                gotobtn.interactable = true;
                return;
            }

            SimplyRelayInstance currentInstance = null; 
            foreach (var inst in status.Instances)
                if (inst.Id == instance.id)
                {
                    currentInstance = inst;
                    break;
                }

            if (currentInstance == null)
            {
                Debug.LogError("Instance not found");
                gotobtn.interactable = true;
                return;
            }

            Debug.Log("Joining instance: " + instance.id + " with internal id: " + currentInstance.InternalId);

            var enter = await currentInstance.Enter(new()
            {
                DisplayName = user.username,
                Password = ""
            });

            if (enter == null || !enter.IsSuccess)
            {
                Debug.LogError("Failed to enter instance");
                gotobtn.interactable = true;
                return;
            }

            Debug.Log("Entered instance: " + instance.id + " with internal id: " + currentInstance.InternalId);






        }


    }
}