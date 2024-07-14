
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
        private TileObject tile;
        private EventSubscription eventWorldUpdate;
        private HomeWidget worldMeWidget;

        internal WorldTileManager(GameClientSystem clientMod)
        {
            this.clientMod = clientMod;
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

        internal void OnDispose()
        {
            clientMod.coreAPI.EventAPI.Unsubscribe(eventWorldUpdate);
        }

        internal void SendTile(EventData context)
        {
            if (this.tile != null)
            {
                clientMod.coreAPI.EventAPI.Emit("game.tile", this.tile);
                return;
            }
            var world = ((context.Data[1] as object[])[0] as ShareObject).Convert<World>();
            var tile = new TileObject()
            {
                onRemove = () => { 
                    this.tile = null;
                }
            };
            var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.world");
            pf.SetActive(false);
            tile.content = Object.Instantiate(pf);
            UpdateContent(tile.content, world);
            tile.id = "api.nox.game.world";
            this.tile = tile;
            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        private void UpdateContent(GameObject tile, World world)
        {
            Reference.GetReference("display", tile).GetComponent<TextLanguage>().arguments = new string[] { world.title };
            Reference.GetReference("title", tile).GetComponent<TextLanguage>().arguments = new string[] { world.title };

            var icon = Reference.GetReference("icon", tile).GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(world.thumbnail)) UpdateTexure(icon, world.thumbnail).Forget();
        }
    }
}