using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Networks;

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