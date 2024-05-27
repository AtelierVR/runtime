using Cysharp.Threading.Tasks;

namespace Nox.CCK.Mods.Mods
{
    public interface ModAPI
    {
        public Mod GetMod(string id);
        public Mod[] GetMods();
        public UniTask<Mod> LoadMod(string id);
        public UniTask<bool> UnloadMod(string id);
        public UniTask<bool> ReloadMod(string id);
        public ModMetadata GetMetadata(string id);
    }
}