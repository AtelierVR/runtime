using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Metadata;
using IMod = Nox.ModLoader.Mods.Mod;

namespace Nox.ModLoader.Cores.Mods
{
    public class ModAPI : CCK.Mods.Mods.IModAPI
    {
        internal IMod Mod;

        public ModAPI(IMod mod)
        {
            Mod = mod;
        }

        public ModMetadata GetMetadata(string id) => GetInternalMod(id)?.GetMetadata();
        public ModMetadata[] GetDetectedMetadatas() => GetInternalMods().Select(mod => mod.GetMetadata()).ToArray();

        public Mod GetMod(string id) => GetInternalMod(id);
        internal IMod GetInternalMod(string id) => ModManager.GetMod(id);

        public Mod[] GetMods() => GetInternalMods();
        internal IMod[] GetInternalMods() => ModManager.GetMods();


        public UniTask<Mod> LoadMod(string id)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<bool> UnloadMod(string id)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<bool> ReloadMod(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}