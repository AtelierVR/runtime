
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.CCK.Worlds;
using UnityEngine;
using Nox.CCK;
using UnityEngine.UI;

namespace api.nox.game
{
    internal class WorldTileManager
    {
        internal GameClientSystem clientMod;
        private GameObject tile;
        private EventSubscription eventWorldUpdate;
        private HomeWidget worldMeWidget;

        internal WorldTileManager(GameClientSystem clientMod)
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
            clientMod.coreAPI.EventAPI.Unsubscribe(eventWorldUpdate);
        }

        internal void SendTile(EventData context)
        {
            var world = ((context.Data[1] as object[])[0] as ShareObject).Convert<SimplyWorld>();
            var tile = new TileObject()
            {
                id = "api.nox.game.world",
                onRemove = () => this.tile = null,
                GetContent = (Transform tf) =>
                {
                    var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.world");
                    pf.SetActive(false);
                    this.tile = Object.Instantiate(pf, tf);
                    UpdateContent(this.tile, world);
                    return this.tile;
                }
            };
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        private void UpdateContent(GameObject tile, SimplyWorld world)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>().arguments = new string[] { world.title };
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { world.title };
            Reference.GetReference("description", tile).GetComponent<TextLanguage>().arguments = new string[] { world.description };
            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(world.thumbnail)) UpdateTexure(icon, world.thumbnail).Forget();
        }
    }
}