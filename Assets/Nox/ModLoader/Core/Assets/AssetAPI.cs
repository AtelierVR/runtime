using Cysharp.Threading.Tasks;

namespace Nox.ModLoader.Cores.Assets
{
    public interface AssetAPI : CCK.Mods.Assets.AssetAPI
    {
        public UniTask<bool> RegisterAssets();
        public UniTask<bool> UnRegisterAssets();
        public bool IsLoaded();
    }
}