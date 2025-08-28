using Nox.CCK.Mods.Libs;
using Nox.CCK.Mods.Panels;

namespace Nox.CCK.Mods.Cores
{
    public interface EditorModCoreAPI : IModCoreAPI
    {
        public EditorModPanelAPI PanelAPI { get; }
        public EditorLibsAPI LibsAPI { get; }
    }
}