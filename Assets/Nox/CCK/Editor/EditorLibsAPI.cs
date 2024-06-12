
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;

namespace Nox.CCK.Editor
{
    public interface EditorLibsAPI
    {
        public ModMetadata LoadMetadata(string path);
    }
}