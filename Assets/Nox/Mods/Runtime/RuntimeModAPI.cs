using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Mods;
using Nox.Mods.Client;

namespace Nox.Mods.Mods
{
    public class RuntimeModAPI : ModAPI
    {
        private RuntimeMod _mod;
        internal RuntimeModAPI(RuntimeMod mod)
        {
            _mod = mod;
        }

        public CCK.Mods.ModMetadata[] GetDetectedMetadatas() => ModManager.DetectedMetadatas();
        public CCK.Mods.ModMetadata GetMetadata(string id) => ModManager.GetMod(id)?.GetMetadata();
        public Mod GetMod(string id) => ModManager.GetMod(id);
        public Mod[] GetMods() => ModManager.GetMods();

        public UniTask<Mod> LoadMod(string id)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<bool> ReloadMod(string id)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<bool> UnloadMod(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}