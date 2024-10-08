using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace api.nox.game.Tiles
{
    public class TileManager
    {
        internal static async UniTask<bool> UpdateTexure(RawImage img, string url)
        {
            var tex = await GameClientSystem.Instance.NetworkAPI.FetchTexture(url);
            if (tex != null)
            {
                img.texture = tex;
                return true;
            }
            else return false;
        }
    }
}