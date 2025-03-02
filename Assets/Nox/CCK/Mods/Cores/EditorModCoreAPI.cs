using Nox.CCK.Mods.Libs;
using Nox.CCK.Mods.Panels;

namespace Nox.CCK.Mods.Cores
{
    public interface EditorModCoreAPI : ModCoreAPI
    {
        public EditorModPanelAPI PanelAPI { get; }
        public EditorLibsAPI LibsAPI { get; }
    }
}