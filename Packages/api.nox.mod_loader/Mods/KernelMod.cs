using Cysharp.Threading.Tasks;
using Nox.ModLoader.Cores.Assets;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.ModLoader.Mods
{
    public class KernelMod : Mod
    {

        internal KernelMod()
        {
            CoreAPI = new CoreAPI(this);
            #if UNITY_EDITOR
            AssetAPI = new EditorKernelAssetAPI(this);
            #else
            AssetAPI = new KernelAssetAPI(this);
            #endif
        }
        
        public override bool IsLoaded() 
            => base.IsLoaded() && AssetAPI.IsLoaded();

        public override async UniTask<bool> Load()
        {
            Logger.LogDebug($"Loading {Metadata.GetId()}");

            if (!await base.Load())
                return false;

            return await AssetAPI.RegisterAssets();
        }

        public override async UniTask<bool> Unload()
        {
            Logger.LogDebug($"Unloading {Metadata.GetId()}");

            if (!await base.Unload())
                return false;

            return await AssetAPI.UnRegisterAssets();
        }


    }
}