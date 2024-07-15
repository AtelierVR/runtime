
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;

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
        }
    }
}