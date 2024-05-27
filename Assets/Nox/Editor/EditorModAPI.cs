using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Mods;

namespace Nox.Editor.Mods
{
    public class EditorModAPI : ModAPI
    {
        private EditorMod _mod;
        internal EditorModAPI(EditorMod mod) => _mod = mod;
        
        public ModMetadata GetMetadata(string id) => GetEditorMod(id)?.GetMetadata();

        internal EditorMod GetEditorMod(string id) => EditorModManager.GetMod(id);

        internal EditorMod[] GetEditorMods() => EditorModManager.GetMods().ToArray();
        public Mod GetMod(string id) => GetEditorMod(id);

        public Mod[] GetMods() => GetEditorMods();

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