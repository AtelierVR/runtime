/*using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.game.Tiles
{
    public class TileManager
    {
        internal static async UniTask<bool> UpdateTexture(RawImage img, string url)
        {
            var tex = await GameClientSystem.NetworkAPI.CallAsyncMethod<Texture2D>("FetchTexture", url, null, null, null);
            if (!tex) return false;
            
            try
            {
                img.texture = tex;
                return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }
    }
}*/