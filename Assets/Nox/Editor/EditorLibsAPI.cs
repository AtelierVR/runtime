using Nox.CCK.Mods;

namespace Nox.Editor.Mods
{
    public class EditorLibsAPI : CCK.Editor.EditorLibsAPI
    {
        private EditorMod _mod;

        internal EditorLibsAPI(EditorMod mod)
        {
            _mod = mod;
        }

        public ModMetadata LoadMetadata(string path) => Nox.Mods.ModMetadata.LoadFromPath(path);
    }
}